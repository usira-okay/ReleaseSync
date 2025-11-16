// <copyright file="GoogleSheetSyncService.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseSync.Application.Configuration;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Exceptions;
using ReleaseSync.Application.Mappers;
using ReleaseSync.Application.Models;

namespace ReleaseSync.Application.Services;

/// <summary>
/// Google Sheet 同步服務實作。
/// 協調 Google Sheet 的資料同步流程，包含 UK 比對、row 更新/插入等邏輯。
/// </summary>
public class GoogleSheetSyncService : IGoogleSheetSyncService
{
    private readonly GoogleSheetSettings _settings;
    private readonly IGoogleSheetApiClient _apiClient;
    private readonly IGoogleSheetDataMapper _dataMapper;
    private readonly IGoogleSheetRowParser _rowParser;
    private readonly ILogger<GoogleSheetSyncService> _logger;

    /// <summary>
    /// 初始化 <see cref="GoogleSheetSyncService"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">Google Sheet 設定。</param>
    /// <param name="apiClient">Google Sheets API 客戶端。</param>
    /// <param name="dataMapper">資料對應器。</param>
    /// <param name="rowParser">Row 解析器。</param>
    /// <param name="logger">日誌記錄器。</param>
    public GoogleSheetSyncService(
        IOptions<GoogleSheetSettings> settings,
        IGoogleSheetApiClient apiClient,
        IGoogleSheetDataMapper dataMapper,
        IGoogleSheetRowParser rowParser,
        ILogger<GoogleSheetSyncService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _dataMapper = dataMapper ?? throw new ArgumentNullException(nameof(dataMapper));
        _rowParser = rowParser ?? throw new ArgumentNullException(nameof(rowParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("正在驗證 Google Sheet 組態...");

        // 驗證必要設定
        if (string.IsNullOrWhiteSpace(_settings.SpreadsheetId))
        {
            throw new GoogleSheetConfigurationException("Google Sheet ID 未設定。請在 appsettings.json 或 User Secrets 中設定 GoogleSheet:SpreadsheetId。");
        }

        if (string.IsNullOrWhiteSpace(_settings.SheetName))
        {
            throw new GoogleSheetConfigurationException("工作表名稱未設定。請在 appsettings.json 中設定 GoogleSheet:SheetName。");
        }

        if (string.IsNullOrWhiteSpace(_settings.ServiceAccountCredentialPath))
        {
            throw new GoogleSheetConfigurationException("Service Account 憑證路徑未設定。請在 appsettings.json 或 User Secrets 中設定 GoogleSheet:ServiceAccountCredentialPath。");
        }

        // 驗證憑證檔案是否存在
        if (!File.Exists(_settings.ServiceAccountCredentialPath))
        {
            throw new GoogleSheetCredentialNotFoundException(_settings.ServiceAccountCredentialPath);
        }

        // 驗證欄位對應
        var columnMapping = new GoogleSheetColumnMapping
        {
            RepositoryNameColumn = _settings.ColumnMapping.RepositoryNameColumn,
            FeatureColumn = _settings.ColumnMapping.FeatureColumn,
            TeamColumn = _settings.ColumnMapping.TeamColumn,
            AuthorsColumn = _settings.ColumnMapping.AuthorsColumn,
            PullRequestUrlsColumn = _settings.ColumnMapping.PullRequestUrlsColumn,
            UniqueKeyColumn = _settings.ColumnMapping.UniqueKeyColumn,
        };

        if (!columnMapping.IsValid())
        {
            throw new GoogleSheetConfigurationException("欄位對應設定無效。請確認所有欄位都是 A-Z 或 AA-ZZ 格式，且不重複。");
        }

        // 驗證憑證
        await _apiClient.AuthenticateAsync(cancellationToken);

        // 驗證 Spreadsheet 是否存在
        var spreadsheetExists = await _apiClient.SpreadsheetExistsAsync(_settings.SpreadsheetId, cancellationToken);
        if (!spreadsheetExists)
        {
            throw new GoogleSheetNotFoundException($"Google Sheet 不存在或無法存取。ID: {_settings.SpreadsheetId}。請確認 Google Sheet ID 正確，並確保 Service Account 已被授予存取權限。");
        }

        // 驗證工作表是否存在
        var sheetExists = await _apiClient.SheetExistsAsync(_settings.SpreadsheetId, _settings.SheetName, cancellationToken);
        if (!sheetExists)
        {
            throw new GoogleSheetNotFoundException($"工作表不存在。名稱: {_settings.SheetName}。請在 Google Sheet 中建立此工作表，或更正設定中的工作表名稱。");
        }

        _logger.LogInformation("Google Sheet 組態驗證成功");
    }

    /// <inheritdoc/>
    public async Task<GoogleSheetSyncResult> SyncAsync(RepositoryBasedOutputDto repositoryData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryData);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("開始 Google Sheet 同步...");

            // 驗證組態
            await ValidateConfigurationAsync(cancellationToken);

            // 取得欄位對應設定
            var columnMapping = new GoogleSheetColumnMapping
            {
                RepositoryNameColumn = _settings.ColumnMapping.RepositoryNameColumn,
                FeatureColumn = _settings.ColumnMapping.FeatureColumn,
                TeamColumn = _settings.ColumnMapping.TeamColumn,
                AuthorsColumn = _settings.ColumnMapping.AuthorsColumn,
                PullRequestUrlsColumn = _settings.ColumnMapping.PullRequestUrlsColumn,
                UniqueKeyColumn = _settings.ColumnMapping.UniqueKeyColumn,
            };

            // 讀取現有 Sheet 資料
            _logger.LogInformation("正在讀取現有 Google Sheet 資料...");
            var existingData = await _apiClient.ReadSheetDataAsync(_settings.SpreadsheetId, _settings.SheetName, cancellationToken);

            // 建立 UK 索引 (跳過第一行標題列)
            var ukToRowIndex = BuildUniqueKeyIndex(existingData, columnMapping);

            // 將 Repository 資料轉換為 SheetRowData
            var newRowDataList = _dataMapper.MapToSheetRows(repositoryData);
            _logger.LogInformation("處理 PR/MR 資料: {RepositoryCount} repositories, {RowCount} rows", repositoryData.Repositories.Count, newRowDataList.Count);

            // 比對 UK 並產生同步操作
            var operations = GenerateSyncOperations(newRowDataList, ukToRowIndex, existingData, columnMapping);

            // 執行批次更新
            var updatedCount = operations.Count(op => op.OperationType == SheetOperationType.Update);
            var insertedCount = operations.Count(op => op.OperationType == SheetOperationType.Insert);

            _logger.LogInformation("計畫操作: {UpdateCount} updates, {InsertCount} inserts", updatedCount, insertedCount);

            if (operations.Count > 0)
            {
                await _apiClient.BatchUpdateAsync(_settings.SpreadsheetId, operations, columnMapping, cancellationToken);
            }

            stopwatch.Stop();

            var spreadsheetUrl = _apiClient.GenerateSpreadsheetUrl(_settings.SpreadsheetId);
            var processedPrCount = repositoryData.Repositories.Sum(r => r.PullRequests.Count);

            _logger.LogInformation(
                "Google Sheet 同步完成 - 更新: {UpdatedCount}, 新增: {InsertedCount}, 處理 PR/MR: {ProcessedCount}, 執行時間: {Duration:F1} 秒",
                updatedCount,
                insertedCount,
                processedPrCount,
                stopwatch.Elapsed.TotalSeconds);

            return GoogleSheetSyncResult.Success(
                updatedCount,
                insertedCount,
                processedPrCount,
                spreadsheetUrl,
                stopwatch.Elapsed);
        }
        catch (GoogleSheetException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Google Sheet 同步失敗: {Message}", ex.Message);
            return GoogleSheetSyncResult.Failure(ex.Message, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Google Sheet 同步發生非預期錯誤");
            return GoogleSheetSyncResult.Failure($"非預期錯誤: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// 建立 Unique Key 到 Row 索引的對應。
    /// </summary>
    /// <param name="sheetData">工作表資料。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>UK 到 Row 索引的字典。</returns>
    private Dictionary<string, int> BuildUniqueKeyIndex(IList<IList<object>> sheetData, GoogleSheetColumnMapping columnMapping)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // 跳過第一行 (標題列)，從第二行開始
        for (var i = 1; i < sheetData.Count; i++)
        {
            var rowData = _rowParser.ParseRow(sheetData[i], i + 1, columnMapping);
            if (!string.IsNullOrWhiteSpace(rowData.UniqueKey))
            {
                index[rowData.UniqueKey] = i + 1; // 1-based row number
            }
        }

        _logger.LogDebug("建立 UK 索引: {Count} 筆", index.Count);
        return index;
    }

    /// <summary>
    /// 產生同步操作清單。
    /// </summary>
    /// <param name="newRowDataList">新的 row 資料清單。</param>
    /// <param name="ukToRowIndex">UK 到 Row 索引的字典。</param>
    /// <param name="existingData">現有工作表資料。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>同步操作清單。</returns>
    private List<SheetSyncOperation> GenerateSyncOperations(
        IReadOnlyList<SheetRowData> newRowDataList,
        Dictionary<string, int> ukToRowIndex,
        IList<IList<object>> existingData,
        GoogleSheetColumnMapping columnMapping)
    {
        var operations = new List<SheetSyncOperation>();
        var nextRowNumber = existingData.Count + 1; // 新增資料的起始位置

        foreach (var newRowData in newRowDataList)
        {
            if (ukToRowIndex.TryGetValue(newRowData.UniqueKey, out var existingRowNumber))
            {
                // 更新現有 row - 合併 Authors 和 PR URLs
                var existingRowData = _rowParser.ParseRow(existingData[existingRowNumber - 1], existingRowNumber, columnMapping);
                var mergedRowData = existingRowData.MergeWith(newRowData);

                operations.Add(new SheetSyncOperation
                {
                    OperationType = SheetOperationType.Update,
                    TargetRowNumber = existingRowNumber,
                    RowData = mergedRowData,
                });
            }
            else
            {
                // 插入新 row
                var rowDataWithNumber = newRowData with { RowNumber = nextRowNumber };
                operations.Add(new SheetSyncOperation
                {
                    OperationType = SheetOperationType.Insert,
                    TargetRowNumber = nextRowNumber,
                    RowData = rowDataWithNumber,
                });
                nextRowNumber++;
            }
        }

        return operations;
    }
}
