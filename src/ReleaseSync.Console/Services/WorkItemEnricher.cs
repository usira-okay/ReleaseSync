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
    private readonly ILogger<WorkItemEnricher> _logger;

    /// <summary>
    /// 建立 WorkItemEnricher
    /// </summary>
    /// <param name="logger">日誌記錄器</param>
    /// <param name="workItemService">Work Item 服務 (可選)</param>
    public WorkItemEnricher(
        ILogger<WorkItemEnricher> logger,
        IWorkItemService? workItemService = null)
    {
        _logger = logger;
        _workItemService = workItemService;
    }

    /// <summary>
    /// 從 Work Item 服務抓取資訊並關聯到 PR/MR
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
        int failureCount = 0;

        foreach (var pr in result.PullRequests)
        {
            var enrichResult = await TryEnrichPullRequestAsync(pr, cancellationToken);
            if (enrichResult)
                successCount++;
            else
                failureCount++;
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Work Item 整合完成 - 成功: {SuccessCount}, 失敗: {FailureCount}, 耗時: {ElapsedMs} ms",
            successCount, failureCount, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 嘗試為單一 PR/MR 整合 Work Item 資訊
    /// </summary>
    /// <param name="pr">Pull Request DTO</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>整合是否成功</returns>
    private async Task<bool> TryEnrichPullRequestAsync(
        PullRequestDto pr,
        CancellationToken cancellationToken)
    {
        try
        {
            // 從 Branch 名稱解析並取得 Work Item
            var branchName = new Domain.Models.BranchName(pr.SourceBranch);
            var workItem = await _workItemService!.GetWorkItemFromBranchAsync(
                branchName,
                includeParent: true,
                cancellationToken);

            if (workItem != null)
            {
                pr.AssociatedWorkItem = WorkItemDto.FromDomain(workItem);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "整合 Work Item 失敗: PR={PrTitle}, Branch={BranchName}",
                pr.Title, pr.SourceBranch);

            return false;
        }
    }
}
