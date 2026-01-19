using ReleaseSync.Domain.Models;

namespace ReleaseSync.Application.Models;

/// <summary>
/// Commit Fetcher 配置模型
/// </summary>
public class FetcherConfiguration
{
    /// <summary>
    /// Repository 識別 (例如: group/project 或 workspace/repo)
    /// </summary>
    public required string RepositoryId { get; init; }

    /// <summary>
    /// 抓取模式
    /// </summary>
    public required FetchMode FetchMode { get; init; }

    /// <summary>
    /// 目標分支名稱
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// Release Branch 名稱 (當 FetchMode = ReleaseBranch 時必填)
    /// </summary>
    public ReleaseBranchName? ReleaseBranch { get; init; }

    /// <summary>
    /// 下一版 Release Branch 名稱 (當比對舊版 Release 時使用)
    /// </summary>
    public ReleaseBranchName? NextReleaseBranch { get; init; }

    /// <summary>
    /// 時間範圍 (當 FetchMode = DateRange 時必填)
    /// </summary>
    public DateRange? DateRange { get; init; }
}
