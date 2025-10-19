using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.Services;

/// <summary>
/// Work Item ID 解析服務介面
/// </summary>
public interface IWorkItemIdParser
{
    /// <summary>
    /// 從 Branch 名稱解析 Work Item ID
    /// </summary>
    /// <param name="branchName">Branch 名稱</param>
    /// <returns>解析出的 Work Item ID,若無法解析則回傳 null</returns>
    WorkItemId ParseWorkItemId(BranchName branchName);

    /// <summary>
    /// 嘗試從 Branch 名稱解析 Work Item ID
    /// </summary>
    /// <param name="branchName">Branch 名稱</param>
    /// <param name="workItemId">解析出的 Work Item ID</param>
    /// <returns>是否成功解析</returns>
    bool TryParseWorkItemId(BranchName branchName, out WorkItemId workItemId);
}
