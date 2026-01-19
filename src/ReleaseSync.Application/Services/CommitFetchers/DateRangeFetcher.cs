using ReleaseSync.Application.Models;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// 使用時間範圍抓取 PR 的 Commit Fetcher
/// </summary>
public class DateRangeFetcher : ICommitFetcher
{
    private readonly IPullRequestRepository _repository;
    private readonly FetcherConfiguration _configuration;

    /// <summary>
    /// 建構子
    /// </summary>
    public DateRangeFetcher(
        IPullRequestRepository repository,
        FetcherConfiguration configuration)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (_configuration.DateRange is null)
        {
            throw new ArgumentException(
                "DateRangeFetcher 需要配置 DateRange",
                nameof(configuration)
            );
        }
    }

    /// <summary>
    /// 抓取指定時間範圍內的 PR Commit 資料
    /// </summary>
    public async Task<CommitDiff> FetchCommitsAsync(CancellationToken cancellationToken = default)
    {
        var pullRequests = await _repository.GetPullRequestsAsync(
            _configuration.RepositoryId,
            _configuration.DateRange!,
            new[] { _configuration.TargetBranch },
            cancellationToken
        );

        var commits = pullRequests
            .SelectMany(pr => pr.Commits)
            .Select(commitId => new CommitHash(commitId))
            .Distinct()
            .ToList();

        return new CommitDiff
        {
            RepositoryId = _configuration.RepositoryId,
            SourceBranch = $"DateRange({_configuration.DateRange!.StartDate:yyyy-MM-dd} ~ {_configuration.DateRange.EndDate:yyyy-MM-dd})",
            TargetBranch = _configuration.TargetBranch,
            Commits = commits
        };
    }
}
