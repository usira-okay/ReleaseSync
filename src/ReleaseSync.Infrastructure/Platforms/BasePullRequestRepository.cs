using Microsoft.Extensions.Logging;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Infrastructure.Platforms;

/// <summary>
/// Pull Request Repository 抽象基礎類別
/// 提供共同的查詢與轉換邏輯
/// </summary>
/// <typeparam name="TApiDto">API 回傳的 DTO 型別</typeparam>
public abstract class BasePullRequestRepository<TApiDto> : IPullRequestRepository
{
    protected readonly ILogger _logger;
    protected readonly IUserMappingService _userMappingService;

    /// <summary>
    /// 平台名稱
    /// </summary>
    protected abstract string PlatformName { get; }

    /// <summary>
    /// 建立 BasePullRequestRepository
    /// </summary>
    protected BasePullRequestRepository(
        ILogger logger,
        IUserMappingService userMappingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userMappingService = userMappingService ?? throw new ArgumentNullException(nameof(userMappingService));
    }

    /// <summary>
    /// 查詢指定時間範圍內的 Pull Requests
    /// </summary>
    public async Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string>? targetBranches = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName, nameof(projectName));
        ArgumentNullException.ThrowIfNull(dateRange, nameof(dateRange));

        try
        {
            // 呼叫 API Client 取得 Pull Requests
            var apiDtos = await FetchPullRequestsFromApiAsync(
                projectName,
                dateRange,
                targetBranches,
                cancellationToken);

            // 轉換為 Domain Model
            var allPullRequests = apiDtos
                .Select(dto => ConvertToPullRequestInfo(dto, projectName))
                .ToList();

            // 套用目標分支過濾（如果需要）
            var filteredPullRequests = ApplyTargetBranchFilter(allPullRequests, targetBranches, projectName);

            // 套用 UserMapping 過濾 (過濾不在清單內的使用者)
            var userFilteredPullRequests = ApplyUserMappingFilter(filteredPullRequests, projectName);

            _logger.LogInformation(
                "取得 {Count} 筆 PR/MR - 平台: {Platform}, 專案: {ProjectName}",
                userFilteredPullRequests.Count, PlatformName, projectName);

            return userFilteredPullRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢 {Platform} PR/MR 失敗 - 專案: {ProjectName}", PlatformName, projectName);
            throw;
        }
    }

    /// <summary>
    /// 從 API 取得 Pull Requests
    /// </summary>
    protected abstract Task<IEnumerable<TApiDto>> FetchPullRequestsFromApiAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string>? targetBranches,
        CancellationToken cancellationToken);

    /// <summary>
    /// 將 API DTO 轉換為 Domain Model
    /// </summary>
    protected abstract PullRequestInfo ConvertToPullRequestInfo(TApiDto apiDto, string projectName);

    /// <summary>
    /// 套用目標分支過濾（如果需要）
    /// 預設不做額外過濾，子類別可覆寫
    /// </summary>
    protected virtual List<PullRequestInfo> ApplyTargetBranchFilter(
        List<PullRequestInfo> pullRequests,
        IEnumerable<string>? targetBranches,
        string projectName)
    {
        return pullRequests;
    }

    /// <summary>
    /// 套用 UserMapping 過濾 (過濾不在清單內的使用者)
    /// </summary>
    /// <param name="pullRequests">待過濾的 Pull Requests</param>
    /// <param name="projectName">專案名稱 (用於日誌)</param>
    /// <returns>過濾後的 Pull Requests</returns>
    protected virtual List<PullRequestInfo> ApplyUserMappingFilter(
        List<PullRequestInfo> pullRequests,
        string projectName)
    {
        // 如果未啟用過濾 (UserMapping 為空)，則不過濾
        if (!_userMappingService.IsFilteringEnabled())
        {
            _logger.LogDebug(
                "UserMapping 過濾未啟用，保留所有 PR/MR - 平台: {Platform}, 專案: {ProjectName}",
                PlatformName, projectName);
            return pullRequests;
        }

        var originalCount = pullRequests.Count;
        var filteredPullRequests = new List<PullRequestInfo>();

        foreach (var pr in pullRequests)
        {
            if (_userMappingService.HasMapping(PlatformName, pr.AuthorUserId))
            {
                filteredPullRequests.Add(pr);
            }
            else
            {
                _logger.LogInformation(
                    "過濾 PR/MR: 作者不在 UserMapping 清單中 - 平台: {Platform}, PR: #{Number}, 作者: {AuthorUserId} ({AuthorDisplayName})",
                    PlatformName, pr.Number, pr.AuthorUserId, pr.AuthorDisplayName);
            }
        }

        var removedCount = originalCount - filteredPullRequests.Count;
        if (removedCount > 0)
        {
            _logger.LogInformation(
                "UserMapping 過濾完成 - 平台: {Platform}, 專案: {ProjectName}, 原始數量: {OriginalCount}, 移除: {RemovedCount}, 保留: {RemainingCount}",
                PlatformName, projectName, originalCount, removedCount, filteredPullRequests.Count);
        }

        return filteredPullRequests;
    }

}

