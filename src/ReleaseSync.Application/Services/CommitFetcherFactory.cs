using ReleaseSync.Application.Models;
using ReleaseSync.Application.Services.CommitFetchers;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services;

/// <summary>
/// Commit Fetcher 工廠,根據配置建立對應的 Fetcher 實例
/// </summary>
public class CommitFetcherFactory
{
    private readonly IPullRequestRepository _pullRequestRepository;
    private readonly IGitRepository _gitRepository;

    /// <summary>
    /// 建構子
    /// </summary>
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
    /// <returns>ICommitFetcher 實例</returns>
    public ICommitFetcher Create(FetcherConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return configuration.FetchMode switch
        {
            FetchMode.DateRange => CreateDateRangeFetcher(configuration),
            FetchMode.ReleaseBranch => CreateReleaseBranchFetcher(configuration),
            _ => throw new ArgumentException(
                $"不支援的 FetchMode: {configuration.FetchMode}",
                nameof(configuration)
            )
        };
    }

    /// <summary>
    /// 建立 DateRangeFetcher
    /// </summary>
    private ICommitFetcher CreateDateRangeFetcher(FetcherConfiguration configuration)
    {
        if (configuration.DateRange is null)
        {
            throw new ArgumentException(
                "當 FetchMode 為 DateRange 時,必須提供 DateRange 配置",
                nameof(configuration)
            );
        }

        return new DateRangeFetcher(_pullRequestRepository, configuration);
    }

    /// <summary>
    /// 建立 ReleaseBranch 相關的 Fetcher (Latest 或 Historical)
    /// </summary>
    private ICommitFetcher CreateReleaseBranchFetcher(FetcherConfiguration configuration)
    {
        if (configuration.ReleaseBranch is null)
        {
            throw new ArgumentException(
                "當 FetchMode 為 ReleaseBranch 時,必須提供 ReleaseBranch 配置",
                nameof(configuration)
            );
        }

        // 如果有提供 NextReleaseBranch,則使用 HistoricalReleaseFetcher
        if (configuration.NextReleaseBranch is not null)
        {
            return new HistoricalReleaseFetcher(_gitRepository, configuration);
        }

        // 否則使用 LatestReleaseFetcher
        return new LatestReleaseFetcher(_gitRepository, configuration);
    }
}
