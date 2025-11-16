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

        // 分離 Insert 和 Update 操作
        var insertOperations = operations.Where(op => op.OperationType == SheetOperationType.Insert)
                                          .OrderBy(op => op.TargetRowNumber) // 從小到大排序
                                          .ToList();
        var updateOperations = operations.Where(op => op.OperationType == SheetOperationType.Update).ToList();

        // 再執行更新操作
        if (updateOperations.Count > 0)
        {
            await UpdateExistingRowsAsync(spreadsheetId, updateOperations, columnMapping, cancellationToken);
        }

        // 先執行插入操作（按行數由小到大，確保插入順序正確）
        if (insertOperations.Count > 0)
        {
            await InsertRowsAsync(spreadsheetId, insertOperations, columnMapping, cancellationToken);
        }

        _logger.LogInformation("批次更新完成: {InsertCount} 新增, {UpdateCount} 更新", insertOperations.Count, updateOperations.Count);
        return operations.Count;
    }

    /// <summary>
    /// 插入新 rows（在指定位置插入並下推現有資料）。
    /// </summary>
    private async Task InsertRowsAsync(
        string spreadsheetId,
        IReadOnlyList<SheetSyncOperation> insertOperations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("正在插入 {Count} 個新 rows", insertOperations.Count);

        // 取得 SheetId
        var sheetId = await GetSheetIdAsync(spreadsheetId, _settings.SheetName, cancellationToken);

        var sortedOperations = insertOperations.OrderBy(op => op.TargetRowNumber).ToList();

        foreach (var operation in sortedOperations)
        {
            var requests = new List<Request>();

            // 1. 插入空白行（在 TargetRowNumber 位置插入）
            requests.Add(new Request
            {
                InsertDimension = new InsertDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = operation.TargetRowNumber - 1, // 0-based index
                        EndIndex = operation.TargetRowNumber, // 插入 1 行
                    },
                    InheritFromBefore = false,
                },
            });

            // 2. 更新插入的 row 資料
            var rowValues = _rowParser.ToRowValues(operation.RowData, columnMapping);
            var rowData = new RowData
            {
                Values = rowValues.Select(v => new CellData
                {
                    UserEnteredValue = new ExtendedValue
                    {
                        StringValue = v?.ToString(),
                    },
                }).ToList(),
            };

            requests.Add(new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = operation.TargetRowNumber - 1, // 0-based index
                        EndRowIndex = operation.TargetRowNumber,
                        StartColumnIndex = 0,
                        EndColumnIndex = rowValues.Count,
                    },
                    Rows = new List<RowData> { rowData },
                    Fields = "userEnteredValue",
                },
            });

            // 執行單一插入操作
            var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests,
            };

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = _sheetsService!.Spreadsheets.BatchUpdate(batchUpdateRequest, spreadsheetId);
                await request.ExecuteAsync(cancellationToken);
            });

            _logger.LogDebug("插入 row {RowNumber} 完成: {RepositoryName}", operation.TargetRowNumber, operation.RowData.RepositoryName);
        }

        _logger.LogInformation("插入 {Count} 個新 rows 完成", insertOperations.Count);
    }

    /// <summary>
    /// 更新現有 rows。
    /// </summary>
    private async Task UpdateExistingRowsAsync(
        string spreadsheetId,
        IReadOnlyList<SheetSyncOperation> updateOperations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("正在更新 {Count} 個現有 rows", updateOperations.Count);

        var updateValues = new List<ValueRange>();
        var sheetName = _settings.SheetName;

        foreach (var operation in updateOperations)
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

        _logger.LogInformation("更新 {Count} 個現有 rows 完成", updateOperations.Count);
    }

    /// <summary>
    /// 取得 Sheet ID。
    /// </summary>
    private async Task<int> GetSheetIdAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken)
    {
        var spreadsheet = await _retryPolicy.ExecuteAsync(async () =>
        {
            var request = _sheetsService!.Spreadsheets.Get(spreadsheetId);
            return await request.ExecuteAsync(cancellationToken);
        });

        var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
        if (sheet == null)
        {
            throw new GoogleSheetNotFoundException($"找不到工作表: {sheetName}");
        }

        return sheet.Properties.SheetId ?? 0;
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
