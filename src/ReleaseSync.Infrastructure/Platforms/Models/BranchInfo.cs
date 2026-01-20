namespace ReleaseSync.Infrastructure.Platforms.Models;

/// <summary>
/// 分支資訊
/// </summary>
public record BranchInfo
{
    /// <summary>
    /// 分支名稱
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 最新 Commit SHA
    /// </summary>
    public required string CommitSha { get; init; }

    /// <summary>
    /// 最新 Commit 日期
    /// </summary>
    public DateTimeOffset? CommitDate { get; init; }
}
