using Microsoft.Extensions.Logging;
using NGitLab.Models;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;

namespace ReleaseSync.Infrastructure.Platforms.GitLab;

/// <summary>
/// GitLab Pull Request (Merge Request) Repository 實作
/// </summary>
public class GitLabPullRequestRepository : BasePullRequestRepository<MergeRequest>
{
    private readonly GitLabApiClient _apiClient;

    /// <summary>
    /// 平台名稱
    /// </summary>
    protected override string PlatformName => "GitLab";

    /// <summary>
    /// 建立 GitLabPullRequestRepository
    /// </summary>
    public GitLabPullRequestRepository(
        GitLabApiClient apiClient,
        ILogger<GitLabPullRequestRepository> logger,
        IUserMappingService userMappingService)
        : base(logger, userMappingService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <summary>
    /// 從 API 取得 Merge Requests
    /// </summary>
    protected override async Task<IEnumerable<MergeRequest>> FetchPullRequestsFromApiAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string>? targetBranches,
        CancellationToken cancellationToken)
    {
        return await _apiClient.GetMergeRequestsAsync(
            projectName,
            dateRange.StartDate,
            dateRange.EndDate,
            targetBranches,
            cancellationToken);
    }

    /// <summary>
    /// 將 NGitLab 的 MergeRequest 模型轉換為 Domain 的 PullRequestInfo
    /// </summary>
    protected override PullRequestInfo ConvertToPullRequestInfo(MergeRequest mr, string projectName)
    {
        try
        {
            var authorUserId = mr.Author?.Id.ToString();
            var originalDisplayName = mr.Author?.Name;

            // 使用 UserMapping 服務取得映射後的 DisplayName
            var mappedDisplayName = originalDisplayName != null
                ? _userMappingService.GetDisplayName(PlatformName, authorUserId, originalDisplayName)
                : null;

            return new PullRequestInfo
            {
                Platform = PlatformName,
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
