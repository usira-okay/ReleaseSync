using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.Services;

/// <summary>
/// 平台服務介面
/// 定義各平台 (GitLab, BitBucket, AzureDevOps) 的統一介面
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// 平台名稱
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// 取得指定時間範圍的 Pull Requests / Merge Requests
    /// </summary>
    /// <param name="dateRange">時間範圍</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Pull Request 清單</returns>
    Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default);
}
