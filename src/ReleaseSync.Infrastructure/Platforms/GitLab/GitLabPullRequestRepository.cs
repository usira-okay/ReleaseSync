using Microsoft.Extensions.Logging;
using NGitLab.Models;
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

    /// <summary>
    /// 建立 GitLabPullRequestRepository
    /// </summary>
    public GitLabPullRequestRepository(
        GitLabApiClient apiClient,
        ILogger<GitLabPullRequestRepository> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var pullRequests = mergeRequests
                .Select(mr => ConvertToPullRequestInfo(mr, projectName))
                .ToList();

            _logger.LogInformation("成功轉換 {Count} 筆 GitLab MR 為 Domain Model - 專案: {ProjectName}",
                pullRequests.Count, projectName);

            return pullRequests;
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
                AuthorUsername = mr.Author?.Username ?? "unknown",
                AuthorDisplayName = mr.Author?.Name,
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
