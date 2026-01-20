namespace ReleaseSync.Domain.Models;

/// <summary>
/// 定義 PR/MR 資料的抓取模式
/// </summary>
public enum FetchMode
{
    /// <summary>
    /// 使用時間範圍抓取（預設模式，向後相容）
    /// </summary>
    DateRange = 0,

    /// <summary>
    /// 使用 Release Branch 比對抓取
    /// </summary>
    ReleaseBranch = 1
}
