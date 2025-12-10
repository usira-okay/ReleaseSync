// <copyright file="SheetBlockReorderOperation.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Application.Models;

/// <summary>
/// Google Sheet 區塊重新排列操作值物件。
/// 表示單一區塊（相同 RepositoryName）的 rows 重新排列操作。
/// </summary>
public sealed record SheetBlockReorderOperation
{
    /// <summary>
    /// 區塊起始 Row 編號 (1-based index，含)。
    /// </summary>
    public int StartRowNumber { get; init; }

    /// <summary>
    /// 區塊結束 Row 編號 (1-based index，含)。
    /// </summary>
    public int EndRowNumber { get; init; }

    /// <summary>
    /// Repository 名稱（用於日誌和除錯）。
    /// </summary>
    public string RepositoryName { get; init; } = string.Empty;

    /// <summary>
    /// 排序後的原始 Row 編號清單 (1-based index)。
    /// 清單順序即為排序後的順序，每個元素是原始資料的 Row 編號。
    /// 例如：[5, 3, 4] 表示排序後順序為原本的 Row 5, Row 3, Row 4。
    /// </summary>
    public IReadOnlyList<int> SortedOriginalRowNumbers { get; init; } = Array.Empty<int>();

    /// <summary>
    /// 取得區塊包含的 Row 數量。
    /// </summary>
    public int RowCount => EndRowNumber - StartRowNumber + 1;

    /// <summary>
    /// 驗證操作資料是否有效。
    /// </summary>
    /// <returns>是否有效。</returns>
    public bool IsValid()
    {
        // StartRowNumber 必須大於 1 (跳過標題列)
        if (StartRowNumber <= 1)
        {
            return false;
        }

        // EndRowNumber 必須大於等於 StartRowNumber
        if (EndRowNumber < StartRowNumber)
        {
            return false;
        }

        // SortedOriginalRowNumbers 數量必須與 Row 數量相同
        if (SortedOriginalRowNumbers.Count != RowCount)
        {
            return false;
        }

        return true;
    }
}
