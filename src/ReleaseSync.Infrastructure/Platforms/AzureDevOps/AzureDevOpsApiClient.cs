using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.Extensions.Logging;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.Platforms.AzureDevOps;

/// <summary>
/// Azure DevOps API 客戶端
/// </summary>
public class AzureDevOpsApiClient : IDisposable
{
    private readonly VssConnection _connection;
    private readonly WorkItemTrackingHttpClient _witClient;
    private readonly ILogger<AzureDevOpsApiClient> _logger;
    private bool _disposed;

    public AzureDevOpsApiClient(
        AzureDevOpsSettings settings,
        ILogger<AzureDevOpsApiClient> logger)
    {
        _logger = logger;

        var uri = new Uri(settings.OrganizationUrl);
        var credentials = new VssBasicCredential(string.Empty, settings.PersonalAccessToken);

        _connection = new VssConnection(uri, credentials);
        _witClient = _connection.GetClient<WorkItemTrackingHttpClient>();

        _logger.LogInformation(
            "已建立 Azure DevOps API 連線: Organization={OrganizationUrl}",
            settings.OrganizationUrl);
    }

    /// <summary>
    /// 取得 Work Item
    /// </summary>
    /// <param name="workItemId">Work Item ID</param>
    /// <param name="includeRelations">是否包含關聯資訊</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊,若不存在則回傳 null</returns>
    public async Task<WorkItem?> GetWorkItemAsync(
        int workItemId,
        bool includeRelations = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("開始抓取 Work Item: {WorkItemId}", workItemId);

            var workItem = await _witClient.GetWorkItemAsync(
                id: workItemId,
                expand: includeRelations ? WorkItemExpand.Relations : WorkItemExpand.None,
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "成功抓取 Work Item: {WorkItemId}, Type={Type}, Title={Title}",
                workItemId,
                workItem.Fields.GetValueOrDefault("System.WorkItemType"),
                workItem.Fields.GetValueOrDefault("System.Title"));

            return workItem;
        }
        catch (VssServiceException ex) when (ex.Message.Contains("does not exist"))
        {
            _logger.LogWarning(
                "Work Item 不存在: {WorkItemId}",
                workItemId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "抓取 Work Item 失敗: {WorkItemId}",
                workItemId);
            return null;
        }
    }

    /// <summary>
    /// 取得 Parent Work Item
    /// </summary>
    /// <param name="workItem">子 Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Parent Work Item,若不存在則回傳 null</returns>
    public async Task<WorkItem?> GetParentWorkItemAsync(
        WorkItem workItem,
        CancellationToken cancellationToken = default)
    {
        const string ParentLinkType = "System.LinkTypes.Hierarchy-Reverse";

        var parentRelation = workItem.Relations?
            .FirstOrDefault(r => r.Rel == ParentLinkType);

        if (parentRelation == null)
        {
            _logger.LogDebug(
                "Work Item {WorkItemId} 沒有 Parent Work Item",
                workItem.Id);
            return null;
        }

        try
        {
            // 從 URL 解析 Parent Work Item ID
            // URL 格式: https://dev.azure.com/{org}/_apis/wit/workItems/{id}
            var parentIdString = parentRelation.Url.Split('/').Last();
            if (!int.TryParse(parentIdString, out var parentId))
            {
                _logger.LogWarning(
                    "無法解析 Parent Work Item ID from URL: {Url}",
                    parentRelation.Url);
                return null;
            }

            _logger.LogDebug(
                "Work Item {WorkItemId} 的 Parent Work Item ID: {ParentId}",
                workItem.Id, parentId);

            return await GetWorkItemAsync(parentId, false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "抓取 Parent Work Item 失敗: ChildWorkItemId={WorkItemId}",
                workItem.Id);
            return null;
        }
    }

    /// <summary>
    /// 批次取得多個 Work Items
    /// </summary>
    /// <param name="workItemIds">Work Item ID 清單</param>
    /// <param name="includeRelations">是否包含關聯資訊</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 清單</returns>
    public async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(
        IEnumerable<int> workItemIds,
        bool includeRelations = true,
        CancellationToken cancellationToken = default)
    {
        var ids = workItemIds.ToList();
        if (!ids.Any())
        {
            return Enumerable.Empty<WorkItem>();
        }

        try
        {
            _logger.LogDebug(
                "開始批次抓取 Work Items: Count={Count}",
                ids.Count);

            var workItems = await _witClient.GetWorkItemsAsync(
                ids: ids,
                expand: includeRelations ? WorkItemExpand.Relations : WorkItemExpand.None,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "成功批次抓取 Work Items: Requested={RequestedCount}, Retrieved={RetrievedCount}",
                ids.Count, workItems.Count);

            return workItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "批次抓取 Work Items 失敗: Count={Count}",
                ids.Count);
            return Enumerable.Empty<WorkItem>();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _connection?.Dispose();
        _disposed = true;

        _logger.LogDebug("已釋放 Azure DevOps API 連線");
    }
}
