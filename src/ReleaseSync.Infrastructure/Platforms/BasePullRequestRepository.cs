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

            // 根據 UserMapping 過濾 PR (如果啟用)
            var beforeUserFilterCount = filteredPullRequests.Count;
            filteredPullRequests = filteredPullRequests
                .Where(pr => pr.AuthorDisplayName != null && _userMappingService.HasMapping(PlatformName, pr.AuthorUserId))
                .ToList();

            // 記錄過濾統計
            LogUserMappingFilterResults(beforeUserFilterCount, filteredPullRequests.Count, projectName);

            return filteredPullRequests;
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
    /// 記錄 UserMapping 過濾結果
    /// </summary>
    private void LogUserMappingFilterResults(int beforeCount, int afterCount, string projectName)
    {
        var filteredCount = beforeCount - afterCount;

        if (_userMappingService.IsFilteringEnabled())
        {
            if (filteredCount > 0)
            {
                _logger.LogInformation(
                    "根據 UserMapping 過濾 {FilteredCount} 筆 PR/MR (總共 {TotalCount} 筆,保留 {RetainedCount} 筆) - 專案: {ProjectName}",
                    filteredCount, beforeCount, afterCount, projectName);
            }
            else
            {
                _logger.LogInformation(
                    "所有 {Count} 筆 PR/MR 都在 UserMapping 中,無需過濾 - 專案: {ProjectName}",
                    beforeCount, projectName);
            }
        }
        else
        {
            _logger.LogDebug(
                "UserMapping 為空,保留所有 {Count} 筆 PR/MR (向後相容模式) - 專案: {ProjectName}",
                afterCount, projectName);
        }
    }
}
