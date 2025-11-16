// <copyright file="GoogleSheetApiClient.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using ReleaseSync.Application.Configuration;
using ReleaseSync.Application.Exceptions;
using ReleaseSync.Application.Models;
using ReleaseSync.Application.Services;

namespace ReleaseSync.Infrastructure.GoogleSheet;

/// <summary>
/// Google Sheets API 客戶端實作。
/// 使用 Service Account 憑證驗證並與 Google Sheets API 互動。
/// </summary>
public class GoogleSheetApiClient : IGoogleSheetApiClient, IDisposable
{
    private readonly GoogleSheetSettings _settings;
    private readonly IGoogleSheetRowParser _rowParser;
    private readonly ILogger<GoogleSheetApiClient> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private SheetsService? _sheetsService;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="GoogleSheetApiClient"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">Google Sheet 設定。</param>
    /// <param name="rowParser">Row 解析器。</param>
    /// <param name="logger">日誌記錄器。</param>
    public GoogleSheetApiClient(
        IOptions<GoogleSheetSettings> settings,
        IGoogleSheetRowParser rowParser,
        ILogger<GoogleSheetApiClient> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _rowParser = rowParser ?? throw new ArgumentNullException(nameof(rowParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 建立 Polly retry 策略
        _retryPolicy = Policy
            .Handle<Google.GoogleApiException>(ex => ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                _settings.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(_settings.RetryWaitSeconds),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Google Sheets API 速率限制，重試 {RetryCount}/{MaxRetries}，等待 {WaitSeconds} 秒",
                        retryCount,
                        _settings.RetryCount,
                        timeSpan.TotalSeconds);
                });
    }

    /// <inheritdoc/>
    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ServiceAccountCredentialPath))
        {
            throw new GoogleSheetConfigurationException("Service Account 憑證路徑未設定。請在 appsettings.json 或 User Secrets 中設定 GoogleSheet:ServiceAccountCredentialPath。");
        }

        if (!File.Exists(_settings.ServiceAccountCredentialPath))
        {
            throw new GoogleSheetCredentialNotFoundException(_settings.ServiceAccountCredentialPath);
        }

        try
        {
            _logger.LogDebug("正在驗證 Service Account 憑證...");

            GoogleCredential credential;
            using (var stream = new FileStream(_settings.ServiceAccountCredentialPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            _sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ReleaseSync",
            });

            _logger.LogInformation("Service Account 憑證驗證成功");
        }
        catch (Exception ex) when (ex is not GoogleSheetException)
        {
            throw new GoogleSheetAuthenticationException($"Service Account 憑證驗證失敗: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SpreadsheetExistsAsync(string spreadsheetId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = _sheetsService!.Spreadsheets.Get(spreadsheetId);
                await request.ExecuteAsync(cancellationToken);
            });

            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new GoogleSheetPermissionDeniedException(spreadsheetId);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SheetExistsAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            var spreadsheet = await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = _sheetsService!.Spreadsheets.Get(spreadsheetId);
                return await request.ExecuteAsync(cancellationToken);
            });

            return spreadsheet.Sheets.Any(s => s.Properties.Title == sheetName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IList<IList<object>>> ReadSheetDataAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        _logger.LogDebug("正在讀取工作表資料: {SheetName}", sheetName);

        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var range = $"{sheetName}!A:ZZ"; // 讀取所有欄位
            var request = _sheetsService!.Spreadsheets.Values.Get(spreadsheetId, range);
            return await request.ExecuteAsync(cancellationToken);
        });

        var result = response.Values ?? new List<IList<object>>();
        _logger.LogInformation("讀取工作表資料完成: {RowCount} rows", result.Count);

        return result;
    }

    /// <inheritdoc/>
    public async Task<int> BatchUpdateAsync(
        string spreadsheetId,
        IReadOnlyList<SheetSyncOperation> operations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (operations.Count == 0)
        {
            _logger.LogInformation("無同步操作需要執行");
            return 0;
        }

        _logger.LogDebug("正在執行批次更新: {OperationCount} 個操作", operations.Count);

        var updateValues = new List<ValueRange>();
        var sheetName = _settings.SheetName;

        foreach (var operation in operations)
        {
            var rowValues = _rowParser.ToRowValues(operation.RowData, columnMapping);
            var range = $"{sheetName}!A{operation.TargetRowNumber}";

            updateValues.Add(new ValueRange
            {
                Range = range,
                Values = new List<IList<object>> { rowValues },
            });
        }

        var batchUpdateRequest = new BatchUpdateValuesRequest
        {
            Data = updateValues,
            ValueInputOption = "USER_ENTERED",
        };

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var request = _sheetsService!.Spreadsheets.Values.BatchUpdate(batchUpdateRequest, spreadsheetId);
            await request.ExecuteAsync(cancellationToken);
        });

        _logger.LogInformation("批次更新完成: {OperationCount} 個操作", operations.Count);
        return operations.Count;
    }

    /// <inheritdoc/>
    public string GenerateSpreadsheetUrl(string spreadsheetId)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new ArgumentException("Spreadsheet ID 不可為空", nameof(spreadsheetId));
        }

        return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/edit";
    }

    /// <summary>
    /// 確保已完成驗證。
    /// </summary>
    private void EnsureAuthenticated()
    {
        if (_sheetsService == null)
        {
            throw new InvalidOperationException("尚未完成驗證。請先呼叫 AuthenticateAsync。");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 釋放資源。
    /// </summary>
    /// <param name="disposing">是否正在釋放。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sheetsService?.Dispose();
                _sheetsService = null;
            }

            _disposed = true;
        }
    }
}
