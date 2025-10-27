namespace ReleaseSync.Application.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Services;

/// <summary>
/// 同步協調器
/// 協調多平台的 PR/MR 抓取與 Work Item 整合
/// </summary>
public class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IEnumerable<IPlatformService> _platformServices;
    private readonly IWorkItemService? _workItemService;
    private readonly ILogger<SyncOrchestrator> _logger;

    /// <summary>
    /// 建立 SyncOrchestrator
    /// </summary>
    public SyncOrchestrator(
        IEnumerable<IPlatformService> platformServices,
        ILogger<SyncOrchestrator> logger,
        IWorkItemService? workItemService = null)
    {
        _platformServices = platformServices ?? throw new ArgumentNullException(nameof(platformServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workItemService = workItemService;
    }

    /// <summary>
    /// 執行同步作業
    /// </summary>
    public async Task<SyncResultDto> SyncAsync(
        SyncRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 驗證至少啟用一個平台
        if (!request.EnableGitLab && !request.EnableBitBucket)
        {
            throw new ArgumentException("至少須啟用一個平台 (GitLab 或 BitBucket)");
        }

        // 建立 DateRange
        var dateRange = new DateRange(request.StartDate, request.EndDate);
        dateRange.Validate();

        _logger.LogInformation(
            "開始同步作業 - 時間範圍: {StartDate} - {EndDate}, 啟用平台: {Platforms}",
            dateRange.StartDate, dateRange.EndDate,
            GetEnabledPlatformsDescription(request));

        // 建立 SyncResult
        var syncResult = new SyncResult
        {
            SyncDateRange = dateRange
        };

        // 篩選要執行的平台
        var enabledServices = FilterEnabledServices(request).ToList();

        if (!enabledServices.Any())
        {
            _logger.LogWarning("沒有啟用的平台服務");
            syncResult.MarkAsCompleted();
            return SyncResultDto.FromDomain(syncResult);
        }

        // 依序執行所有啟用的平台
        foreach (var service in enabledServices)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("開始同步平台: {Platform}", service.PlatformName);

                var pullRequests = await service.GetPullRequestsAsync(
                    dateRange,
                    cancellationToken);

                var prList = pullRequests.ToList();
                stopwatch.Stop();

                _logger.LogInformation("平台 {Platform} 同步成功 - 抓取 {Count} 筆 PR/MR, 耗時 {ElapsedMs} ms",
                    service.PlatformName, prList.Count, stopwatch.ElapsedMilliseconds);

                // 加入到 SyncResult
                syncResult.AddPullRequests(prList);

                // 記錄平台狀態
                syncResult.RecordPlatformStatus(
                    PlatformSyncStatus.Success(
                        service.PlatformName,
                        prList.Count,
                        stopwatch.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "平台 {Platform} 同步失敗 - 耗時 {ElapsedMs} ms",
                    service.PlatformName, stopwatch.ElapsedMilliseconds);

                // 記錄失敗狀態
                syncResult.RecordPlatformStatus(
                    PlatformSyncStatus.Failure(
                        service.PlatformName,
                        ex.Message,
                        stopwatch.ElapsedMilliseconds));
            }
        }

        // 整合 Azure DevOps Work Items (如果啟用)
        if (request.EnableAzureDevOps)
        {
            await EnrichWithWorkItemsAsync(syncResult, cancellationToken);
        }

        syncResult.MarkAsCompleted();

        // 計算總耗時與效能指標
        var totalElapsedMs = syncResult.PlatformStatuses
            .Sum(s => s.ElapsedMilliseconds);
        var totalPRCount = syncResult.PullRequests.Count();

        _logger.LogInformation("同步作業完成 - {Summary}", syncResult.GetSummary());
        _logger.LogInformation(
            "效能指標 - 總耗時: {TotalMs} ms, PR/MR 數量: {Count}, 平均每筆: {AvgMs} ms",
            totalElapsedMs,
            totalPRCount,
            totalPRCount > 0 ? totalElapsedMs / totalPRCount : 0);

        // 檢查效能是否符合目標 (100 筆 PR/MR 於 30 秒內)
        if (totalPRCount >= 100 && totalElapsedMs > 30000)
        {
            _logger.LogWarning(
                "⚠️ 效能低於預期: {Count} 筆資料耗時 {Seconds:F2} 秒 (目標: 30 秒)",
                totalPRCount,
                totalElapsedMs / 1000.0);
        }

        // 轉換為 DTO
        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 從 Work Item 服務抓取資訊並關聯到 PR/MR
    /// </summary>
    private async Task EnrichWithWorkItemsAsync(
        SyncResult syncResult,
        CancellationToken cancellationToken)
    {
        if (_workItemService == null)
        {
            _logger.LogWarning("Work Item 服務未註冊,無法整合 Work Item 資訊");
            return;
        }

        _logger.LogInformation(
            "開始整合 Work Items - PR/MR 數量: {Count}",
            syncResult.PullRequests.Count());

        var stopwatch = Stopwatch.StartNew();
        int successCount = 0;
        int failureCount = 0;

        foreach (var pr in syncResult.PullRequests)
        {
            try
            {
                // 從 Branch 名稱解析並取得 Work Item
                var workItem = await _workItemService.GetWorkItemFromBranchAsync(
                    pr.SourceBranch,
                    includeParent: true,
                    cancellationToken);

                if (workItem != null)
                {
                    pr.AssociatedWorkItem = workItem;
                    successCount++;

                    _logger.LogDebug(
                        "成功關聯 Work Item: PR={PrTitle}, WorkItem={WorkItemId} - {WorkItemTitle}",
                        pr.Title, workItem.Id.Value, workItem.Title);
                }
                else
                {
                    _logger.LogDebug(
                        "PR {PrTitle} 無對應的 Work Item (Branch: {BranchName})",
                        pr.Title, pr.SourceBranch.Value);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogWarning(ex,
                    "整合 Work Item 失敗: PR={PrTitle}, Branch={BranchName}",
                    pr.Title, pr.SourceBranch.Value);
            }
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "完成 Work Item 整合 - 成功: {SuccessCount}, 失敗: {FailureCount}, 耗時: {ElapsedMs} ms",
            successCount, failureCount, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 篩選要執行的平台服務
    /// </summary>
    private IEnumerable<IPlatformService> FilterEnabledServices(SyncRequest request)
    {
        foreach (var service in _platformServices)
        {
            var isEnabled = service.PlatformName switch
            {
                "GitLab" => request.EnableGitLab,
                "BitBucket" => request.EnableBitBucket,
                _ => false
            };

            if (isEnabled)
            {
                yield return service;
            }
        }
    }

    /// <summary>
    /// 取得啟用平台的描述
    /// </summary>
    private static string GetEnabledPlatformsDescription(SyncRequest request)
    {
        var platforms = new List<string>();
        if (request.EnableGitLab) platforms.Add("GitLab");
        if (request.EnableBitBucket) platforms.Add("BitBucket");
        return string.Join(", ", platforms);
    }
}
