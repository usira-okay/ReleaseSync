using Microsoft.Extensions.Logging;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Platforms.BitBucket.Models;
using ReleaseSync.Infrastructure.Platforms.Models;

namespace ReleaseSync.Infrastructure.Platforms.BitBucket;

/// <summary>
/// BitBucket Pull Request Repository 實作
/// </summary>
public class BitBucketPullRequestRepository : BasePullRequestRepository<BitBucketPullRequest>
{
    private readonly BitBucketApiClient _apiClient;

    /// <summary>
    /// 平台名稱
    /// </summary>
    protected override string PlatformName => "BitBucket";

    /// <summary>
    /// 建立 BitBucketPullRequestRepository
    /// </summary>
    public BitBucketPullRequestRepository(
        BitBucketApiClient apiClient,
        ILogger<BitBucketPullRequestRepository> logger,
        IUserMappingService userMappingService)
        : base(logger, userMappingService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <summary>
    /// 從 API 取得 Pull Requests
    /// </summary>
    protected override async Task<IEnumerable<BitBucketPullRequest>> FetchPullRequestsFromApiAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string>? targetBranches,
        CancellationToken cancellationToken)
    {
        // 解析 workspace/repository 格式
        var parts = projectName.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException(
                $"BitBucket 專案名稱格式錯誤,應為 'workspace/repository': {projectName}",
                nameof(projectName));
        }

        var workspace = parts[0];
        var repository = parts[1];

        var pullRequests = await _apiClient.GetPullRequestsAsync(
            workspace,
            repository,
            dateRange.StartDate,
            dateRange.EndDate,
            cancellationToken);

        // 雙重保險: 過濾掉非 MERGED 狀態的 PR
        // (雖然 API 查詢已設定 state=MERGED,但仍在程式碼中額外過濾以確保資料正確性)
        var mergedPullRequests = pullRequests
            .Where(pr => pr.State.Equals("MERGED", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (mergedPullRequests.Count < pullRequests.Count())
        {
            var filteredCount = pullRequests.Count() - mergedPullRequests.Count;
            _logger.LogWarning(
                "BitBucket API 回傳了 {FilteredCount} 筆非 MERGED 狀態的 PR,已過濾 - Workspace: {Workspace}, Repo: {Repository}",
                filteredCount, workspace, repository);
        }

        return mergedPullRequests;
    }

    /// <summary>
    /// 套用目標分支過濾
    /// BitBucket API 不支援分支過濾，需在此處理
    /// </summary>
    protected override List<PullRequestInfo> ApplyTargetBranchFilter(
        List<PullRequestInfo> pullRequests,
        IEnumerable<string>? targetBranches,
        string projectName)
    {
        if (targetBranches == null || !targetBranches.Any())
        {
            return pullRequests;
        }

        var targetBranchList = targetBranches.ToList();
        var filtered = pullRequests
            .Where(pr => targetBranchList.Contains(pr.TargetBranch.Value))
            .ToList();

        _logger.LogInformation("已過濾目標分支: {TargetBranches}, 剩餘 {Count} 筆 PR",
            string.Join(", ", targetBranchList), filtered.Count);

        return filtered;
    }

    /// <summary>
    /// 將 BitBucket 的 PullRequest 模型轉換為 Domain 的 PullRequestInfo
    /// </summary>
    protected override PullRequestInfo ConvertToPullRequestInfo(BitBucketPullRequest pr, string projectName)
    {
        try
        {
            // 使用 ClosedOn 作為 MergedAt 時間 (PR 合併時的實際時間)
            DateTime? mergedAt = null;
            if (pr.State.Equals("MERGED", StringComparison.OrdinalIgnoreCase))
            {
                mergedAt = pr.ClosedOn ?? pr.UpdatedOn;
            }

            var authorUserId = pr.Author?.UuId;
            var originalDisplayName = pr.Author?.DisplayName;

            // 使用 UserMapping 服務取得映射後的 DisplayName
            var mappedDisplayName = originalDisplayName != null
                ? _userMappingService.GetDisplayName(PlatformName, authorUserId, originalDisplayName)
                : null;

            return new PullRequestInfo
            {
                Platform = PlatformName,
                Id = pr.Id.ToString(),
                Number = pr.Id,
                Title = pr.Title ?? string.Empty,
                Description = pr.Description,
                SourceBranch = new BranchName(pr.Source?.Branch?.Name ?? "unknown"),
                TargetBranch = new BranchName(pr.Destination?.Branch?.Name ?? "unknown"),
                CreatedAt = EnsureUtc(pr.CreatedOn),
                MergedAt = mergedAt.HasValue ? EnsureUtc(mergedAt.Value) : null,
                State = ConvertState(pr.State),
                AuthorUserId = authorUserId,
                AuthorDisplayName = mappedDisplayName,
                RepositoryName = projectName,
                Url = pr.Links?.Html?.Href
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "轉換 BitBucket PR 失敗 - PR ID: {PrId}, Title: {Title}",
                pr.Id, pr.Title);
            throw new InvalidOperationException($"轉換 BitBucket PR 失敗: {pr.Id}", ex);
        }
    }

    /// <summary>
    /// 確保 DateTime 為 UTC 時間
    /// </summary>
    private static DateTime EnsureUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }
        
        // 如果是 Unspecified，假設為 UTC (BitBucket API 回傳 UTC 時間)
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        
        // 如果是 Local，轉換為 UTC
        return dateTime.ToUniversalTime();
    }

    /// <summary>
    /// 轉換 BitBucket Pull Request 狀態為標準化狀態
    /// </summary>
    private static string ConvertState(string state)
    {
        return state?.ToUpperInvariant() switch
        {
            "OPEN" => "Open",
            "MERGED" => "Merged",
            "DECLINED" => "Declined",
            "SUPERSEDED" => "Superseded",
            _ => state ?? "Unknown"
        };
    }

    /// <summary>
    /// 解析 workspace 和 repository 名稱
    /// </summary>
    private static (string workspace, string repository) ParseProjectName(string projectName)
    {
        var parts = projectName.Split('/', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException(
                $"BitBucket 專案名稱格式錯誤,應為 'workspace/repository': {projectName}",
                nameof(projectName));
        }
        return (parts[0], parts[1]);
    }

    /// <summary>
    /// 取得專案的分支清單
    /// </summary>
    protected override async Task<IEnumerable<BranchInfo>> GetBranchesAsync(
        string projectName,
        string? searchPattern,
        CancellationToken cancellationToken)
    {
        var (workspace, repository) = ParseProjectName(projectName);
        return await _apiClient.GetBranchesAsync(workspace, repository, searchPattern, cancellationToken);
    }

    /// <summary>
    /// 比對兩個分支之間的差異
    /// </summary>
    protected override async Task<BranchCompareResult> CompareBranchesAsync(
        string projectName,
        string fromBranch,
        string toBranch,
        CancellationToken cancellationToken)
    {
        var (workspace, repository) = ParseProjectName(projectName);
        return await _apiClient.CompareBranchesAsync(workspace, repository, fromBranch, toBranch, cancellationToken);
    }

    /// <summary>
    /// 根據 Commits 找出對應的 Pull Requests
    /// </summary>
    /// <remarks>
    /// BitBucket 的 Merge Commit 訊息格式通常為:
    /// "Merged in feature/xxx (pull request #123)"
    /// </remarks>
    protected override async Task<IEnumerable<PullRequestInfo>> GetPullRequestsFromCommitsAsync(
        string projectName,
        IReadOnlyList<CommitInfo> commits,
        string targetBranch,
        CancellationToken cancellationToken)
    {
        if (commits.Count == 0)
        {
            return Enumerable.Empty<PullRequestInfo>();
        }

        // 從 Commit 訊息中提取 PR ID
        var prIds = new HashSet<int>();
        foreach (var commit in commits)
        {
            // 嘗試從 "(pull request #123)" 格式提取
            var match = System.Text.RegularExpressions.Regex.Match(
                commit.Title ?? string.Empty,
                @"\(pull request #(\d+)\)");

            if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
            {
                prIds.Add(id);
            }
        }

        _logger.LogInformation(
            "從 {CommitCount} 個 Commits 中提取到 {PrCount} 個 PR ID - 專案: {ProjectName}",
            commits.Count, prIds.Count, projectName);

        if (prIds.Count == 0)
        {
            _logger.LogWarning(
                "無法從 Commit 訊息提取 PR ID，將返回空結果 - 專案: {ProjectName}",
                projectName);
            return Enumerable.Empty<PullRequestInfo>();
        }

        // 取得對應的 PR 詳細資訊
        var now = DateTime.UtcNow;
        var dateRange = new DateRange(now.AddYears(-1), now);

        var allPrs = await FetchPullRequestsFromApiAsync(
            projectName,
            dateRange,
            new[] { targetBranch },
            cancellationToken);

        var matchedPrs = allPrs
            .Where(pr => prIds.Contains(pr.Id))
            .ToList();

        _logger.LogInformation(
            "找到 {MatchedCount} 個對應的 PR - 專案: {ProjectName}",
            matchedPrs.Count, projectName);

        return matchedPrs.Select(pr => ConvertToPullRequestInfo(pr, projectName));
    }
}
