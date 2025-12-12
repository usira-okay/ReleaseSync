// <copyright file="GoogleSheetRowParser.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;
using ReleaseSync.Application.Configuration;
using ReleaseSync.Application.Models;
using ReleaseSync.Application.Services;

namespace ReleaseSync.Infrastructure.GoogleSheet;

/// <summary>
/// Google Sheet Row 解析器實作。
/// 負責 Google Sheet 資料與 SheetRowData 之間的轉換。
/// </summary>
public class GoogleSheetRowParser : IGoogleSheetRowParser
{
    private readonly TimeSpan _displayTimeZoneOffset;

    /// <summary>
    /// 初始化 GoogleSheetRowParser。
    /// </summary>
    /// <param name="settings">Google Sheet 設定。</param>
    public GoogleSheetRowParser(IOptions<GoogleSheetSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _displayTimeZoneOffset = TimeSpan.FromHours(settings.Value.DisplayTimeZoneOffset);
    }

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
        var mergedAtText = GetCellValue(rowValues, columnMapping.MergedAtColumn);

        // 解析 Authors (換行分隔)
        var authors = ParseMultiLineValues(authorsText);

        // 解析 PR URLs (換行分隔)
        var pullRequestUrls = ParseMultiLineValues(pullRequestUrlsText);

        // 解析 AutoSync 標記
        var isAutoSync = autoSyncText.Equals("TRUE", StringComparison.OrdinalIgnoreCase);

        // 解析 MergedAt 時間 (將本地時間轉換回 UTC)
        var mergedAt = ParseDateTime(mergedAtText, _displayTimeZoneOffset);

        return new SheetRowData
        {
            RowNumber = rowNumber,
            UniqueKey = uniqueKey,
            RepositoryName = repositoryName,
            Feature = feature,
            Team = team,
            Authors = authors,
            PullRequestUrls = pullRequestUrls,
            MergedAt = mergedAt,
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
        SetCellValue(rowValues, columnMapping.MergedAtColumn, FormatDateTime(rowData.MergedAt, _displayTimeZoneOffset));
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
            columnMapping.MergedAtColumn,
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

    /// <summary>
    /// 解析日期時間字串，並將本地時間轉換為 UTC。
    /// 支援多種格式:
    /// - "yyyy-MM-dd (週) HH:mm" (本程式輸出格式，如 "2025-01-15 (三) 10:30")
    /// - "yyyy-MM-dd HH:mm:ss"
    /// - "yyyy-MM-dd"
    /// - "yyyy/MM/dd"
    /// - "yyyy-MM-ddTHH:mm:ss"
    /// Sheet 中儲存的是本地時間（經過時區轉換），讀取時需轉回 UTC 以確保時間比較正確。
    /// </summary>
    /// <param name="text">日期時間字串 (本地時間)。</param>
    /// <param name="timeZoneOffset">時區偏移量。</param>
    /// <returns>解析成功返回 UTC DateTime，否則返回 null。</returns>
    private static DateTime? ParseDateTime(string text, TimeSpan timeZoneOffset)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        DateTime localDateTime;

        // 嘗試解析本程式輸出的格式: "yyyy-MM-dd (週) HH:mm"
        // 先移除中文星期幾的部分
        var normalizedText = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\s*\([日一二三四五六]\)\s*",
            " ");

        if (DateTime.TryParse(normalizedText.Trim(), out localDateTime))
        {
            // 將本地時間轉換回 UTC (減去時區偏移)
            var utcDateTime = localDateTime.Subtract(timeZoneOffset);
            return DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        // 嘗試直接解析原始文字
        if (DateTime.TryParse(text, out localDateTime))
        {
            // 將本地時間轉換回 UTC (減去時區偏移)
            var utcDateTime = localDateTime.Subtract(timeZoneOffset);
            return DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        return null;
    }

    /// <summary>
    /// 格式化 DateTime 為字串，並轉換為指定時區。
    /// 使用格式: "yyyy-MM-dd (週) HH:mm"，例如 "2025-12-12 (五) 13:30"。
    /// </summary>
    /// <param name="dateTime">要格式化的日期時間 (UTC)。</param>
    /// <param name="timeZoneOffset">時區偏移量。</param>
    /// <returns>格式化後的字串，若為 null 則返回空字串。</returns>
    private static string FormatDateTime(DateTime? dateTime, TimeSpan timeZoneOffset)
    {
        if (dateTime == null)
        {
            return string.Empty;
        }

        // 將 UTC 時間轉換為指定時區
        var localDateTime = dateTime.Value.Add(timeZoneOffset);

        var chineseWeekDay = GetChineseWeekDay(localDateTime.DayOfWeek);
        return $"{localDateTime:yyyy-MM-dd} ({chineseWeekDay}) {localDateTime:HH:mm}";
    }

    /// <summary>
    /// 取得中文星期幾的簡稱。
    /// </summary>
    /// <param name="dayOfWeek">星期幾。</param>
    /// <returns>中文簡稱 (日、一、二、三、四、五、六)。</returns>
    private static string GetChineseWeekDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "日",
            DayOfWeek.Monday => "一",
            DayOfWeek.Tuesday => "二",
            DayOfWeek.Wednesday => "三",
            DayOfWeek.Thursday => "四",
            DayOfWeek.Friday => "五",
            DayOfWeek.Saturday => "六",
            _ => string.Empty,
        };
    }
}
