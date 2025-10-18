using Microsoft.Extensions.Logging;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Infrastructure.Platforms.BitBucket.Models;

namespace ReleaseSync.Infrastructure.Platforms.BitBucket;

/// <summary>
/// BitBucket Pull Request Repository 實作
/// </summary>
public class BitBucketPullRequestRepository : IPullRequestRepository
{
    private readonly BitBucketApiClient _apiClient;
    private readonly ILogger<BitBucketPullRequestRepository> _logger;

    /// <summary>
    /// 建立 BitBucketPullRequestRepository
    /// </summary>
    public BitBucketPullRequestRepository(
        BitBucketApiClient apiClient,
        ILogger<BitBucketPullRequestRepository> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        try
        {
            // 呼叫 API Client 取得 Pull Requests
            var pullRequests = await _apiClient.GetPullRequestsAsync(
                workspace,
                repository,
                dateRange.StartDate,
                dateRange.EndDate,
                cancellationToken);

            // 轉換為 Domain Model
            var domainPullRequests = pullRequests
                .Select(pr => ConvertToPullRequestInfo(pr, projectName))
                .ToList();

            // 如果有指定目標分支,進行過濾
            if (targetBranches != null && targetBranches.Any())
            {
                var targetBranchList = targetBranches.ToList();
                domainPullRequests = domainPullRequests
                    .Where(pr => targetBranchList.Contains(pr.TargetBranch.Value))
                    .ToList();

                _logger.LogDebug("已過濾目標分支: {TargetBranches}, 剩餘 {Count} 筆 PR",
                    string.Join(", ", targetBranchList), domainPullRequests.Count);
            }

            _logger.LogInformation("成功轉換 {Count} 筆 BitBucket PR 為 Domain Model - 專案: {ProjectName}",
                domainPullRequests.Count, projectName);

            return domainPullRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢 BitBucket PR 失敗 - 專案: {ProjectName}", projectName);
            throw;
        }
    }

    /// <summary>
    /// 將 BitBucket 的 PullRequest 模型轉換為 Domain 的 PullRequestInfo
    /// </summary>
    private PullRequestInfo ConvertToPullRequestInfo(BitBucketPullRequest pr, string projectName)
    {
        try
        {
            // 計算 MergedAt 時間 (BitBucket 不直接提供,使用 UpdatedOn 作為近似值)
            DateTime? mergedAt = null;
            if (pr.State.Equals("MERGED", StringComparison.OrdinalIgnoreCase))
            {
                mergedAt = pr.UpdatedOn;
            }

            return new PullRequestInfo
            {
                Platform = "BitBucket",
                Id = pr.Id.ToString(),
                Number = pr.Id,
                Title = pr.Title ?? string.Empty,
                Description = pr.Description,
                SourceBranch = new BranchName(pr.Source?.Branch?.Name ?? "unknown"),
                TargetBranch = new BranchName(pr.Destination?.Branch?.Name ?? "unknown"),
                CreatedAt = pr.CreatedOn,
                MergedAt = mergedAt,
                State = ConvertState(pr.State),
                AuthorUsername = pr.Author?.Username ?? "unknown",
                AuthorDisplayName = pr.Author?.DisplayName,
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
