using ReleaseSync.Application.Models;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// 使用最新版 Release Branch 與 TargetBranch 比對的 Commit Fetcher
/// </summary>
public class LatestReleaseFetcher : ICommitFetcher
{
    private readonly IGitRepository _gitRepository;
    private readonly FetcherConfiguration _configuration;

    /// <summary>
    /// 建構子
    /// </summary>
    public LatestReleaseFetcher(
        IGitRepository gitRepository,
        FetcherConfiguration configuration)
    {
        _gitRepository = gitRepository ?? throw new ArgumentNullException(nameof(gitRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (_configuration.ReleaseBranch is null)
        {
            throw new ArgumentException(
                "LatestReleaseFetcher 需要配置 ReleaseBranch",
                nameof(configuration)
            );
        }
    }

    /// <summary>
    /// 抓取 TargetBranch 有但 ReleaseBranch 沒有的 Commit 差異
    /// </summary>
    public async Task<CommitDiff> FetchCommitsAsync(CancellationToken cancellationToken = default)
    {
        // 檢查 ReleaseBranch 是否存在
        bool branchExists = await _gitRepository.BranchExistsAsync(
            _configuration.RepositoryId,
            _configuration.ReleaseBranch!.Value,
            cancellationToken
        );

        if (!branchExists)
        {
            throw new InvalidOperationException(
                $"Release Branch '{_configuration.ReleaseBranch.Value}' 不存在於 repository '{_configuration.RepositoryId}'。" +
                $"請確認分支名稱是否正確,或檢查該分支是否已在遠端建立。"
            );
        }

        // 取得 TargetBranch 有但 ReleaseBranch 沒有的 commits
        var commitDiff = await _gitRepository.GetCommitDiffAsync(
            _configuration.RepositoryId,
            _configuration.TargetBranch,
            _configuration.ReleaseBranch.Value,
            cancellationToken
        );

        return commitDiff;
    }
}
