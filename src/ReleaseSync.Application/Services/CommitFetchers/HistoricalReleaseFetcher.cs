using ReleaseSync.Application.Models;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// 使用舊版與新版 Release Branch 比對的 Commit Fetcher
/// </summary>
public class HistoricalReleaseFetcher : ICommitFetcher
{
    private readonly IGitRepository _gitRepository;
    private readonly FetcherConfiguration _configuration;

    /// <summary>
    /// 建構子
    /// </summary>
    public HistoricalReleaseFetcher(
        IGitRepository gitRepository,
        FetcherConfiguration configuration)
    {
        _gitRepository = gitRepository ?? throw new ArgumentNullException(nameof(gitRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (_configuration.ReleaseBranch is null)
        {
            throw new ArgumentException(
                "HistoricalReleaseFetcher 需要配置 ReleaseBranch",
                nameof(configuration)
            );
        }

        if (_configuration.NextReleaseBranch is null)
        {
            throw new ArgumentException(
                "HistoricalReleaseFetcher 需要配置 NextReleaseBranch",
                nameof(configuration)
            );
        }

        // 驗證版本順序
        if (!_configuration.NextReleaseBranch.IsNewerThan(_configuration.ReleaseBranch))
        {
            throw new ArgumentException(
                $"NextReleaseBranch ({_configuration.NextReleaseBranch.Value}) " +
                $"必須比 ReleaseBranch ({_configuration.ReleaseBranch.Value}) 更新",
                nameof(configuration)
            );
        }
    }

    /// <summary>
    /// 抓取新版 Release 有但舊版 Release 沒有的 Commit 差異
    /// </summary>
    public async Task<CommitDiff> FetchCommitsAsync(CancellationToken cancellationToken = default)
    {
        // 檢查兩個 ReleaseBranch 是否都存在
        bool currentBranchExists = await _gitRepository.BranchExistsAsync(
            _configuration.RepositoryId,
            _configuration.ReleaseBranch!.Value,
            cancellationToken
        );

        if (!currentBranchExists)
        {
            throw new InvalidOperationException(
                $"Release Branch '{_configuration.ReleaseBranch.Value}' 不存在於 repository '{_configuration.RepositoryId}'。" +
                $"請確認分支名稱是否正確,或檢查該分支是否已在遠端建立。"
            );
        }

        bool nextBranchExists = await _gitRepository.BranchExistsAsync(
            _configuration.RepositoryId,
            _configuration.NextReleaseBranch!.Value,
            cancellationToken
        );

        if (!nextBranchExists)
        {
            throw new InvalidOperationException(
                $"Next Release Branch '{_configuration.NextReleaseBranch.Value}' 不存在於 repository '{_configuration.RepositoryId}'。" +
                $"請確認分支名稱是否正確,或檢查該分支是否已在遠端建立。"
            );
        }

        // 取得新版 Release 有但舊版 Release 沒有的 commits
        var commitDiff = await _gitRepository.GetCommitDiffAsync(
            _configuration.RepositoryId,
            _configuration.NextReleaseBranch.Value,
            _configuration.ReleaseBranch.Value,
            cancellationToken
        );

        return commitDiff;
    }
}
