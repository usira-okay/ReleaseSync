namespace ReleaseSync.Domain.Models;

/// <summary>
/// PR 資料抓取模式
/// </summary>
public enum FetchMode
{
    /// <summary>
    /// 使用時間範圍抓取
    /// </summary>
    DateRange,

    /// <summary>
    /// 使用 Release Branch 抓取
    /// </summary>
    ReleaseBranch
}
