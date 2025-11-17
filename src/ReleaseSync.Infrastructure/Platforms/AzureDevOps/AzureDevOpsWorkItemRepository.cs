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
        var workItem = await _apiClient.GetWorkItemAsync(
            workItemId.Value,
            includeRelations: true,
            cancellationToken);

        if (workItem == null)
        {
            _logger.LogWarning("Work Item 不存在: {WorkItemId}", workItemId.Value);
            return null!;
        }

        var workItemInfo = ConvertToWorkItemInfo(workItem);

        // 如果當前 Work Item 不是 User Story,向上查找 User Story
        if (!workItemInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Work Item {WorkItemId} 類型為 {Type},非 User Story,向上查找 Parent User Story",
                workItemId.Value, workItemInfo.Type);

            var userStory = await FindParentUserStoryAsync(workItem, cancellationToken);

            if (userStory == null)
            {
                _logger.LogWarning(
                    "Work Item {WorkItemId} (Type={Type}) 無法找到 Parent User Story,回傳原始 Work Item",
                    workItemId.Value, workItemInfo.Type);
                // 即使找不到 User Story,仍然需要進行 TeamMapping 過濾
                if (!ShouldIncludeWorkItem(workItemInfo, workItemId))
                {
                    return null!;
                }
                return workItemInfo;
            }

            _logger.LogInformation(
                "Work Item {WorkItemId} 找到 Parent User Story: {UserStoryId} - {UserStoryTitle}",
                workItemId.Value, userStory.Id.Value, userStory.Title);

            workItemInfo = userStory;
        }

        // 根據 TeamMapping 過濾 Work Item
        if (!ShouldIncludeWorkItem(workItemInfo, workItemId))
        {
            return null!;
        }

        // 取得 User Story 的 Parent (Feature/Epic 等更高階層)
        if (includeParent)
        {
            // 重新取得當前 workItemInfo 對應的完整 WorkItem,因為 workItemInfo 可能已經被替換為 Parent User Story
            var currentWorkItem = await _apiClient.GetWorkItemAsync(
                workItemInfo.Id.Value,
                includeRelations: true,
                cancellationToken);

            if (currentWorkItem != null)
            {
                workItemInfo.ParentWorkItem = await GetParentWorkItemAsync(currentWorkItem, workItemInfo.Id, cancellationToken);
            }
        }

        return workItemInfo;
    }

    private bool ShouldIncludeWorkItem(WorkItemInfo workItemInfo, WorkItemId workItemId)
    {
        if (!_teamMappingService.IsFilteringEnabled())
        {
            return true;
        }

        if (workItemInfo.Id.IsPlaceholder)
        {
            return true;
        }

        if (_teamMappingService.HasMapping(workItemInfo.OriginalTeamName))
        {
            return true;
        }

        _logger.LogWarning(
            "Work Item {WorkItemId} 的團隊 '{OriginalTeam}' 不在 TeamMapping 中,已過濾",
            workItemId.Value, workItemInfo.OriginalTeamName ?? "(null)");

        return false;
    }

    private async Task<WorkItemInfo?> GetParentWorkItemAsync(
        WorkItem workItem,
        WorkItemId workItemId,
        CancellationToken cancellationToken)
    {
        var parent = await _apiClient.GetParentWorkItemAsync(workItem, cancellationToken);
        if (parent == null)
        {
            return null;
        }

        var parentInfo = ConvertToWorkItemInfo(parent);

        // 檢查 Parent 的 TeamMapping
        if (!ShouldIncludeParentWorkItem(parentInfo, workItemId, parent.Id))
        {
            return await FindParentUserStoryAsync(parent, cancellationToken);
        }

        // 只保留 User Story 等級的 Parent
        if (parentInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
        {
            return parentInfo;
        }

        // 如果 Parent 不是 User Story,繼續向上查找
        return await FindParentUserStoryAsync(parent, cancellationToken);
    }

    private bool ShouldIncludeParentWorkItem(WorkItemInfo parentInfo, WorkItemId workItemId, int? parentId)
    {
        if (!_teamMappingService.IsFilteringEnabled())
        {
            return true;
        }

        if (parentInfo.Id.IsPlaceholder)
        {
            _logger.LogInformation(
                "Parent Work Item ID 為 0 (佔位符),跳過 TeamMapping 檢查 - WorkItemId={WorkItemId}, ParentId={ParentId}",
                workItemId.Value, parentId);
            return true;
        }

        if (_teamMappingService.HasMapping(parentInfo.OriginalTeamName))
        {
            return true;
        }

        _logger.LogWarning(
            "Work Item {WorkItemId} 的 Parent {ParentId} 團隊 '{OriginalTeam}' 不在 TeamMapping 中,嘗試向上查找 - Type={Type}, Title={Title}, DisplayTeam={DisplayTeam}",
            workItemId.Value, parentId, parentInfo.OriginalTeamName ?? "(null)", parentInfo.Type, parentInfo.Title, parentInfo.Team);

        return false;
    }

    public async Task<IEnumerable<WorkItemInfo>> GetWorkItemsAsync(
        IEnumerable<WorkItemId> workItemIds,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        var ids = workItemIds.Select(x => x.Value).ToList();

        var workItems = await _apiClient.GetWorkItemsAsync(
            ids,
            includeRelations: includeParent,
            cancellationToken);

        var workItemList = workItems.ToList();
        var result = new List<WorkItemInfo>();
        var filteredCount = 0;

        foreach (var workItem in workItemList)
        {
            var workItemInfo = await ProcessWorkItemAsync(workItem, includeParent, cancellationToken);

            if (workItemInfo == null)
            {
                filteredCount++;
                continue;
            }

            result.Add(workItemInfo);
        }

        if (filteredCount > 0 && _teamMappingService.IsFilteringEnabled())
        {
            _logger.LogInformation(
                "批次查詢完成 - 過濾 {FilteredCount} 筆, 保留 {RetainedCount} 筆 Work Item",
                filteredCount, result.Count);
        }

        return result;
    }

    private async Task<WorkItemInfo?> ProcessWorkItemAsync(
        WorkItem workItem,
        bool includeParent,
        CancellationToken cancellationToken)
    {
        var workItemInfo = ConvertToWorkItemInfo(workItem);

        // 如果當前 Work Item 不是 User Story,向上查找 User Story
        if (!workItemInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "批次查詢 - Work Item {WorkItemId} 類型為 {Type},非 User Story,向上查找 Parent User Story",
                workItem.Id, workItemInfo.Type);

            var userStory = await FindParentUserStoryAsync(workItem, cancellationToken);

            if (userStory == null)
            {
                _logger.LogWarning(
                    "批次查詢 - Work Item {WorkItemId} (Type={Type}) 無法找到 Parent User Story,回傳原始 Work Item",
                    workItem.Id, workItemInfo.Type);
                return workItemInfo;
            }

            _logger.LogInformation(
                "批次查詢 - Work Item {WorkItemId} 找到 Parent User Story: {UserStoryId} - {UserStoryTitle}",
                workItem.Id, userStory.Id.Value, userStory.Title);

            workItemInfo = userStory;
        }

        // 根據 TeamMapping 過濾 Work Item
        if (_teamMappingService.IsFilteringEnabled())
        {
            if (!workItemInfo.Id.IsPlaceholder &&
                !_teamMappingService.HasMapping(workItemInfo.OriginalTeamName))
            {
                return null;
            }
        }

        if (includeParent)
        {
            workItemInfo.ParentWorkItem = await GetBatchParentWorkItemAsync(workItem, cancellationToken);
        }

        return workItemInfo;
    }

    private async Task<WorkItemInfo?> GetBatchParentWorkItemAsync(
        WorkItem workItem,
        CancellationToken cancellationToken)
    {
        var parent = await _apiClient.GetParentWorkItemAsync(workItem, cancellationToken);
        if (parent == null)
        {
            return null;
        }

        var parentInfo = ConvertToWorkItemInfo(parent);

        // 只保留 User Story 等級的 Parent
        if (parentInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
        {
            return parentInfo;
        }

        // 如果 Parent 不是 User Story,繼續向上查找
        return await FindParentUserStoryAsync(parent, cancellationToken);
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

        // 將 API URL 轉換為網頁 URL
        var webUrl = ConvertApiUrlToWebUrl(workItem.Url, (int)workItem.Id!);

        return new WorkItemInfo
        {
            Id = new WorkItemId((int)workItem.Id!),
            Title = GetFieldValue(workItem, "System.Title") ?? "Untitled",
            Type = GetFieldValue(workItem, "System.WorkItemType") ?? "Unknown",
            State = GetFieldValue(workItem, "System.State") ?? "Unknown",
            Url = webUrl,
            AssignedTo = GetAssignedToValue(workItem),
            CreatedAt = GetDateTimeField(workItem, "System.CreatedDate"),
            UpdatedAt = GetDateTimeField(workItem, "System.ChangedDate"),
            OriginalTeamName = originalTeamName,  // 保存原始團隊名稱 (用於過濾)
            Team = teamDisplayName                 // 保存顯示名稱 (用於輸出)
        };
    }

    /// <summary>
    /// 將 Azure DevOps API URL 轉換為網頁 URL
    /// </summary>
    /// <param name="apiUrl">API URL,例如: https://dev.azure.com/{organization}/_apis/wit/workItems/{id}</param>
    /// <param name="workItemId">Work Item ID</param>
    /// <returns>網頁 URL,例如: https://dev.azure.com/{organization}/{project}/_workitems/edit/{id}</returns>
    private string? ConvertApiUrlToWebUrl(string? apiUrl, int workItemId)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return null;
        }

        try
        {
            // API URL 格式: https://dev.azure.com/{organization}/_apis/wit/workItems/{id}
            // 網頁 URL 格式: https://dev.azure.com/{organization}/{project}/_workitems/edit/{id}

            var uri = new Uri(apiUrl);
            var organization = uri.Segments.Length > 1 ? uri.Segments[1].TrimEnd('/') : null;

            if (string.IsNullOrWhiteSpace(organization))
            {
                _logger.LogWarning(
                    "無法從 API URL 提取 organization: {ApiUrl}",
                    apiUrl);
                return apiUrl; // 回傳原始 URL
            }

            // 從 API URL 中提取 project (如果存在於 URL 參數中)
            // 注意: Work Item 的 API URL 通常不包含 project,需要從 TeamProject 欄位取得
            // 但為了簡化,我們使用 organization 層級的 URL
            // 格式: https://dev.azure.com/{organization}/_workitems/edit/{id}
            var webUrl = $"{uri.Scheme}://{uri.Host}/{organization}/_workitems/edit/{workItemId}";

            _logger.LogInformation(
                "轉換 Work Item URL: API={ApiUrl} -> Web={WebUrl}",
                apiUrl, webUrl);

            return webUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "轉換 Work Item URL 失敗,使用原始 URL: {ApiUrl}",
                apiUrl);
            return apiUrl;
        }
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
        _logger.LogInformation(
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
            return null;
        }

        var parentInfo = ConvertToWorkItemInfo(parent);

        // 檢查 TeamMapping 是否允許此 Parent
        if (!ShouldIncludeParentInSearch(parentInfo, parent.Id))
        {
            // 繼續向上查找,也許上層的 Parent 在 TeamMapping 中
            return await FindParentUserStoryAsync(parent, cancellationToken, maxDepth - 1);
        }

        // 如果是 User Story,直接返回
        if (parentInfo.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
        {
            return parentInfo;
        }

        // 繼續向上查找
        return await FindParentUserStoryAsync(parent, cancellationToken, maxDepth - 1);
    }

    private bool ShouldIncludeParentInSearch(WorkItemInfo parentInfo, int? parentId)
    {
        if (!_teamMappingService.IsFilteringEnabled())
        {
            return true;
        }

        if (parentInfo.Id.IsPlaceholder)
        {
            return true;
        }

        if (_teamMappingService.HasMapping(parentInfo.OriginalTeamName))
        {
            return true;
        }

        return false;
    }
}
