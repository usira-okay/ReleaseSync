using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// 歷史版本 Release Branch 比對的 Fetcher
/// 取得「下一版 Release 有,但當前 Release 沒有」的 commits
/// </summary>
public sealed class HistoricalReleaseFetcher : ICommitFetcher
{
    private readonly IGitRepository _gitRepository;
    private readonly FetcherConfiguration _configuration;

    /// <summary>
    /// 建立 HistoricalReleaseFetcher 實例
    /// </summary>
    /// <param name="gitRepository">Git Repository</param>
    /// <param name="configuration">Fetcher 配置</param>
    /// <exception cref="ArgumentNullException">參數為 null</exception>
    /// <exception cref="ArgumentException">配置缺少必要參數或版本順序錯誤</exception>
    public HistoricalReleaseFetcher(IGitRepository gitRepository, FetcherConfiguration configuration)
    {
        _gitRepository = gitRepository ?? throw new ArgumentNullException(nameof(gitRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(_configuration.ReleaseBranch))
            throw new ArgumentException(
                "HistoricalRelease mode requires ReleaseBranch in configuration.",
                nameof(configuration));

        if (string.IsNullOrWhiteSpace(_configuration.NextReleaseBranch))
            throw new ArgumentException(
                "HistoricalRelease mode requires NextReleaseBranch in configuration.",
                nameof(configuration));

        // 驗證版本順序
        var currentRelease = new ReleaseBranchName(_configuration.ReleaseBranch);
        var nextRelease = new ReleaseBranchName(_configuration.NextReleaseBranch);

        if (!nextRelease.IsNewerThan(currentRelease))
            throw new ArgumentException(
                $"NextReleaseBranch '{_configuration.NextReleaseBranch}' must be newer than ReleaseBranch '{_configuration.ReleaseBranch}'.",
                nameof(configuration));
    }

    /// <summary>
    /// 抓取兩個 Release Branch 之間的 commit 差異
    /// </summary>
    public async Task<CommitDiff> FetchCommitsAsync(CancellationToken cancellationToken = default)
    {
        // 驗證當前 Release Branch 存在
        var currentBranchExists = await _gitRepository.BranchExistsAsync(
            _configuration.RepositoryId,
            _configuration.ReleaseBranch!,
            cancellationToken);

        if (!currentBranchExists)
            throw new InvalidOperationException(
                $"Release branch '{_configuration.ReleaseBranch}' does not exist in repository '{_configuration.RepositoryId}'.");

        // 驗證下一版 Release Branch 存在
        var nextBranchExists = await _gitRepository.BranchExistsAsync(
            _configuration.RepositoryId,
            _configuration.NextReleaseBranch!,
            cancellationToken);

        if (!nextBranchExists)
            throw new InvalidOperationException(
                $"Next release branch '{_configuration.NextReleaseBranch}' does not exist in repository '{_configuration.RepositoryId}'.");

        // 取得差異:下一版 Release 有但當前 Release 沒有的 commits
        var commitDiff = await _gitRepository.GetCommitDiffAsync(
            _configuration.RepositoryId,
            _configuration.ReleaseBranch!,      // 來源 (舊版本)
            _configuration.NextReleaseBranch!,  // 目標 (新版本)
            cancellationToken);

        return commitDiff;
    }
}
