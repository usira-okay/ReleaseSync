namespace ReleaseSync.Domain.Models;

/// <summary>
/// Commit 差異結果
/// </summary>
public sealed record CommitDiff
{
    /// <summary>
    /// Repository 識別 (專案路徑或名稱)
    /// </summary>
    public string RepositoryId { get; }

    /// <summary>
    /// 來源分支
    /// </summary>
    public string SourceBranch { get; }

    /// <summary>
    /// 目標分支
    /// </summary>
    public string TargetBranch { get; }

    /// <summary>
    /// 差異的 Commit Hash 清單 (目標分支有但來源分支沒有)
    /// </summary>
    public IReadOnlyList<CommitHash> Commits { get; }

    /// <summary>
    /// Commit 數量
    /// </summary>
    public int CommitCount => Commits.Count;

    /// <summary>
    /// 是否有差異
    /// </summary>
    public bool HasCommits => Commits.Count > 0;

    /// <summary>
    /// 建立 CommitDiff 實例
    /// </summary>
    /// <param name="repositoryId">Repository 識別</param>
    /// <param name="sourceBranch">來源分支</param>
    /// <param name="targetBranch">目標分支</param>
    /// <param name="commits">差異的 Commit Hash 清單</param>
    public CommitDiff(
        string repositoryId,
        string sourceBranch,
        string targetBranch,
        IEnumerable<CommitHash> commits)
    {
        if (string.IsNullOrWhiteSpace(repositoryId))
            throw new ArgumentException("Repository ID cannot be empty.", nameof(repositoryId));

        if (string.IsNullOrWhiteSpace(sourceBranch))
            throw new ArgumentException("Source branch cannot be empty.", nameof(sourceBranch));

        if (string.IsNullOrWhiteSpace(targetBranch))
            throw new ArgumentException("Target branch cannot be empty.", nameof(targetBranch));

        RepositoryId = repositoryId;
        SourceBranch = sourceBranch;
        TargetBranch = targetBranch;
        Commits = commits?.ToList() ?? new List<CommitHash>();
    }

    /// <summary>
    /// 取得摘要資訊
    /// </summary>
    public string GetSummary()
    {
        return $"Repository: {RepositoryId}, {SourceBranch}...{TargetBranch}: {CommitCount} commit(s)";
    }

    /// <summary>
    /// 取得短 Hash 清單
    /// </summary>
    public IEnumerable<string> GetShortHashes() => Commits.Select(c => c.ShortHash);
}
