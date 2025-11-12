using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;
using Microsoft.Extensions.Logging;

namespace ReleaseSync.Infrastructure.Platforms.AzureDevOps;

/// <summary>
/// Azure DevOps 平台服務
/// </summary>
public class AzureDevOpsService : IWorkItemService
{
    private readonly IWorkItemRepository _repository;
    private readonly IWorkItemIdParser _parser;
    private readonly ILogger<AzureDevOpsService> _logger;

    /// <summary>
    /// 建立 AzureDevOpsService
    /// </summary>
    public AzureDevOpsService(
        IWorkItemRepository repository,
        IWorkItemIdParser parser,
        ILogger<AzureDevOpsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 取得 Work Item
    /// </summary>
    public async Task<WorkItemInfo?> GetWorkItemAsync(
        WorkItemId workItemId,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        // 當 Work Item ID = 0 時,回傳假的佔位符 Work Item
        if (workItemId.IsPlaceholder)
        {
            _logger.LogInformation(
                "Work Item ID = 0 (佔位符),回傳假資料,不呼叫 Azure DevOps API");

            return CreatePlaceholderWorkItem();
        }

        _logger.LogInformation(
            "開始取得 Work Item: {WorkItemId}, IncludeParent={IncludeParent}",
            workItemId.Value, includeParent);

        try
        {
            var workItem = await _repository.GetWorkItemAsync(
                workItemId,
                includeParent,
                cancellationToken);

            if (workItem == null)
            {
                _logger.LogWarning("Work Item 不存在: {WorkItemId}", workItemId.Value);
                return null;
            }

            _logger.LogInformation(
                "成功取得 Work Item: {WorkItemId}, Type={Type}, State={State}",
                workItemId.Value, workItem.Type, workItem.State);

            return workItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "取得 Work Item 失敗: {WorkItemId}",
                workItemId.Value);
            return null;
        }
    }

    /// <summary>
    /// 建立佔位符 Work Item (用於 VSTS000000 等無關聯 Work Item 的情況)
    /// </summary>
    /// <remarks>
    /// 所有 Work Item ID = 0 的 PR 都會使用同一個佔位符 Work Item
    /// </remarks>
    private static WorkItemInfo CreatePlaceholderWorkItem()
    {
        return new WorkItemInfo
        {
            Id = new WorkItemId(0),
            Title = "無關聯 Work Item",
            Type = "Placeholder",
            State = "N/A",
            AssignedTo = "N/A",
            Team = "N/A",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 從 Branch 名稱解析並取得 Work Item
    /// </summary>
    public async Task<WorkItemInfo?> GetWorkItemFromBranchAsync(
        BranchName branchName,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "開始從 Branch 名稱解析 Work Item: {BranchName}",
            branchName.Value);

        // 解析 Work Item ID
        if (!_parser.TryParseWorkItemId(branchName, out var workItemId))
        {
            _logger.LogWarning(
                "無法從 Branch 名稱解析 Work Item ID: {BranchName}",
                branchName.Value);
            return null;
        }

        _logger.LogInformation(
            "成功解析 Work Item ID: {WorkItemId} from Branch: {BranchName}",
            workItemId.Value, branchName.Value);

        // 取得 Work Item
        return await GetWorkItemAsync(workItemId, includeParent, cancellationToken);
    }

    /// <summary>
    /// 批次取得多個 Work Items
    /// </summary>
    public async Task<IEnumerable<WorkItemInfo>> GetWorkItemsAsync(
        IEnumerable<WorkItemId> workItemIds,
        bool includeParent = true,
        CancellationToken cancellationToken = default)
    {
        var ids = workItemIds.ToList();

        _logger.LogInformation(
            "開始批次取得 Work Items: Count={Count}",
            ids.Count);

        try
        {
            var workItems = await _repository.GetWorkItemsAsync(
                ids,
                includeParent,
                cancellationToken);

            _logger.LogInformation(
                "成功批次取得 Work Items: Count={Count}",
                workItems.Count());

            return workItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "批次取得 Work Items 失敗: Count={Count}",
                ids.Count);
            return Enumerable.Empty<WorkItemInfo>();
        }
    }
}
