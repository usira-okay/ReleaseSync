using Microsoft.Extensions.Logging;
using NGitLab.Models;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using LocalModels = ReleaseSync.Infrastructure.Platforms.Models;

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

    /// <summary>
    /// 取得專案的分支清單
    /// </summary>
    protected override async Task<IEnumerable<LocalModels.BranchInfo>> GetBranchesAsync(
        string projectName,
        string? searchPattern,
        CancellationToken cancellationToken)
    {
        return await _apiClient.GetBranchesAsync(projectName, searchPattern, cancellationToken);
    }

    /// <summary>
    /// 比對兩個分支之間的差異
    /// </summary>
    protected override async Task<LocalModels.BranchCompareResult> CompareBranchesAsync(
        string projectName,
        string fromBranch,
        string toBranch,
        CancellationToken cancellationToken)
    {
        return await _apiClient.CompareBranchesAsync(projectName, fromBranch, toBranch, cancellationToken);
    }

    /// <summary>
    /// 根據 Commits 找出對應的 Merge Requests
    /// </summary>
    /// <remarks>
    /// GitLab 的 Merge Commit 訊息格式通常為:
    /// "Merge branch 'feature/xxx' into 'main'"
    /// 或包含 "See merge request !123" 的訊息
    /// </remarks>
    protected override async Task<IEnumerable<PullRequestInfo>> GetPullRequestsFromCommitsAsync(
        string projectName,
        IReadOnlyList<LocalModels.CommitInfo> commits,
        string targetBranch,
        CancellationToken cancellationToken)
    {
        if (commits.Count == 0)
        {
            return Enumerable.Empty<PullRequestInfo>();
        }

        // 從 Commit 訊息中提取 MR IID
        var mrIids = new HashSet<int>();
        foreach (var commit in commits)
        {
            // 嘗試從 "See merge request !123" 格式提取
            var match = System.Text.RegularExpressions.Regex.Match(
                commit.Title ?? string.Empty,
                @"See merge request\s+[^!]*!(\d+)");

            if (match.Success && int.TryParse(match.Groups[1].Value, out var iid))
            {
                mrIids.Add(iid);
            }
        }

        _logger.LogInformation(
            "從 {CommitCount} 個 Commits 中提取到 {MrCount} 個 MR IID - 專案: {ProjectName}",
            commits.Count, mrIids.Count, projectName);

        if (mrIids.Count == 0)
        {
            // 如果無法從 commit 訊息提取 MR IID，嘗試取得最近的 MRs 並比對 merge commit
            _logger.LogWarning(
                "無法從 Commit 訊息提取 MR IID，將返回空結果 - 專案: {ProjectName}",
                projectName);
            return Enumerable.Empty<PullRequestInfo>();
        }

        // 取得對應的 MR 詳細資訊
        // 注意：這裡使用較大的時間範圍來確保能找到對應的 MR
        var now = DateTime.UtcNow;
        var dateRange = new DateRange(now.AddYears(-1), now);

        var allMrs = await FetchPullRequestsFromApiAsync(
            projectName,
            dateRange,
            new[] { targetBranch },
            cancellationToken);

        var matchedMrs = allMrs
            .Where(mr => mrIids.Contains((int)mr.Iid))
            .ToList();

        _logger.LogInformation(
            "找到 {MatchedCount} 個對應的 MR - 專案: {ProjectName}",
            matchedMrs.Count, projectName);

        return matchedMrs.Select(mr => ConvertToPullRequestInfo(mr, projectName));
    }
}
