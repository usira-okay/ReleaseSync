namespace ReleaseSync.Infrastructure.Platforms.Models;

/// <summary>
/// 分支比對結果
/// </summary>
public record BranchCompareResult
{
    /// <summary>
    /// 起始分支
    /// </summary>
    public required string FromBranch { get; init; }

    /// <summary>
    /// 結束分支
    /// </summary>
    public required string ToBranch { get; init; }

    /// <summary>
    /// 差異的 Commit 清單
    /// </summary>
    public IReadOnlyList<CommitInfo> Commits { get; init; } = Array.Empty<CommitInfo>();

    /// <summary>
    /// 差異的 Commit SHA 清單（簡化版）
    /// </summary>
    public IEnumerable<string> CommitShas => Commits.Select(c => c.Sha);
}
