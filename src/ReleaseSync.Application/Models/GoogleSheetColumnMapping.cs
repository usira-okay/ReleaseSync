// <copyright file="GoogleSheetColumnMapping.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Application.Models;

/// <summary>
/// Google Sheet 欄位對應設定值物件。
/// 定義 PR/MR 資料在 Google Sheet 中對應的欄位位置。
/// </summary>
public sealed record GoogleSheetColumnMapping
{
    /// <summary>
    /// Repository 名稱欄位 (預設: "Z")。
    /// </summary>
    public string RepositoryNameColumn { get; init; } = "Z";

    /// <summary>
    /// Feature (Work Item) 欄位 (預設: "B")。
    /// </summary>
    public string FeatureColumn { get; init; } = "B";

    /// <summary>
    /// 上線團隊欄位 (預設: "D")。
    /// </summary>
    public string TeamColumn { get; init; } = "D";

    /// <summary>
    /// RD 負責人欄位 (預設: "W")。
    /// </summary>
    public string AuthorsColumn { get; init; } = "W";

    /// <summary>
    /// PR/MR 連結欄位 (預設: "X")。
    /// </summary>
    public string PullRequestUrlsColumn { get; init; } = "X";

    /// <summary>
    /// Unique Key 欄位 (預設: "Y")。
    /// 格式: {WorkItemId}{RepositoryName}。
    /// </summary>
    public string UniqueKeyColumn { get; init; } = "Y";

    /// <summary>
    /// 自動同步標記欄位 (預設: "F")。
    /// 用於標記資料是否為自動同步產生，值為 "TRUE"。
    /// </summary>
    public string AutoSyncColumn { get; init; } = "F";

    /// <summary>
    /// 驗證所有欄位設定是否有效。
    /// </summary>
    /// <returns>驗證是否通過。</returns>
    public bool IsValid()
    {
        // 驗證所有欄位都是有效的 A-Z 或 AA-ZZ 格式
        var columns = new[]
        {
            RepositoryNameColumn,
            FeatureColumn,
            TeamColumn,
            AuthorsColumn,
            PullRequestUrlsColumn,
            UniqueKeyColumn,
            AutoSyncColumn,
        };

        foreach (var column in columns)
        {
            if (!IsValidColumnLetter(column))
            {
                return false;
            }
        }

        // 驗證欄位不重複
        var uniqueColumns = columns.Distinct().ToList();
        return uniqueColumns.Count == columns.Length;
    }

    /// <summary>
    /// 驗證欄位字母是否有效 (A-Z 或 AA-ZZ)。
    /// </summary>
    /// <param name="column">欄位字母。</param>
    /// <returns>是否有效。</returns>
    private static bool IsValidColumnLetter(string column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return false;
        }

        // 支援 A-Z (單字母) 或 AA-ZZ (雙字母)
        if (column.Length == 1)
        {
            return char.IsLetter(column[0]) && char.IsUpper(column[0]);
        }

        if (column.Length == 2)
        {
            return char.IsLetter(column[0]) && char.IsUpper(column[0])
                && char.IsLetter(column[1]) && char.IsUpper(column[1]);
        }

        return false;
    }
}
