using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.Repositories;

/// <summary>
/// Git Repository 操作介面
/// </summary>
public interface IGitRepository
{
    /// <summary>
    /// 檢查指定的分支是否存在
    /// </summary>
    /// <param name="repositoryId">Repository 識別 (專案路徑或名稱)</param>
    /// <param name="branchName">分支名稱</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分支存在回傳 true,否則回傳 false</returns>
    Task<bool> BranchExistsAsync(
        string repositoryId,
        string branchName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得兩個分支之間的 Commit 差異
    /// </summary>
    /// <param name="repositoryId">Repository 識別 (專案路徑或名稱)</param>
    /// <param name="sourceBranch">來源分支 (基準)</param>
    /// <param name="targetBranch">目標分支 (比較對象)</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Commit 差異結果 (目標分支有但來源分支沒有的 commits)</returns>
    Task<CommitDiff> GetCommitDiffAsync(
        string repositoryId,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default);
}
