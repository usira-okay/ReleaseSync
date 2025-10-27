using Microsoft.Extensions.Logging;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Platforms.BitBucket.Models;

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

        return await _apiClient.GetPullRequestsAsync(
            workspace,
            repository,
            dateRange.StartDate,
            dateRange.EndDate,
            cancellationToken);
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

        _logger.LogDebug("已過濾目標分支: {TargetBranches}, 剩餘 {Count} 筆 PR",
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
            // 計算 MergedAt 時間 (BitBucket 不直接提供,使用 UpdatedOn 作為近似值)
            DateTime? mergedAt = null;
            if (pr.State.Equals("MERGED", StringComparison.OrdinalIgnoreCase))
            {
                mergedAt = pr.UpdatedOn;
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
                CreatedAt = pr.CreatedOn,
                MergedAt = mergedAt,
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
}
