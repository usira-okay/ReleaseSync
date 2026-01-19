namespace ReleaseSync.Domain.Models;

/// <summary>
/// Git Commit 差異結果模型
/// </summary>
public class CommitDiff
{
    /// <summary>
    /// Repository 識別 (例如: group/project 或 workspace/repo)
    /// </summary>
    public string RepositoryId { get; init; } = string.Empty;

    /// <summary>
    /// 來源分支名稱
    /// </summary>
    public string SourceBranch { get; init; } = string.Empty;

    /// <summary>
    /// 目標分支名稱
    /// </summary>
    public string TargetBranch { get; init; } = string.Empty;

    /// <summary>
    /// 差異的 Commit Hash 清單 (防禦性複製,確保 immutability)
    /// </summary>
    public IReadOnlyList<CommitHash> Commits { get; init; } = Array.Empty<CommitHash>();

    /// <summary>
    /// Commit 總數
    /// </summary>
    public int CommitCount => Commits.Count;

    /// <summary>
    /// 是否有 Commit 差異
    /// </summary>
    public bool HasCommits => Commits.Count > 0;

    /// <summary>
    /// 取得差異摘要說明
    /// </summary>
    public string GetSummary()
    {
        if (!HasCommits)
        {
            return $"[{RepositoryId}] {SourceBranch} 與 {TargetBranch} 之間無差異";
        }

        return $"[{RepositoryId}] {SourceBranch} 比 {TargetBranch} 多出 {CommitCount} 個 commit";
    }

    /// <summary>
    /// 建立空的 CommitDiff 實例
    /// </summary>
    public static CommitDiff Empty(string repositoryId, string sourceBranch, string targetBranch)
    {
        return new CommitDiff
        {
            RepositoryId = repositoryId,
            SourceBranch = sourceBranch,
            TargetBranch = targetBranch,
            Commits = Array.Empty<CommitHash>()
        };
    }
}
