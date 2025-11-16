using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Domain.Services;
using System.Diagnostics;

namespace ReleaseSync.Console.Services;

/// <summary>
/// Work Item 整合服務,負責從 Azure DevOps 抓取 Work Item 資訊並關聯到 PR/MR
/// </summary>
public class WorkItemEnricher : IWorkItemEnricher
{
    private readonly IWorkItemService? _workItemService;
    private readonly IWorkItemIdParser? _workItemIdParser;
    private readonly ILogger<WorkItemEnricher> _logger;

    /// <summary>
    /// 建立 WorkItemEnricher
    /// </summary>
    /// <param name="logger">日誌記錄器</param>
    /// <param name="workItemService">Work Item 服務 (可選)</param>
    /// <param name="workItemIdParser">Work Item ID 解析器 (可選)</param>
    public WorkItemEnricher(
        ILogger<WorkItemEnricher> logger,
        IWorkItemService? workItemService = null,
        IWorkItemIdParser? workItemIdParser = null)
    {
        _logger = logger;
        _workItemService = workItemService;
        _workItemIdParser = workItemIdParser;
    }

    /// <summary>
    /// 從 Work Item 服務抓取資訊並關聯到 PR/MR,並移除抓取失敗的 PR (VSTS000000 開頭除外)
    /// </summary>
    /// <param name="result">同步結果</param>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task EnrichAsync(
        SyncResultDto result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (_workItemService == null)
        {
            _logger.LogWarning("Work Item 服務未設定,跳過 Work Item 整合");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        int successCount = 0;
        int placeholderCount = 0;
        int failureCount = 0;
        var prsToRemove = new List<PullRequestDto>();

        foreach (var pr in result.PullRequests)
        {
            var enrichResult = await TryEnrichPullRequestAsync(pr, cancellationToken);

            switch (enrichResult)
            {
                case EnrichmentResult.Success:
                    successCount++;
                    break;
                case EnrichmentResult.Placeholder:
                    placeholderCount++;
                    // VSTS000000 開頭的佔位符保留 PR,但 WorkItem 為 null
                    break;
                case EnrichmentResult.Failure:
                    failureCount++;
                    prsToRemove.Add(pr);
                    break;
            }
        }

        // 移除抓取失敗的 PR
        foreach (var pr in prsToRemove)
        {
            result.PullRequests.Remove(pr);
            _logger.LogInformation(
                "移除 Work Item 抓取失敗的 PR: Platform={Platform}, Number={Number}, Title={Title}",
                pr.Platform, pr.Number, pr.Title);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Work Item 整合完成 - 成功: {SuccessCount}, 佔位符: {PlaceholderCount}, 移除: {RemovedCount}, 耗時: {ElapsedMs} ms",
            successCount, placeholderCount, prsToRemove.Count, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 整合結果列舉
    /// </summary>
    private enum EnrichmentResult
    {
        /// <summary>
        /// 成功抓取並關聯 Work Item
        /// </summary>
        Success,

        /// <summary>
        /// Branch 為佔位符 (VSTS000000 開頭),保留 PR 但 WorkItem 為 null
        /// </summary>
        Placeholder,

        /// <summary>
        /// 抓取失敗,需要移除該 PR
        /// </summary>
        Failure
    }

    /// <summary>
    /// 嘗試為單一 PR/MR 整合 Work Item 資訊
    /// </summary>
    /// <param name="pr">Pull Request DTO</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>整合結果</returns>
    private async Task<EnrichmentResult> TryEnrichPullRequestAsync(
        PullRequestDto pr,
        CancellationToken cancellationToken)
    {
        try
        {
            var branchName = new Domain.Models.BranchName(pr.SourceBranch);

            // 先檢查是否能解析 Work Item ID,並判斷是否為佔位符
            if (_workItemIdParser != null &&
                _workItemIdParser.TryParseWorkItemId(branchName, out var parsedWorkItemId))
            {
                // 如果 Work Item ID 為 0 (佔位符,如 VSTS000000),保留 PR 但 WorkItem 為 null
                if (parsedWorkItemId.IsPlaceholder)
                {
                    _logger.LogInformation(
                        "Branch 為佔位符 (VSTS000000 開頭),保留 PR 但不抓取 Work Item: Branch={BranchName}",
                        pr.SourceBranch);
                    return EnrichmentResult.Placeholder;
                }
            }

            // 從 Branch 名稱解析並取得 Work Item
            var workItem = await _workItemService!.GetWorkItemFromBranchAsync(
                branchName,
                includeParent: true,
                cancellationToken);

            if (workItem != null)
            {
                // 如果是佔位符 Work Item (ID=0),保留 PR 但 WorkItem 為 null
                if (workItem.Id.IsPlaceholder)
                {
                    _logger.LogInformation(
                        "Work Item 為佔位符,保留 PR 但 WorkItem 為 null: Branch={BranchName}",
                        pr.SourceBranch);
                    return EnrichmentResult.Placeholder;
                }

                pr.AssociatedWorkItem = WorkItemDto.FromDomain(workItem);
                return EnrichmentResult.Success;
            }

            // Work Item 抓取失敗 (API 返回 null),需要移除該 PR
            _logger.LogWarning(
                "Work Item 抓取失敗,將移除該 PR: PR={PrTitle}, Branch={BranchName}",
                pr.Title, pr.SourceBranch);
            return EnrichmentResult.Failure;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "整合 Work Item 發生例外,將移除該 PR: PR={PrTitle}, Branch={BranchName}",
                pr.Title, pr.SourceBranch);

            return EnrichmentResult.Failure;
        }
    }
}
