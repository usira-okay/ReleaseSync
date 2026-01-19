using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// 最新 Release Branch 與 Target Branch 比對的 Fetcher
/// 取得「TargetBranch 有,但 Release Branch 沒有」的 commits
/// </summary>
public sealed class LatestReleaseFetcher : ICommitFetcher
{
    private readonly IGitRepository _gitRepository;
    private readonly FetcherConfiguration _configuration;

    /// <summary>
    /// 建立 LatestReleaseFetcher 實例
    /// </summary>
    /// <param name="gitRepository">Git Repository</param>
    /// <param name="configuration">Fetcher 配置</param>
    /// <exception cref="ArgumentNullException">參數為 null</exception>
    /// <exception cref="ArgumentException">配置缺少必要參數</exception>
    public LatestReleaseFetcher(IGitRepository gitRepository, FetcherConfiguration configuration)
    {
        _gitRepository = gitRepository ?? throw new ArgumentNullException(nameof(gitRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(_configuration.ReleaseBranch))
            throw new ArgumentException(
                "ReleaseBranch mode requires ReleaseBranch in configuration.",
                nameof(configuration));

        if (string.IsNullOrWhiteSpace(_configuration.TargetBranch))
            throw new ArgumentException(
                "LatestRelease mode requires TargetBranch in configuration.",
                nameof(configuration));
    }

    /// <summary>
    /// 抓取 TargetBranch 與 ReleaseBranch 的 commit 差異
    /// </summary>
    public async Task<CommitDiff> FetchCommitsAsync(CancellationToken cancellationToken = default)
    {
        // 驗證 Release Branch 存在
        var branchExists = await _gitRepository.BranchExistsAsync(
            _configuration.RepositoryId,
            _configuration.ReleaseBranch!,
            cancellationToken);

        if (!branchExists)
            throw new InvalidOperationException(
                $"Release branch '{_configuration.ReleaseBranch}' does not exist in repository '{_configuration.RepositoryId}'. " +
                $"Please verify the branch name or create the release branch first.");

        // 取得差異:TargetBranch 有但 ReleaseBranch 沒有的 commits
        var commitDiff = await _gitRepository.GetCommitDiffAsync(
            _configuration.RepositoryId,
            _configuration.ReleaseBranch!,      // 來源 (基準)
            _configuration.TargetBranch!,       // 目標 (比較對象)
            cancellationToken);

        return commitDiff;
    }
}
