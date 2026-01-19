using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// Commit Fetcher 工廠
/// 根據配置建立對應的 Fetcher 實例
/// </summary>
public sealed class CommitFetcherFactory
{
    private readonly IPullRequestRepository _pullRequestRepository;
    private readonly IGitRepository _gitRepository;

    /// <summary>
    /// 建立 CommitFetcherFactory 實例
    /// </summary>
    /// <param name="pullRequestRepository">Pull Request Repository</param>
    /// <param name="gitRepository">Git Repository</param>
    public CommitFetcherFactory(
        IPullRequestRepository pullRequestRepository,
        IGitRepository gitRepository)
    {
        _pullRequestRepository = pullRequestRepository ?? throw new ArgumentNullException(nameof(pullRequestRepository));
        _gitRepository = gitRepository ?? throw new ArgumentNullException(nameof(gitRepository));
    }

    /// <summary>
    /// 根據配置建立對應的 Commit Fetcher
    /// </summary>
    /// <param name="configuration">Fetcher 配置</param>
    /// <returns>Commit Fetcher 實例</returns>
    /// <exception cref="ArgumentException">配置無效或不支援的模式</exception>
    public ICommitFetcher Create(FetcherConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        return configuration.FetchMode switch
        {
            FetchMode.DateRange => CreateDateRangeFetcher(configuration),
            FetchMode.ReleaseBranch => CreateReleaseBranchFetcher(configuration),
            _ => throw new ArgumentException($"Unsupported fetch mode: {configuration.FetchMode}", nameof(configuration))
        };
    }

    /// <summary>
    /// 建立 DateRange 模式的 Fetcher
    /// </summary>
    private ICommitFetcher CreateDateRangeFetcher(FetcherConfiguration configuration)
    {
        return new DateRangeFetcher(_pullRequestRepository, configuration);
    }

    /// <summary>
    /// 建立 ReleaseBranch 模式的 Fetcher
    /// </summary>
    private ICommitFetcher CreateReleaseBranchFetcher(FetcherConfiguration configuration)
    {
        // 判斷是最新版本比對還是歷史版本比對
        if (string.IsNullOrWhiteSpace(configuration.NextReleaseBranch))
        {
            // 場景 2: 最新版本 - Release Branch ↔ TargetBranch
            return new LatestReleaseFetcher(_gitRepository, configuration);
        }
        else
        {
            // 場景 3: 歷史版本 - 當前 Release ↔ 下一版 Release
            return new HistoricalReleaseFetcher(_gitRepository, configuration);
        }
    }
}
