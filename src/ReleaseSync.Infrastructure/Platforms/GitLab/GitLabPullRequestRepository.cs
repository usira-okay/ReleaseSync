using Microsoft.Extensions.Logging;
using NGitLab.Models;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Infrastructure.Platforms.GitLab;

/// <summary>
/// GitLab Pull Request (Merge Request) Repository 實作
/// </summary>
public class GitLabPullRequestRepository : IPullRequestRepository
{
    private readonly GitLabApiClient _apiClient;
    private readonly ILogger<GitLabPullRequestRepository> _logger;
    private readonly IUserMappingService _userMappingService;

    /// <summary>
    /// 建立 GitLabPullRequestRepository
    /// </summary>
    public GitLabPullRequestRepository(
        GitLabApiClient apiClient,
        ILogger<GitLabPullRequestRepository> logger,
        IUserMappingService userMappingService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userMappingService = userMappingService ?? throw new ArgumentNullException(nameof(userMappingService));
    }

    /// <summary>
    /// 查詢指定時間範圍內的 Merge Requests
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
            // 呼叫 API Client 取得 Merge Requests
            var mergeRequests = await _apiClient.GetMergeRequestsAsync(
                projectName,
                dateRange.StartDate,
                dateRange.EndDate,
                targetBranches,
                cancellationToken);

            // 轉換為 Domain Model
            var allPullRequests = mergeRequests
                .Select(mr => ConvertToPullRequestInfo(mr, projectName))
                .ToList();

            // 根據 UserMapping 過濾 PR (如果啟用)
            var filteredPullRequests = allPullRequests
                .Where(pr => pr.AuthorDisplayName != null && _userMappingService.HasMapping("GitLab", pr.AuthorDisplayName))
                .ToList();

            // 記錄過濾統計
            var filteredCount = allPullRequests.Count - filteredPullRequests.Count;
            if (_userMappingService.IsFilteringEnabled())
            {
                if (filteredCount > 0)
                {
                    _logger.LogInformation(
                        "根據 UserMapping 過濾 {FilteredCount} 筆 MR (總共 {TotalCount} 筆,保留 {RetainedCount} 筆) - 專案: {ProjectName}",
                        filteredCount, allPullRequests.Count, filteredPullRequests.Count, projectName);
                }
                else
                {
                    _logger.LogInformation(
                        "所有 {Count} 筆 MR 都在 UserMapping 中,無需過濾 - 專案: {ProjectName}",
                        allPullRequests.Count, projectName);
                }
            }
            else
            {
                _logger.LogDebug(
                    "UserMapping 為空,保留所有 {Count} 筆 MR (向後相容模式) - 專案: {ProjectName}",
                    allPullRequests.Count, projectName);
            }

            return filteredPullRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢 GitLab MR 失敗 - 專案: {ProjectName}", projectName);
            throw;
        }
    }

    /// <summary>
    /// 將 NGitLab 的 MergeRequest 模型轉換為 Domain 的 PullRequestInfo
    /// </summary>
    private PullRequestInfo ConvertToPullRequestInfo(MergeRequest mr, string projectName)
    {
        try
        {
            var authorUserId = mr.Author?.Id.ToString();
            var originalDisplayName = mr.Author?.Name;

            // 使用 UserMapping 服務取得映射後的 DisplayName
            // 注意: 現在使用 originalDisplayName 作為 key,因為 authorUsername 已被移除
            var mappedDisplayName = originalDisplayName != null
                ? _userMappingService.GetDisplayName("GitLab", originalDisplayName, originalDisplayName)
                : null;

            return new PullRequestInfo
            {
                Platform = "GitLab",
                Id = mr.Id.ToString(),
                Number = (int)mr.Iid,
                Title = mr.Title ?? string.Empty,
                Description = mr.Description,
                SourceBranch = new BranchName(mr.SourceBranch ?? "unknown"),
                TargetBranch = new BranchName(mr.TargetBranch ?? "unknown"),
                CreatedAt = mr.CreatedAt,
                MergedAt = mr.MergedAt,
                State = mr.State.ToString(),
                AuthorUserId = authorUserId,
                AuthorDisplayName = mappedDisplayName,
                RepositoryName = projectName,
                Url = mr.WebUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "轉換 GitLab MR 失敗 - MR ID: {MrId}, Title: {Title}",
                mr.Id, mr.Title);
            throw new InvalidOperationException($"轉換 GitLab MR 失敗: {mr.Id}", ex);
        }
    }

}
