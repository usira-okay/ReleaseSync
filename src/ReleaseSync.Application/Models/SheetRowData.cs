// <copyright file="SheetRowData.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Application.Models;

/// <summary>
/// Google Sheet row 資料值物件。
/// 表示 Sheet 中單一 row 的完整資料。
/// </summary>
public sealed record SheetRowData
{
    /// <summary>
    /// Row 編號 (1-based index)。
    /// </summary>
    public int RowNumber { get; init; }

    /// <summary>
    /// Unique Key (WorkItemId + RepositoryName)。
    /// 用於識別 Sheet 中的唯一 row。
    /// </summary>
    public string UniqueKey { get; init; } = string.Empty;

    /// <summary>
    /// Repository 名稱。
    /// </summary>
    public string RepositoryName { get; init; } = string.Empty;

    /// <summary>
    /// Feature 描述 (VSTS{ID} - {Title})。
    /// </summary>
    public string Feature { get; init; } = string.Empty;

    /// <summary>
    /// Feature 超連結 URL (Azure DevOps Work Item URL)。
    /// </summary>
    public string? FeatureUrl { get; init; }

    /// <summary>
    /// 上線團隊。
    /// </summary>
    public string Team { get; init; } = string.Empty;

    /// <summary>
    /// RD 負責人清單 (使用 HashSet 避免重複)。
    /// </summary>
    public HashSet<string> Authors { get; init; } = new();

    /// <summary>
    /// PR/MR 連結清單 (使用 HashSet 避免重複)。
    /// </summary>
    public HashSet<string> PullRequestUrls { get; init; } = new();

    public DateTime? MergedAt { get; init; }

    /// <summary>
    /// 驗證 row 資料是否有效。
    /// </summary>
    /// <returns>驗證是否通過。</returns>
    public bool IsValid()
    {
        // RowNumber 必須大於 0
        if (RowNumber <= 0)
        {
            return false;
        }

        // UniqueKey 不可為空
        if (string.IsNullOrWhiteSpace(UniqueKey))
        {
            return false;
        }

        // RepositoryName 不可為空
        if (string.IsNullOrWhiteSpace(RepositoryName))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 合併另一個 SheetRowData 的 Authors 和 PullRequestUrls。
    /// 用於當現有 row 與新資料 UK 相同時的資料合併。
    /// </summary>
    /// <param name="other">要合併的資料。</param>
    /// <returns>合併後的新 SheetRowData。</returns>
    public SheetRowData MergeWith(SheetRowData other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        // 合併 Authors
        var mergedAuthors = new HashSet<string>(Authors);
        foreach (var author in other.Authors)
        {
            mergedAuthors.Add(author);
        }

        // 合併 PullRequestUrls
        var mergedUrls = new HashSet<string>(PullRequestUrls);
        foreach (var url in other.PullRequestUrls)
        {
            mergedUrls.Add(url);
        }

        return this with
        {
            Authors = mergedAuthors,
            PullRequestUrls = mergedUrls,
        };
    }
}
