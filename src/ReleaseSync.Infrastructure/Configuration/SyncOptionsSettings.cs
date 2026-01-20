using ReleaseSync.Domain.Models;

namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// 全域同步選項設定
/// </summary>
public class SyncOptionsSettings
{
    /// <summary>
    /// 預設的 Release Branch 名稱
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 預設的開始日期
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 預設的結束日期
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 預設的抓取模式
    /// </summary>
    public FetchMode DefaultFetchMode { get; init; } = FetchMode.DateRange;
}
