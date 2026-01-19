using ReleaseSync.Domain.Models;

namespace ReleaseSync.Application.Services;

/// <summary>
/// Commit 資料抓取器介面
/// </summary>
public interface ICommitFetcher
{
    /// <summary>
    /// 抓取 Commit 差異資料
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Commit 差異結果</returns>
    Task<CommitDiff> FetchCommitsAsync(CancellationToken cancellationToken = default);
}
