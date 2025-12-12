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
using ReleaseSync.Domain.Services;

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
    private readonly ITeamMappingService _teamMappingService;
    private readonly ILogger<GoogleSheetSyncService> _logger;

    /// <summary>
    /// 初始化 <see cref="GoogleSheetSyncService"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">Google Sheet 設定。</param>
    /// <param name="apiClient">Google Sheets API 客戶端。</param>
    /// <param name="dataMapper">資料對應器。</param>
    /// <param name="rowParser">Row 解析器。</param>
    /// <param name="teamMappingService">團隊對應服務。</param>
    /// <param name="logger">日誌記錄器。</param>
    public GoogleSheetSyncService(
        IOptions<GoogleSheetSettings> settings,
        IGoogleSheetApiClient apiClient,
        IGoogleSheetDataMapper dataMapper,
        IGoogleSheetRowParser rowParser,
        ITeamMappingService teamMappingService,
        ILogger<GoogleSheetSyncService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _dataMapper = dataMapper ?? throw new ArgumentNullException(nameof(dataMapper));
        _rowParser = rowParser ?? throw new ArgumentNullException(nameof(rowParser));
        _teamMappingService = teamMappingService ?? throw new ArgumentNullException(nameof(teamMappingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("正在驗證 Google Sheet 組態...");

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
        return await SyncAsync(repositoryData, spreadsheetIdOverride: null, sheetNameOverride: null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GoogleSheetSyncResult> SyncAsync(
        RepositoryBasedOutputDto repositoryData,
        string? spreadsheetIdOverride,
        string? sheetNameOverride,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repositoryData);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 決定實際使用的 SpreadsheetId 和 SheetName (命令列參數優先)
            var effectiveSpreadsheetId = spreadsheetIdOverride ?? _settings.SpreadsheetId;
            var effectiveSheetName = sheetNameOverride ?? _settings.SheetName;

            // 驗證必要設定
            if (string.IsNullOrWhiteSpace(effectiveSpreadsheetId))
            {
                throw new GoogleSheetConfigurationException(
                    "Google Sheet ID 未設定。請透過 --google-sheet-id 參數提供，或在 appsettings.json 中設定 GoogleSheet:SpreadsheetId。");
            }

            if (string.IsNullOrWhiteSpace(effectiveSheetName))
            {
                throw new GoogleSheetConfigurationException(
                    "工作表名稱未設定。請透過 --google-sheet-name 參數提供，或在 appsettings.json 中設定 GoogleSheet:SheetName。");
            }

            // 驗證憑證路徑 (無法透過命令列覆蓋)
            if (string.IsNullOrWhiteSpace(_settings.ServiceAccountCredentialPath))
            {
                throw new GoogleSheetConfigurationException(
                    "Service Account 憑證路徑未設定。請在 appsettings.json 或 User Secrets 中設定 GoogleSheet:ServiceAccountCredentialPath。");
            }

            if (!File.Exists(_settings.ServiceAccountCredentialPath))
            {
                throw new GoogleSheetCredentialNotFoundException(_settings.ServiceAccountCredentialPath);
            }

            _logger.LogInformation(
                "開始 Google Sheet 同步... (Spreadsheet ID: {SpreadsheetId}, Sheet Name: {SheetName})",
                effectiveSpreadsheetId,
                effectiveSheetName);

            // 驗證憑證
            await _apiClient.AuthenticateAsync(cancellationToken);

            // 驗證 Spreadsheet 是否存在 (使用有效的 SpreadsheetId)
            var spreadsheetExists = await _apiClient.SpreadsheetExistsAsync(effectiveSpreadsheetId, cancellationToken);
            if (!spreadsheetExists)
            {
                throw new GoogleSheetNotFoundException(
                    $"Google Sheet 不存在或無法存取。ID: {effectiveSpreadsheetId}。請確認 Google Sheet ID 正確，並確保 Service Account 已被授予存取權限。");
            }

            // 驗證工作表是否存在 (使用有效的 SheetName)
            var sheetExists = await _apiClient.SheetExistsAsync(effectiveSpreadsheetId, effectiveSheetName, cancellationToken);
            if (!sheetExists)
            {
                throw new GoogleSheetNotFoundException(
                    $"工作表不存在。名稱: {effectiveSheetName}。請在 Google Sheet 中建立此工作表，或更正設定中的工作表名稱。");
            }

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

            // 讀取現有 Sheet 資料 (使用有效的設定)
            _logger.LogInformation("正在讀取現有 Google Sheet 資料...");
            var existingData = await _apiClient.ReadSheetDataAsync(effectiveSpreadsheetId, effectiveSheetName, cancellationToken);

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
                await _apiClient.BatchUpdateAsync(effectiveSpreadsheetId, effectiveSheetName, operations, columnMapping, cancellationToken);
            }

            // 在更新完成後，執行區塊排序
            var sortedRowCount = await SortRepositoryBlocksAsync(
                effectiveSpreadsheetId,
                effectiveSheetName,
                columnMapping,
                cancellationToken);

            if (sortedRowCount > 0)
            {
                _logger.LogInformation("區塊排序完成: {SortedRowCount} 個 rows 已重新排列", sortedRowCount);
            }

            stopwatch.Stop();

            var spreadsheetUrl = _apiClient.GenerateSpreadsheetUrl(effectiveSpreadsheetId);
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

        _logger.LogInformation("建立 UK 索引: {Count} 筆", index.Count);
        return index;
    }

    /// <summary>
    /// 產生同步操作清單。
    /// 先處理所有 Update 操作以確定固定的 row 編號，再處理 Insert 操作並計算偏移值。
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
        var pendingInserts = new List<(SheetRowData RowData, string RepositoryName)>();

        // 建立 RepositoryName 到最後一筆資料 row 的索引
        var repositoryLastRowIndex = BuildRepositoryLastRowIndex(existingData, columnMapping);

        // 第一階段：處理所有 Update 操作，確定固定的 TargetRowNumber
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
                // 收集需要插入的資料，稍後處理
                pendingInserts.Add((newRowData, newRowData.RepositoryName));
            }
        }

        // 第二階段：處理 Insert 操作，按照 Repository 最後行的索引從小到大排序處理
        // 這樣可以正確計算偏移值
        // var insertOperations = new List<(int OriginalInsertPosition, SheetRowData RowData)>();
        int offset = 0;
        var sortedRepositories = repositoryLastRowIndex
                                .Select(x => (x.Key, x.Value.Index))
                                .OrderBy(x => x.Index)
                                .Select(x => x.Key)
                                .ToArray();

        foreach (var repositoryName in sortedRepositories)
        {
            var lastRowIndex = repositoryLastRowIndex[repositoryName];

            if (!lastRowIndex.IsAdded)
                lastRowIndex.Add(offset);

            var waitToInserts = pendingInserts
                                    .Where(x => x.RepositoryName == repositoryName)
                                    .Select(x =>
                                    {
                                        offset++;
                                        lastRowIndex.Add();
                                        return new SheetSyncOperation
                                        {
                                            OperationType = SheetOperationType.Insert,
                                            TargetRowNumber = lastRowIndex.Index,
                                            RowData = x.RowData,
                                        };
                                    });
            operations.AddRange(waitToInserts);

        }
        return operations;
    }

    /// <summary>
    /// 建立 RepositoryName 到最後一筆資料 row 的索引。
    /// </summary>
    /// <param name="sheetData">工作表資料。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>RepositoryName 到最後一筆 row number 的字典。</returns>
    private Dictionary<string, LastRowIndex> BuildRepositoryLastRowIndex(IList<IList<object>> sheetData, GoogleSheetColumnMapping columnMapping)
    {
        var index = new Dictionary<string, LastRowIndex>(StringComparer.OrdinalIgnoreCase);

        // 跳過第一行 (標題列)，從第二行開始
        for (var i = 1; i < sheetData.Count; i++)
        {
            var rowData = _rowParser.ParseRow(sheetData[i], i + 1, columnMapping);
            if (!string.IsNullOrWhiteSpace(rowData.RepositoryName))
            {
                var repositories = rowData.RepositoryName.Split(',');
                var lastRowIndex = new LastRowIndex(i + 1);
                // 記錄每個 Repository 最後出現的行數
                foreach (var repo in repositories)
                {
                    index[repo.Trim()] = lastRowIndex; // 1-based row number
                }
            }
        }

        _logger.LogInformation("建立 Repository 最後行數索引: {Count} repositories", index.Count);
        return index;
    }

    /// <summary>
    /// 對同一 Repository 的區塊內進行排序。
    /// 排序規則: Team → MergedAt (空排最後)。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    /// <param name="sheetName">工作表名稱。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>重新排列的 row 數量。</returns>
    private async Task<int> SortRepositoryBlocksAsync(
        string spreadsheetId,
        string sheetName,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken)
    {
        // 讀取最新的 Sheet 資料
        _logger.LogInformation("正在讀取 Sheet 資料以進行區塊排序...");
        var sheetData = await _apiClient.ReadSheetDataAsync(spreadsheetId, sheetName, cancellationToken);

        if (sheetData.Count <= 1)
        {
            _logger.LogInformation("Sheet 資料為空或僅有標題列，跳過排序");
            return 0;
        }

        // 識別所有 Repository 區塊
        var blocks = IdentifyRepositoryBlocks(sheetData, columnMapping);

        if (blocks.Count == 0)
        {
            _logger.LogInformation("未識別到任何區塊，跳過排序");
            return 0;
        }

        _logger.LogInformation("識別到 {BlockCount} 個區塊", blocks.Count);

        // 對每個區塊進行排序（只處理有足夠資料列的區塊）
        var reorderOperations = new List<SheetBlockReorderOperation>();

        foreach (var block in blocks)
        {
            // 區塊至少需要 2 行資料才需要排序
            if (!block.HasDataToSort)
            {
                _logger.LogDebug(
                    "區塊 {RepositoryName} 資料列數不足 ({DataRowCount})，跳過排序",
                    block.RepositoryName,
                    block.DataRowCount);
                continue;
            }

            var sortedOriginalRowNumbers = SortBlockRows(sheetData, block, columnMapping);

            // 檢查是否需要重新排列 (比較排序前後順序)
            var needsReorder = false;
            for (var i = 0; i < sortedOriginalRowNumbers.Count; i++)
            {
                // 檢查排序後的行號是否與原始位置相同
                var expectedRowNumber = block.DataStartRowNumber + i;
                if (sortedOriginalRowNumbers[i] != expectedRowNumber)
                {
                    needsReorder = true;
                    break;
                }
            }

            if (needsReorder)
            {
                reorderOperations.Add(new SheetBlockReorderOperation
                {
                    // 使用 DataStartRowNumber，排序範圍不含區塊標題列
                    StartRowNumber = block.DataStartRowNumber,
                    EndRowNumber = block.EndRowNumber,
                    RepositoryName = block.RepositoryName,
                    SortedOriginalRowNumbers = sortedOriginalRowNumbers,
                });
            }
        }

        if (reorderOperations.Count == 0)
        {
            _logger.LogInformation("所有區塊已排序完成，無需重新排列");
            return 0;
        }

        _logger.LogInformation("需要重新排列 {BlockCount} 個區塊", reorderOperations.Count);

        // 執行批次重新排列
        return await _apiClient.BatchReorderRowsAsync(spreadsheetId, sheetName, reorderOperations, columnMapping, cancellationToken);
    }

    /// <summary>
    /// 識別 Sheet 中的所有 Repository 區塊。
    /// 區塊以 RepositoryName 欄位的值來分組；當 RepositoryName 為空時，看 Feature 欄位是否有值。
    /// 每個區塊的第一行是區塊標題列，不參與排序。
    /// </summary>
    /// <param name="sheetData">工作表資料。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>區塊資訊清單。</returns>
    private List<RepositoryBlock> IdentifyRepositoryBlocks(IList<IList<object>> sheetData, GoogleSheetColumnMapping columnMapping)
    {
        var blocks = new List<RepositoryBlock>();

        // 跳過 Sheet 標題列 (index 0)，從 index 1 開始
        var currentBlockHeaderIndex = -1;
        string? currentRepositoryName = null;

        for (var i = 1; i < sheetData.Count; i++)
        {
            var rowData = _rowParser.ParseRow(sheetData[i], i + 1, columnMapping);
            var repositoryName = rowData.RepositoryName;
            var feature = rowData.Feature;

            // 判斷是否為有效的資料列
            // 當 RepositoryName 為空時，看 Feature 是否有值
            var isValidRow = !string.IsNullOrWhiteSpace(repositoryName) ||
                             !string.IsNullOrWhiteSpace(feature);

            if (!isValidRow)
            {
                // 結束目前區塊
                if (currentBlockHeaderIndex >= 0 && currentRepositoryName != null)
                {
                    blocks.Add(new RepositoryBlock
                    {
                        HeaderRowIndex = currentBlockHeaderIndex,
                        EndRowIndex = i - 1,
                        RepositoryName = currentRepositoryName,
                    });
                }

                currentBlockHeaderIndex = -1;
                currentRepositoryName = null;
                continue;
            }

            // 使用有效的 RepositoryName 或繼承前一列
            var effectiveRepositoryName = !string.IsNullOrWhiteSpace(repositoryName)
                ? repositoryName
                : currentRepositoryName ?? "Unknown";

            // 判斷是否開始新區塊
            if (currentBlockHeaderIndex < 0)
            {
                // 開始新區塊，目前行是區塊標題列
                currentBlockHeaderIndex = i;
                currentRepositoryName = effectiveRepositoryName;
            }
            else if (!string.IsNullOrWhiteSpace(repositoryName) &&
                     !repositoryName.Equals(currentRepositoryName, StringComparison.OrdinalIgnoreCase))
            {
                // RepositoryName 變更，結束目前區塊並開始新區塊
                blocks.Add(new RepositoryBlock
                {
                    HeaderRowIndex = currentBlockHeaderIndex,
                    EndRowIndex = i - 1,
                    RepositoryName = currentRepositoryName!,
                });

                // 新區塊的標題列
                currentBlockHeaderIndex = i;
                currentRepositoryName = effectiveRepositoryName;
            }
            // 否則繼續目前區塊
        }

        // 處理最後一個區塊
        if (currentBlockHeaderIndex >= 0 && currentRepositoryName != null)
        {
            blocks.Add(new RepositoryBlock
            {
                HeaderRowIndex = currentBlockHeaderIndex,
                EndRowIndex = sheetData.Count - 1,
                RepositoryName = currentRepositoryName,
            });
        }

        return blocks;
    }

    /// <summary>
    /// 對單一區塊內的資料列進行排序（跳過區塊標題列）。
    /// 排序規則: Team → MergedAt (空排最後)。
    /// </summary>
    /// <param name="sheetData">工作表資料。</param>
    /// <param name="block">區塊資訊。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>排序後的原始 Row 編號清單 (1-based index)。</returns>
    private List<int> SortBlockRows(
        IList<IList<object>> sheetData,
        RepositoryBlock block,
        GoogleSheetColumnMapping columnMapping)
    {
        // 收集區塊內的資料列（跳過區塊標題列）
        var rowsWithData = new List<(int RowNumber, SheetRowData ParsedData)>();

        // 從 DataStartRowIndex 開始，跳過區塊標題列 (HeaderRowIndex)
        for (var i = block.DataStartRowIndex; i <= block.EndRowIndex; i++)
        {
            var rowValues = sheetData[i];
            var rowNumber = i + 1; // 1-based
            var parsedData = _rowParser.ParseRow(rowValues, rowNumber, columnMapping);
            rowsWithData.Add((rowNumber, parsedData));
        }

        // 排序: Team → MergedAt (空排最後)
        var sortedRowNumbers = rowsWithData
            .OrderBy(x => _teamMappingService.GetTeamSortOrder(x.ParsedData.Team))
            .ThenBy(x => x.ParsedData.MergedAt == null ? 1 : 0)
            .ThenBy(x => x.ParsedData.MergedAt)
            .Select(x => x.RowNumber)
            .ToList();

        return sortedRowNumbers;
    }

    /// <summary>
    /// Repository 區塊資訊。
    /// </summary>
    private sealed class RepositoryBlock
    {
        /// <summary>
        /// 區塊標題列索引 (0-based)。
        /// 區塊的第一行是標題列，不參與排序。
        /// </summary>
        public int HeaderRowIndex { get; init; }

        /// <summary>
        /// 資料起始索引 (0-based)。
        /// 從區塊標題列的下一行開始。
        /// </summary>
        public int DataStartRowIndex => HeaderRowIndex + 1;

        /// <summary>
        /// 區塊結束索引 (0-based，含)。
        /// </summary>
        public int EndRowIndex { get; init; }

        /// <summary>
        /// 資料起始 Row 編號 (1-based)。
        /// 用於 API 呼叫。
        /// </summary>
        public int DataStartRowNumber => DataStartRowIndex + 1;

        /// <summary>
        /// 區塊結束 Row 編號 (1-based)。
        /// </summary>
        public int EndRowNumber => EndRowIndex + 1;

        /// <summary>
        /// Repository 名稱。
        /// </summary>
        public string RepositoryName { get; init; } = string.Empty;

        /// <summary>
        /// 資料列數量（不含區塊標題列）。
        /// </summary>
        public int DataRowCount => EndRowIndex - DataStartRowIndex + 1;

        /// <summary>
        /// 是否有資料列需要排序。
        /// 區塊至少要有 2 行資料才需要排序。
        /// </summary>
        public bool HasDataToSort => DataRowCount >= 2;
    }
}
