using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.Repositories;

/// <summary>
/// Pull Request Repository 介面
/// </summary>
public interface IPullRequestRepository
{
    /// <summary>
    /// 查詢指定時間範圍內的 Pull Requests
    /// </summary>
    /// <param name="projectName">專案名稱 (例如: owner/repo)</param>
    /// <param name="dateRange">時間範圍</param>
    /// <param name="targetBranches">目標分支清單 (若為空則查詢所有分支)</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Pull Request 清單</returns>
    Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string>? targetBranches = null,
        CancellationToken cancellationToken = default
    );
}
