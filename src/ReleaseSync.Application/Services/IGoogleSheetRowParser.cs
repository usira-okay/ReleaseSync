// <copyright file="IGoogleSheetRowParser.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using ReleaseSync.Application.Models;

namespace ReleaseSync.Application.Services;

/// <summary>
/// Google Sheet Row 解析器介面。
/// 負責將 Google Sheet API 回傳的原始資料轉換為 SheetRowData。
/// </summary>
public interface IGoogleSheetRowParser
{
    /// <summary>
    /// 解析 Google Sheet 的 row 資料。
    /// </summary>
    /// <param name="rowValues">Google Sheet API 回傳的 row 值清單。</param>
    /// <param name="rowNumber">Row 編號 (1-based)。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>解析後的 SheetRowData。</returns>
    SheetRowData ParseRow(IList<object> rowValues, int rowNumber, GoogleSheetColumnMapping columnMapping);

    /// <summary>
    /// 將 SheetRowData 轉換為 Google Sheet API 所需的 row 值清單。
    /// </summary>
    /// <param name="rowData">要轉換的 row 資料。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <param name="existingRowValues">現有的 row 值 (用於保留未對應的欄位)。</param>
    /// <returns>Google Sheet API 所需的 row 值清單。</returns>
    IList<object> ToRowValues(SheetRowData rowData, GoogleSheetColumnMapping columnMapping, IList<object>? existingRowValues = null);
}
