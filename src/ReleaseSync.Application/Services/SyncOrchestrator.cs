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
/// 協調多平台的 PR/MR 抓取
/// </summary>
public class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IEnumerable<IPlatformService> _platformServices;
    private readonly ILogger<SyncOrchestrator> _logger;

    /// <summary>
    /// 建立 SyncOrchestrator
    /// </summary>
    public SyncOrchestrator(
        IEnumerable<IPlatformService> platformServices,
        ILogger<SyncOrchestrator> logger)
    {
        _platformServices = platformServices ?? throw new ArgumentNullException(nameof(platformServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        // 根據抓取模式執行不同的同步邏輯
        if (request.FetchMode == FetchMode.ReleaseBranch)
        {
            return await SyncByReleaseBranchAsync(request, cancellationToken);
        }

        return await SyncByDateRangeAsync(request, cancellationToken);
    }

    /// <summary>
    /// 依日期範圍執行同步 (DateRange 模式)
    /// </summary>
    private async Task<SyncResultDto> SyncByDateRangeAsync(
        SyncRequest request,
        CancellationToken cancellationToken)
    {
        // 建立 DateRange
        var dateRange = new DateRange(request.StartDate, request.EndDate);
        dateRange.Validate();

        _logger.LogInformation(
            "開始同步作業 (DateRange 模式) - 時間範圍: {StartDate} - {EndDate}, 啟用平台: {Platforms}",
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

        // 並行執行所有啟用的平台
        var platformTasks = enabledServices.Select(async service =>
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Platform"] = service.PlatformName
            });

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var pullRequests = await service.GetPullRequestsAsync(
                    dateRange,
                    cancellationToken);

                var prList = pullRequests.ToList();
                stopwatch.Stop();

                _logger.LogInformation("平台 {Platform} 完成 - {Count} 筆 PR/MR, {ElapsedMs} ms",
                    service.PlatformName, prList.Count, stopwatch.ElapsedMilliseconds);

                return (
                    Success: true,
                    Service: service,
                    PullRequests: prList,
                    Status: PlatformSyncStatus.Success(
                        service.PlatformName,
                        prList.Count,
                        stopwatch.ElapsedMilliseconds)
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "平台 {Platform} 失敗 - {ElapsedMs} ms",
                    service.PlatformName, stopwatch.ElapsedMilliseconds);

                return (
                    Success: false,
                    Service: service,
                    PullRequests: new List<PullRequestInfo>(),
                    Status: PlatformSyncStatus.Failure(
                        service.PlatformName,
                        ex.Message,
                        stopwatch.ElapsedMilliseconds)
                );
            }
        });

        // 等待所有平台完成
        var platformResults = await Task.WhenAll(platformTasks);

        // 彙整結果
        foreach (var result in platformResults)
        {
            if (result.Success)
            {
                syncResult.AddPullRequests(result.PullRequests);
            }
            syncResult.RecordPlatformStatus(result.Status);
        }

        syncResult.MarkAsCompleted();

        // 計算總耗時與效能指標
        var totalElapsedMs = syncResult.PlatformStatuses
            .Sum(s => s.ElapsedMilliseconds);
        var totalPRCount = syncResult.PullRequests.Count();

        _logger.LogInformation("同步作業完成 - {Summary}, 總耗時: {TotalMs} ms, PR/MR: {Count} 筆",
            syncResult.GetSummary(), totalElapsedMs, totalPRCount);

        // 檢查效能是否符合目標 (100 筆 PR/MR 於 30 秒內)
        if (totalPRCount >= 100 && totalElapsedMs > 30000)
        {
            _logger.LogWarning(
                "效能低於預期: {Count} 筆資料耗時 {Seconds:F2} 秒 (目標: 30 秒)",
                totalPRCount,
                totalElapsedMs / 1000.0);
        }

        return SyncResultDto.FromDomain(syncResult);
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

    /// <summary>
    /// 依 Release Branch 執行同步 (ReleaseBranch 模式)
    /// </summary>
    /// <remarks>
    /// 此模式會比對指定 Release Branch 與下一個版本之間的差異，
    /// 找出該版本包含的所有 PR/MR。
    /// </remarks>
    private async Task<SyncResultDto> SyncByReleaseBranchAsync(
        SyncRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReleaseBranch))
        {
            throw new ArgumentException("ReleaseBranch 模式必須指定 Release Branch 名稱");
        }

        _logger.LogInformation(
            "開始同步作業 (ReleaseBranch 模式) - Release Branch: {ReleaseBranch}, 啟用平台: {Platforms}",
            request.ReleaseBranch,
            GetEnabledPlatformsDescription(request));

        // 建立 SyncResult (使用假的日期範圍，因為 ReleaseBranch 模式不使用日期)
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow)
        };

        // 篩選要執行的平台
        var enabledServices = FilterEnabledServices(request).ToList();

        if (!enabledServices.Any())
        {
            _logger.LogWarning("沒有啟用的平台服務");
            syncResult.MarkAsCompleted();
            return SyncResultDto.FromDomain(syncResult);
        }

        // 並行執行所有啟用的平台
        var platformTasks = enabledServices.Select(async service =>
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Platform"] = service.PlatformName
            });

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var pullRequests = await service.GetPullRequestsByReleaseBranchAsync(
                    request.ReleaseBranch,
                    cancellationToken);

                var prList = pullRequests.ToList();
                stopwatch.Stop();

                _logger.LogInformation("平台 {Platform} 完成 - {Count} 筆 PR/MR, {ElapsedMs} ms",
                    service.PlatformName, prList.Count, stopwatch.ElapsedMilliseconds);

                return (
                    Success: true,
                    Service: service,
                    PullRequests: prList,
                    Status: PlatformSyncStatus.Success(
                        service.PlatformName,
                        prList.Count,
                        stopwatch.ElapsedMilliseconds)
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "平台 {Platform} 失敗 - {ElapsedMs} ms",
                    service.PlatformName, stopwatch.ElapsedMilliseconds);

                return (
                    Success: false,
                    Service: service,
                    PullRequests: new List<PullRequestInfo>(),
                    Status: PlatformSyncStatus.Failure(
                        service.PlatformName,
                        ex.Message,
                        stopwatch.ElapsedMilliseconds)
                );
            }
        });

        // 等待所有平台完成
        var platformResults = await Task.WhenAll(platformTasks);

        // 彙整結果
        foreach (var result in platformResults)
        {
            if (result.Success)
            {
                syncResult.AddPullRequests(result.PullRequests);
            }
            syncResult.RecordPlatformStatus(result.Status);
        }

        syncResult.MarkAsCompleted();

        // 計算總耗時與效能指標
        var totalElapsedMs = syncResult.PlatformStatuses
            .Sum(s => s.ElapsedMilliseconds);
        var totalPRCount = syncResult.PullRequests.Count();

        _logger.LogInformation(
            "同步作業完成 (ReleaseBranch 模式) - Release Branch: {ReleaseBranch}, {Summary}, 總耗時: {TotalMs} ms, PR/MR: {Count} 筆",
            request.ReleaseBranch, syncResult.GetSummary(), totalElapsedMs, totalPRCount);

        return SyncResultDto.FromDomain(syncResult);
    }
}
