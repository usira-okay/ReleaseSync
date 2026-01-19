using ReleaseSync.Domain.Models;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// Commit Fetcher 配置
/// </summary>
public sealed record FetcherConfiguration
{
    /// <summary>
    /// Repository 識別 (專案路徑或名稱)
    /// </summary>
    public required string RepositoryId { get; init; }

    /// <summary>
    /// 抓取模式
    /// </summary>
    public required FetchMode FetchMode { get; init; }

    /// <summary>
    /// 目標分支 (TargetBranch)
    /// 用於與 Release Branch 比對
    /// </summary>
    public string? TargetBranch { get; init; }

    /// <summary>
    /// Release Branch 名稱
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 下一版 Release Branch 名稱 (用於歷史版本比對)
    /// </summary>
    public string? NextReleaseBranch { get; init; }

    /// <summary>
    /// 開始時間 (用於 DateRange 模式)
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// 結束時間 (用於 DateRange 模式)
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }
}
