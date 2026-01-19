using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.Repositories;

/// <summary>
/// Git Repository 介面,用於執行 Git 操作
/// </summary>
public interface IGitRepository
{
    /// <summary>
    /// 取得兩個分支之間的 Commit 差異
    /// </summary>
    /// <param name="repositoryId">Repository 識別 (例如: group/project 或 workspace/repo)</param>
    /// <param name="sourceBranch">來源分支</param>
    /// <param name="targetBranch">目標分支</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>來源分支有但目標分支沒有的 Commit 清單</returns>
    Task<CommitDiff> GetCommitDiffAsync(
        string repositoryId,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 檢查指定的分支是否存在
    /// </summary>
    /// <param name="repositoryId">Repository 識別</param>
    /// <param name="branchName">分支名稱</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>如果分支存在則返回 true</returns>
    Task<bool> BranchExistsAsync(
        string repositoryId,
        string branchName,
        CancellationToken cancellationToken = default
    );
}
