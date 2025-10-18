using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.Repositories;

/// <summary>
/// Work Item Repository 介面
/// </summary>
public interface IWorkItemRepository
{
    /// <summary>
    /// 根據 Work Item ID 查詢 Work Item
    /// </summary>
    /// <param name="workItemId">Work Item 識別碼</param>
    /// <param name="includeParent">是否包含 Parent Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊,若不存在則回傳 null</returns>
    Task<WorkItemInfo> GetWorkItemAsync(
        WorkItemId workItemId,
        bool includeParent = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 批次查詢多個 Work Items
    /// </summary>
    /// <param name="workItemIds">Work Item 識別碼清單</param>
    /// <param name="includeParent">是否包含 Parent Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊清單</returns>
    Task<IEnumerable<WorkItemInfo>> GetWorkItemsAsync(
        IEnumerable<WorkItemId> workItemIds,
        bool includeParent = true,
        CancellationToken cancellationToken = default
    );
}
