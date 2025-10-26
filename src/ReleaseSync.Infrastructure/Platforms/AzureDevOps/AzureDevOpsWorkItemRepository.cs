using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;

namespace ReleaseSync.Infrastructure.Platforms.AzureDevOps;

/// <summary>
/// Azure DevOps Work Item Repository 實作
/// </summary>
public class AzureDevOpsWorkItemRepository : IWorkItemRepository
{
    private readonly AzureDevOpsApiClient _apiClient;
    private readonly ILogger<AzureDevOpsWorkItemRepository> _logger;
    private readonly ITeamMappingService _teamMappingService;

    public AzureDevOpsWorkItemRepository(
        AzureDevOpsApiClient apiClient,
        ILogger<AzureDevOpsWorkItemRepository> logger,
        ITeamMappingService teamMappingService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _teamMappingService = teamMappingService;
    }

    public async Task<WorkItemInfo> GetWorkItemAsync(
        WorkItemId workItemId,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "開始查詢 Work Item: {WorkItemId}, IncludeParent={IncludeParent}",
            workItemId.Value, includeParent);

        var workItem = await _apiClient.GetWorkItemAsync(
            workItemId.Value,
            includeRelations: includeParent,
            cancellationToken);

        if (workItem == null)
        {
            _logger.LogWarning(
                "Work Item 不存在: {WorkItemId}",
                workItemId.Value);
            return null!;
        }

        var workItemInfo = ConvertToWorkItemInfo(workItem);

        // 根據 TeamMapping 過濾 Work Item
        // 例外:Work Item ID 為 0 (佔位符) 時,跳過 TeamMapping 檢查
        if (_teamMappingService.IsFilteringEnabled())
        {
            if (workItemInfo.Id.IsPlaceholder)
            {
                _logger.LogInformation(
                    "Work Item ID 為 0 (佔位符),跳過 TeamMapping 檢查 - Type={Type}, Title={Title}",
                    workItemInfo.Type, workItemInfo.Title);
            }
            else if (!_teamMappingService.HasMapping(workItemInfo.OriginalTeamName))
            {
                _logger.LogWarning(
                    "Work Item {WorkItemId} 的團隊 '{OriginalTeam}' 不在 TeamMapping 中,已過濾 - Type={Type}, Title={Title}, DisplayTeam={DisplayTeam}",
                    workItemId.Value, workItemInfo.OriginalTeamName ?? "(null)", workItemInfo.Type, workItemInfo.Title, workItemInfo.Team);
                return null!;
            }
        }

        if (includeParent)
        {
            var parent = await _apiClient.GetParentWorkItemAsync(workItem, cancellationToken);
            if (parent != null)
            {
                var parentInfo = ConvertToWorkItemInfo(parent);

                // 檢查 Parent 的 TeamMapping (除非是佔位符)
                bool shouldIncludeParent = true;
                if (_teamMappingService.IsFilteringEnabled())
                {
                    if (parentInfo.Id.IsPlaceholder)
                    {
                        _logger.LogDebug(
                            "Parent Work Item ID 為 0 (佔位符),跳過 TeamMapping 檢查 - WorkItemId={WorkItemId}, ParentId={ParentId}",
                            workItemId.Value, parent.Id);
                    }
                    else if (!_teamMappingService.HasMapping(parentInfo.OriginalTeamName))
                    {
                        _logger.LogWarning(
                            "Work Item {WorkItemId} 的 Parent {ParentId} 團隊 '{OriginalTeam}' 不在 TeamMapping 中,嘗試向上查找 - Type={Type}, Title={Title}, DisplayTeam={DisplayTeam}",
                            workItemId.Value, parent.Id, parentInfo.OriginalTeamName ?? "(null)", parentInfo.Type, parentInfo.Title, parentInfo.Team);
                        shouldIncludeParent = false;
                    }
                }

                if (shouldIncludeParent)
                {
                    // 只保留 User Story 等級的 Parent
                    if (parentInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
                    {
                        workItemInfo.ParentWorkItem = parentInfo;
                        _logger.LogDebug(
                            "Work Item {WorkItemId} 的 Parent User Story: {ParentId} - {ParentTitle}, Team={Team}",
                            workItemId.Value, parent.Id, parentInfo.Title, parentInfo.Team);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Work Item {WorkItemId} 的 Parent 類型為 '{ParentType}',不是 User Story,嘗試向上查找",
                            workItemId.Value, parentInfo.Type);

                        // 如果 Parent 不是 User Story,繼續向上查找
                        workItemInfo.ParentWorkItem = await FindParentUserStoryAsync(parent, cancellationToken);
                    }
                }
                else
                {
                    // Parent 不在 TeamMapping 中,嘗試向上查找
                    _logger.LogDebug(
                        "Work Item {WorkItemId} 的 Parent 不在 TeamMapping 中,嘗試向上查找更高層級的 Parent",
                        workItemId.Value);
                    workItemInfo.ParentWorkItem = await FindParentUserStoryAsync(parent, cancellationToken);
                }
            }
        }

        _logger.LogDebug(
            "成功查詢 Work Item: {WorkItemId}, Type={Type}, State={State}, Team={Team}",
            workItemId.Value, workItemInfo.Type, workItemInfo.State, workItemInfo.Team);

        return workItemInfo;
    }

    public async Task<IEnumerable<WorkItemInfo>> GetWorkItemsAsync(
        IEnumerable<WorkItemId> workItemIds,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        var ids = workItemIds.Select(x => x.Value).ToList();

        _logger.LogDebug(
            "開始批次查詢 Work Items: Count={Count}, IncludeParent={IncludeParent}",
            ids.Count, includeParent);

        var workItems = await _apiClient.GetWorkItemsAsync(
            ids,
            includeRelations: includeParent,
            cancellationToken);

        var workItemList = workItems.ToList();
        var result = new List<WorkItemInfo>();
        var filteredCount = 0;

        foreach (var workItem in workItemList)
        {
            var workItemInfo = ConvertToWorkItemInfo(workItem);

            // 根據 TeamMapping 過濾 Work Item
            // 例外:Work Item ID 為 0 (佔位符) 時,跳過 TeamMapping 檢查
            if (_teamMappingService.IsFilteringEnabled())
            {
                if (workItemInfo.Id.IsPlaceholder)
                {
                    _logger.LogInformation(
                        "Work Item ID 為 0 (佔位符),跳過 TeamMapping 檢查 - Type={Type}, Title={Title}",
                        workItemInfo.Type, workItemInfo.Title);
                }
                else if (!_teamMappingService.HasMapping(workItemInfo.OriginalTeamName))
                {
                    _logger.LogWarning(
                        "Work Item {WorkItemId} 的團隊 '{OriginalTeam}' 不在 TeamMapping 中,已過濾 - Type={Type}, Title={Title}, DisplayTeam={DisplayTeam}",
                        workItem.Id, workItemInfo.OriginalTeamName ?? "(null)", workItemInfo.Type, workItemInfo.Title, workItemInfo.Team);
                    filteredCount++;
                    continue; // 跳過此 Work Item
                }
            }

            if (includeParent)
            {
                var parent = await _apiClient.GetParentWorkItemAsync(workItem, cancellationToken);
                if (parent != null)
                {
                    var parentInfo = ConvertToWorkItemInfo(parent);

                    // 只保留 User Story 等級的 Parent
                    if (parentInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
                    {
                        workItemInfo.ParentWorkItem = parentInfo;
                    }
                    else
                    {
                        // 如果 Parent 不是 User Story,繼續向上查找
                        workItemInfo.ParentWorkItem = await FindParentUserStoryAsync(parent, cancellationToken);
                    }
                }
            }

            result.Add(workItemInfo);
        }

        // 記錄過濾統計
        if (_teamMappingService.IsFilteringEnabled())
        {
            _logger.LogInformation(
                "根據 TeamMapping 過濾 {FilteredCount} 筆 Work Item (總共 {TotalCount} 筆,保留 {RetainedCount} 筆)",
                filteredCount, workItemList.Count, result.Count);
        }
        else
        {
            _logger.LogInformation(
                "成功批次查詢 Work Items: Count={Count} (TeamMapping 為空,向後相容模式)",
                result.Count);
        }

        return result;
    }

    /// <summary>
    /// 轉換 Azure DevOps WorkItem 到 Domain WorkItemInfo
    /// </summary>
    private WorkItemInfo ConvertToWorkItemInfo(WorkItem workItem)
    {
        // 從 AreaPath 提取團隊名稱
        var areaPath = GetFieldValue(workItem, "System.AreaPath");
        var originalTeamName = ExtractTeamFromAreaPath(areaPath);

        // 使用 TeamMappingService 取得顯示名稱
        var teamDisplayName = _teamMappingService.GetDisplayName(originalTeamName);

        return new WorkItemInfo
        {
            Id = new WorkItemId((int)workItem.Id!),
            Title = GetFieldValue(workItem, "System.Title") ?? "Untitled",
            Type = GetFieldValue(workItem, "System.WorkItemType") ?? "Unknown",
            State = GetFieldValue(workItem, "System.State") ?? "Unknown",
            Url = workItem.Url,
            AssignedTo = GetAssignedToValue(workItem),
            CreatedAt = GetDateTimeField(workItem, "System.CreatedDate"),
            UpdatedAt = GetDateTimeField(workItem, "System.ChangedDate"),
            OriginalTeamName = originalTeamName,  // 保存原始團隊名稱 (用於過濾)
            Team = teamDisplayName                 // 保存顯示名稱 (用於輸出)
        };
    }

    /// <summary>
    /// 從 Area Path 提取團隊名稱
    /// </summary>
    /// <param name="areaPath">Area Path 字串,例如: "ProjectName\TeamName\SubArea"</param>
    /// <returns>團隊名稱,若無法提取則返回 null</returns>
    private string? ExtractTeamFromAreaPath(string? areaPath)
    {
        if (string.IsNullOrWhiteSpace(areaPath))
        {
            return null;
        }

        // Area Path 格式通常為: "ProjectName\TeamName" 或 "ProjectName\TeamName\SubArea"
        // 提取第二層級作為團隊名稱
        var parts = areaPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2)
        {
            return parts[1]; // 返回 TeamName
        }

        // 如果只有一層,無法判斷團隊
        _logger.LogDebug(
            "無法從 Area Path 提取團隊名稱: {AreaPath}",
            areaPath);

        return null;
    }

    /// <summary>
    /// 取得欄位值
    /// </summary>
    private static string? GetFieldValue(WorkItem workItem, string fieldName)
    {
        if (workItem.Fields == null || !workItem.Fields.ContainsKey(fieldName))
        {
            return null;
        }

        return workItem.Fields[fieldName]?.ToString();
    }

    /// <summary>
    /// 取得 AssignedTo 欄位值
    /// </summary>
    private static string? GetAssignedToValue(WorkItem workItem)
    {
        var assignedTo = GetFieldValue(workItem, "System.AssignedTo");

        if (string.IsNullOrWhiteSpace(assignedTo))
        {
            return null;
        }

        // Azure DevOps 回傳格式可能是 "Display Name <email@example.com>"
        // 或只有名稱,這裡簡單處理
        return assignedTo;
    }

    /// <summary>
    /// 取得日期時間欄位
    /// </summary>
    private static DateTime GetDateTimeField(WorkItem workItem, string fieldName)
    {
        var value = GetFieldValue(workItem, fieldName);

        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime.ToUniversalTime();
        }

        return DateTime.UtcNow;
    }

    /// <summary>
    /// 遞迴查找 Parent User Story
    /// </summary>
    /// <param name="workItem">當前 Work Item</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="maxDepth">最大遞迴深度(預設 5 層)</param>
    /// <returns>User Story 等級的 Parent,若找不到則返回 null</returns>
    private async Task<WorkItemInfo?> FindParentUserStoryAsync(
        WorkItem workItem,
        CancellationToken cancellationToken,
        int maxDepth = 5)
    {
        if (maxDepth <= 0)
        {
            _logger.LogWarning(
                "達到最大遞迴深度,停止查找 Parent User Story - WorkItem: {WorkItemId}",
                workItem.Id);
            return null;
        }

        var parent = await _apiClient.GetParentWorkItemAsync(workItem, cancellationToken);
        if (parent == null)
        {
            _logger.LogDebug(
                "Work Item {WorkItemId} 沒有 Parent,停止查找",
                workItem.Id);
            return null;
        }

        var parentInfo = ConvertToWorkItemInfo(parent);

        _logger.LogDebug(
            "查找 Parent: {ParentId} - Type={ParentType}, Title={ParentTitle}, Team={Team} (剩餘深度: {RemainingDepth})",
            parent.Id, parentInfo.Type, parentInfo.Title, parentInfo.Team, maxDepth);

        // 檢查 TeamMapping (除非是佔位符)
        if (_teamMappingService.IsFilteringEnabled())
        {
            if (parentInfo.Id.IsPlaceholder)
            {
                _logger.LogDebug(
                    "Parent Work Item ID 為 0 (佔位符),跳過 TeamMapping 檢查 - ParentId={ParentId}",
                    parent.Id);
            }
            else if (!_teamMappingService.HasMapping(parentInfo.OriginalTeamName))
            {
                _logger.LogWarning(
                    "Parent Work Item {ParentId} 的團隊 '{OriginalTeam}' 不在 TeamMapping 中,已過濾 - Type={Type}, Title={Title}, DisplayTeam={DisplayTeam}",
                    parent.Id, parentInfo.OriginalTeamName ?? "(null)", parentInfo.Type, parentInfo.Title, parentInfo.Team);

                // 繼續向上查找,也許上層的 Parent 在 TeamMapping 中
                return await FindParentUserStoryAsync(parent, cancellationToken, maxDepth - 1);
            }
        }

        if (parentInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "找到 User Story Parent: {ParentId} - {ParentTitle}, Team={Team}",
                parent.Id, parentInfo.Title, parentInfo.Team);
            return parentInfo;
        }

        // 繼續向上查找
        return await FindParentUserStoryAsync(parent, cancellationToken, maxDepth - 1);
    }
}
