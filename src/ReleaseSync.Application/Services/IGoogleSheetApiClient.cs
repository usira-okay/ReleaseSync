// <copyright file="IGoogleSheetApiClient.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using ReleaseSync.Application.Models;

namespace ReleaseSync.Application.Services;

/// <summary>
/// Google Sheets API 客戶端介面。
/// 封裝所有與 Google Sheets API 的互動。
/// </summary>
public interface IGoogleSheetApiClient
{
    /// <summary>
    /// 驗證 Service Account 憑證。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步任務。</returns>
    Task AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查指定的 Spreadsheet 是否存在。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>是否存在。</returns>
    Task<bool> SpreadsheetExistsAsync(string spreadsheetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查指定的工作表是否存在。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    /// <param name="sheetName">工作表名稱。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>是否存在。</returns>
    Task<bool> SheetExistsAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 讀取指定工作表的所有資料。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    /// <param name="sheetName">工作表名稱。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>工作表資料 (每個元素為一個 row)。</returns>
    Task<IList<IList<object>>> ReadSheetDataAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批次更新工作表資料。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    /// <param name="sheetName">工作表名稱。</param>
    /// <param name="operations">同步操作清單。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>更新的 row 數量。</returns>
    Task<int> BatchUpdateAsync(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<SheetSyncOperation> operations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 產生 Google Sheet URL。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    /// <returns>Google Sheet URL。</returns>
    string GenerateSpreadsheetUrl(string spreadsheetId);

    /// <summary>
    /// 批次重新排列指定區塊內的 rows。
    /// 此方法會將指定範圍內的 rows 依照提供的順序重新排列。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    /// <param name="sheetName">工作表名稱。</param>
    /// <param name="reorderOperations">重新排列操作清單，每個操作包含區塊的 row 範圍和排序後的資料。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>重新排列的 row 數量。</returns>
    Task<int> BatchReorderRowsAsync(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<SheetBlockReorderOperation> reorderOperations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken = default);
}
