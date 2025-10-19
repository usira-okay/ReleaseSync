using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.Services;

/// <summary>
/// Work Item 服務介面
/// </summary>
public interface IWorkItemService
{
    /// <summary>
    /// 從 Branch 名稱解析並取得 Work Item
    /// </summary>
    /// <param name="branchName">Branch 名稱</param>
    /// <param name="includeParent">是否包含 Parent Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊,若無法解析或不存在則回傳 null</returns>
    Task<WorkItemInfo?> GetWorkItemFromBranchAsync(
        BranchName branchName,
        bool includeParent = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 Work Item ID 取得 Work Item
    /// </summary>
    /// <param name="workItemId">Work Item 識別碼</param>
    /// <param name="includeParent">是否包含 Parent Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊,若不存在則回傳 null</returns>
    Task<WorkItemInfo?> GetWorkItemAsync(
        WorkItemId workItemId,
        bool includeParent = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批次取得多個 Work Items
    /// </summary>
    /// <param name="workItemIds">Work Item 識別碼清單</param>
    /// <param name="includeParent">是否包含 Parent Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊清單</returns>
    Task<IEnumerable<WorkItemInfo>> GetWorkItemsAsync(
        IEnumerable<WorkItemId> workItemIds,
        bool includeParent = true,
        CancellationToken cancellationToken = default);
}
