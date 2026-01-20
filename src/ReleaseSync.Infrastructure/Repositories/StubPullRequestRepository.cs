using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Infrastructure.Repositories;

/// <summary>
/// Stub PR Repository (MVP)
/// </summary>
public class StubPullRequestRepository : IPullRequestRepository
{
    /// <summary>
    /// 取得指定時間範圍內的 Pull Requests
    /// </summary>
    public Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string>? targetBranches = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<PullRequestInfo>());
    }

    /// <summary>
    /// 根據 Release Branch 比對取得待發布的 Pull Requests
    /// </summary>
    public Task<IEnumerable<PullRequestInfo>> GetByReleaseBranchAsync(
        string projectName,
        string releaseBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<PullRequestInfo>());
    }
}
