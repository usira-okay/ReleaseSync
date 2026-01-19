namespace ReleaseSync.Domain.Models;

/// <summary>
/// PR 資料抓取模式
/// </summary>
public enum FetchMode
{
    /// <summary>
    /// 使用開始/結束時間抓取
    /// </summary>
    DateRange,

    /// <summary>
    /// 使用 Release Branch 抓取
    /// </summary>
    ReleaseBranch
}
