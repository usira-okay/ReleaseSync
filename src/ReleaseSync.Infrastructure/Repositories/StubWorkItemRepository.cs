using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Infrastructure.Repositories;

/// <summary>
/// Stub Work Item Repository (MVP)
/// </summary>
public class StubWorkItemRepository : IWorkItemRepository
{
    public Task<WorkItemInfo> GetWorkItemAsync(
        WorkItemId workItemId,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MVP: Work Item 功能尚未實作");
    }

    public Task<IEnumerable<WorkItemInfo>> GetWorkItemsAsync(
        IEnumerable<WorkItemId> workItemIds,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("MVP: Work Item 功能尚未實作");
    }
}
