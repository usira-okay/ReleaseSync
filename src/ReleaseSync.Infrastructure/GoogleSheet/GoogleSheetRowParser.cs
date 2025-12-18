// <copyright file="GoogleSheetRowParser.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using Google.Apis.Sheets.v4.Data;
using ReleaseSync.Application.Models;
using ReleaseSync.Application.Services;

namespace ReleaseSync.Infrastructure.GoogleSheet;

/// <summary>
/// Google Sheet Row 解析器實作。
/// 負責 Google Sheet 資料與 SheetRowData 之間的轉換。
/// </summary>
public class GoogleSheetRowParser : IGoogleSheetRowParser
{
    /// <inheritdoc/>
    public SheetRowData ParseRow(IList<object> rowValues, int rowNumber, GoogleSheetColumnMapping columnMapping)
    {
        ArgumentNullException.ThrowIfNull(rowValues);
        ArgumentNullException.ThrowIfNull(columnMapping);

        if (rowNumber <= 0)
        {
            throw new ArgumentException("Row 編號必須大於 0", nameof(rowNumber));
        }

        // 取得各欄位的值
        var uniqueKey = GetCellValue(rowValues, columnMapping.UniqueKeyColumn);
        var repositoryName = GetCellValue(rowValues, columnMapping.RepositoryNameColumn);
        var feature = GetCellValue(rowValues, columnMapping.FeatureColumn);
        var team = GetCellValue(rowValues, columnMapping.TeamColumn);
        var authorsText = GetCellValue(rowValues, columnMapping.AuthorsColumn);
        var pullRequestUrlsText = GetCellValue(rowValues, columnMapping.PullRequestUrlsColumn);
        var autoSyncText = GetCellValue(rowValues, columnMapping.AutoSyncColumn);

        // 解析 Authors (換行分隔)
        var authors = ParseMultiLineValues(authorsText);

        // 解析 PR URLs (換行分隔)
        var pullRequestUrls = ParseMultiLineValues(pullRequestUrlsText);

        // 解析 AutoSync 標記
        var isAutoSync = autoSyncText.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

        return new SheetRowData
        {
            RowNumber = rowNumber,
            UniqueKey = uniqueKey,
            RepositoryName = repositoryName,
            Feature = feature,
            Team = team,
            Authors = authors,
            PullRequestUrls = pullRequestUrls,
            IsAutoSync = isAutoSync,
        };
    }

    /// <inheritdoc/>
    public IList<object> ToRowValues(SheetRowData rowData, GoogleSheetColumnMapping columnMapping, IList<object>? existingRowValues = null)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(columnMapping);

        // 計算需要的欄位數量 (根據最大欄位位置)
        var maxColumn = GetMaxColumnIndex(columnMapping);
        var rowValues = new List<object>();

        // 初始化 row values (如果有現有值則複製)
        if (existingRowValues != null)
        {
            rowValues.AddRange(existingRowValues);
        }

        // 確保有足夠的欄位
        while (rowValues.Count <= maxColumn)
        {
            rowValues.Add(string.Empty);
        }

        // 設定各欄位的值
        SetCellValue(rowValues, columnMapping.UniqueKeyColumn, rowData.UniqueKey);
        SetCellValue(rowValues, columnMapping.RepositoryNameColumn, rowData.RepositoryName);
        SetCellValue(rowValues, columnMapping.FeatureColumn, rowData.Feature);
        SetCellValue(rowValues, columnMapping.TeamColumn, rowData.Team);
        SetCellValue(rowValues, columnMapping.AuthorsColumn, string.Join("\n", rowData.Authors.OrderBy(a => a)));
        SetCellValue(rowValues, columnMapping.PullRequestUrlsColumn, string.Join("\n", rowData.PullRequestUrls.OrderBy(u => u)));
        SetCellValue(rowValues, columnMapping.AutoSyncColumn, rowData.IsAutoSync ? "TRUE" : string.Empty);

        return rowValues;
    }

    /// <inheritdoc/>
    public CellData CreateHyperlinkCell(string displayText, string url)
    {
        if (string.IsNullOrWhiteSpace(displayText))
        {
            throw new ArgumentException("顯示文字不可為空", nameof(displayText));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL 不可為空", nameof(url));
        }

        return new CellData
        {
            UserEnteredValue = new ExtendedValue
            {
                FormulaValue = $"=HYPERLINK(\"{url}\", \"{displayText}\")",
            },
        };
    }

    /// <summary>
    /// 從 row values 中取得指定欄位的值。
    /// </summary>
    /// <param name="rowValues">Row 值清單。</param>
    /// <param name="columnLetter">欄位字母 (如 "A", "B", "AA")。</param>
    /// <returns>欄位值的字串表示。</returns>
    private static string GetCellValue(IList<object> rowValues, string columnLetter)
    {
        var columnIndex = ColumnLetterToIndex(columnLetter);
        if (columnIndex >= rowValues.Count)
        {
            return string.Empty;
        }

        var value = rowValues[columnIndex];
        return value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 設定 row values 中指定欄位的值。
    /// </summary>
    /// <param name="rowValues">Row 值清單。</param>
    /// <param name="columnLetter">欄位字母 (如 "A", "B", "AA")。</param>
    /// <param name="value">要設定的值。</param>
    private static void SetCellValue(IList<object> rowValues, string columnLetter, string value)
    {
        var columnIndex = ColumnLetterToIndex(columnLetter);
        if (columnIndex < rowValues.Count)
        {
            rowValues[columnIndex] = value;
        }
    }

    /// <summary>
    /// 將欄位字母轉換為索引 (0-based)。
    /// </summary>
    /// <param name="columnLetter">欄位字母 (如 "A", "B", "AA")。</param>
    /// <returns>0-based 索引。</returns>
    private static int ColumnLetterToIndex(string columnLetter)
    {
        if (string.IsNullOrWhiteSpace(columnLetter))
        {
            throw new ArgumentException("欄位字母不可為空", nameof(columnLetter));
        }

        var index = 0;
        foreach (var c in columnLetter.ToUpperInvariant())
        {
            if (!char.IsLetter(c))
            {
                throw new ArgumentException($"無效的欄位字母: {columnLetter}", nameof(columnLetter));
            }

            index = (index * 26) + (c - 'A' + 1);
        }

        return index - 1; // 轉換為 0-based
    }

    /// <summary>
    /// 取得欄位對應中最大的欄位索引。
    /// </summary>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>最大欄位索引 (0-based)。</returns>
    private static int GetMaxColumnIndex(GoogleSheetColumnMapping columnMapping)
    {
        var columns = new[]
        {
            columnMapping.RepositoryNameColumn,
            columnMapping.FeatureColumn,
            columnMapping.TeamColumn,
            columnMapping.AuthorsColumn,
            columnMapping.PullRequestUrlsColumn,
            columnMapping.UniqueKeyColumn,
            columnMapping.AutoSyncColumn,
        };

        return columns.Max(ColumnLetterToIndex);
    }

    /// <summary>
    /// 解析多行值 (換行分隔) 為 HashSet。
    /// </summary>
    /// <param name="text">包含換行分隔的文字。</param>
    /// <returns>解析後的 HashSet。</returns>
    private static HashSet<string> ParseMultiLineValues(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new HashSet<string>();
        }

        var values = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(v => v.Trim())
                         .Where(v => !string.IsNullOrEmpty(v));

        return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
    }
}
