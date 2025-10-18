using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Infrastructure.Platforms.AzureDevOps;

/// <summary>
/// Azure DevOps Work Item Repository 實作
/// </summary>
public class AzureDevOpsWorkItemRepository : IWorkItemRepository
{
    private readonly AzureDevOpsApiClient _apiClient;
    private readonly ILogger<AzureDevOpsWorkItemRepository> _logger;

    public AzureDevOpsWorkItemRepository(
        AzureDevOpsApiClient apiClient,
        ILogger<AzureDevOpsWorkItemRepository> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
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

        if (includeParent)
        {
            var parent = await _apiClient.GetParentWorkItemAsync(workItem, cancellationToken);
            if (parent != null)
            {
                workItemInfo.ParentWorkItem = ConvertToWorkItemInfo(parent);
                _logger.LogDebug(
                    "Work Item {WorkItemId} 的 Parent: {ParentId}",
                    workItemId.Value, parent.Id);
            }
        }

        _logger.LogDebug(
            "成功查詢 Work Item: {WorkItemId}, Type={Type}, State={State}",
            workItemId.Value, workItemInfo.Type, workItemInfo.State);

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

        var result = new List<WorkItemInfo>();

        foreach (var workItem in workItems)
        {
            var workItemInfo = ConvertToWorkItemInfo(workItem);

            if (includeParent)
            {
                var parent = await _apiClient.GetParentWorkItemAsync(workItem, cancellationToken);
                if (parent != null)
                {
                    workItemInfo.ParentWorkItem = ConvertToWorkItemInfo(parent);
                }
            }

            result.Add(workItemInfo);
        }

        _logger.LogInformation(
            "成功批次查詢 Work Items: Count={Count}",
            result.Count);

        return result;
    }

    /// <summary>
    /// 轉換 Azure DevOps WorkItem 到 Domain WorkItemInfo
    /// </summary>
    private WorkItemInfo ConvertToWorkItemInfo(WorkItem workItem)
    {
        return new WorkItemInfo
        {
            Id = new WorkItemId((int)workItem.Id!),
            Title = GetFieldValue(workItem, "System.Title") ?? "Untitled",
            Type = GetFieldValue(workItem, "System.WorkItemType") ?? "Unknown",
            State = GetFieldValue(workItem, "System.State") ?? "Unknown",
            Url = workItem.Url,
            AssignedTo = GetAssignedToValue(workItem),
            CreatedAt = GetDateTimeField(workItem, "System.CreatedDate"),
            UpdatedAt = GetDateTimeField(workItem, "System.ChangedDate")
        };
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
}
