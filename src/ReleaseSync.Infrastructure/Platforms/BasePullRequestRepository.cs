using Microsoft.Extensions.Logging;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Exceptions;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Infrastructure.Platforms.Models;

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
            _logger.LogInformation(
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

    /// <summary>
    /// 根據 Release Branch 比對取得待發布的 Pull Requests
    /// </summary>
    /// <param name="projectName">專案名稱 (例如: owner/repo)</param>
    /// <param name="releaseBranch">Release Branch 名稱</param>
    /// <param name="targetBranch">目標分支名稱</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>在目標分支但不在 Release Branch 的 Pull Request 清單</returns>
    public async Task<IEnumerable<PullRequestInfo>> GetByReleaseBranchAsync(
        string projectName,
        string releaseBranch,
        string targetBranch,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName, nameof(projectName));
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseBranch, nameof(releaseBranch));
        ArgumentException.ThrowIfNullOrWhiteSpace(targetBranch, nameof(targetBranch));

        try
        {
            _logger.LogInformation(
                "開始 Release Branch 比對 - 平台: {Platform}, 專案: {ProjectName}, Release: {ReleaseBranch}, Target: {TargetBranch}",
                PlatformName, projectName, releaseBranch, targetBranch);

            // 取得所有分支以驗證 Release Branch 存在
            var branches = await GetBranchesAsync(projectName, "release", cancellationToken);
            var branchList = branches.ToList();

            var releaseBranchExists = branchList.Any(b =>
                b.Name.Equals(releaseBranch, StringComparison.OrdinalIgnoreCase));

            if (!releaseBranchExists)
            {
                var availableReleaseBranches = branchList
                    .Select(b => b.Name)
                    .OrderByDescending(n => n)
                    .Take(10)
                    .ToList();

                throw new ReleaseBranchNotFoundException(
                    releaseBranch,
                    projectName,
                    availableReleaseBranches);
            }

            // 比對兩個分支之間的差異
            var compareResult = await CompareBranchesAsync(
                projectName,
                releaseBranch,
                targetBranch,
                cancellationToken);

            _logger.LogInformation(
                "分支比對完成 - 平台: {Platform}, 專案: {ProjectName}, 差異 Commit 數: {Count}",
                PlatformName, projectName, compareResult.Commits.Count);

            // 根據差異的 Commits 找出對應的 PR/MR
            var pullRequests = await GetPullRequestsFromCommitsAsync(
                projectName,
                compareResult.Commits,
                targetBranch,
                cancellationToken);

            // 套用 UserMapping 過濾
            var userFilteredPullRequests = ApplyUserMappingFilter(pullRequests.ToList(), projectName);

            _logger.LogInformation(
                "取得 {Count} 筆待發布 PR/MR - 平台: {Platform}, 專案: {ProjectName}",
                userFilteredPullRequests.Count, PlatformName, projectName);

            return userFilteredPullRequests;
        }
        catch (ReleaseBranchNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Release Branch 比對失敗 - 平台: {Platform}, 專案: {ProjectName}",
                PlatformName, projectName);
            throw;
        }
    }

    /// <summary>
    /// 取得專案的分支清單
    /// </summary>
    /// <param name="projectName">專案名稱</param>
    /// <param name="searchPattern">搜尋模式</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分支資訊清單</returns>
    protected abstract Task<IEnumerable<BranchInfo>> GetBranchesAsync(
        string projectName,
        string? searchPattern,
        CancellationToken cancellationToken);

    /// <summary>
    /// 比對兩個分支之間的差異
    /// </summary>
    /// <param name="projectName">專案名稱</param>
    /// <param name="fromBranch">起始分支</param>
    /// <param name="toBranch">目標分支</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分支比對結果</returns>
    protected abstract Task<BranchCompareResult> CompareBranchesAsync(
        string projectName,
        string fromBranch,
        string toBranch,
        CancellationToken cancellationToken);

    /// <summary>
    /// 根據 Commits 找出對應的 Pull Requests
    /// </summary>
    /// <param name="projectName">專案名稱</param>
    /// <param name="commits">Commit 清單</param>
    /// <param name="targetBranch">目標分支</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>對應的 Pull Request 清單</returns>
    protected abstract Task<IEnumerable<PullRequestInfo>> GetPullRequestsFromCommitsAsync(
        string projectName,
        IReadOnlyList<CommitInfo> commits,
        string targetBranch,
        CancellationToken cancellationToken);

}

