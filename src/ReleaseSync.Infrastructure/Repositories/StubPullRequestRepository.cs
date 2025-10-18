using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Infrastructure.Repositories;

/// <summary>
/// Stub PR Repository (MVP)
/// </summary>
public class StubPullRequestRepository : IPullRequestRepository
{
    public Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string>? targetBranches = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<PullRequestInfo>());
    }
}
