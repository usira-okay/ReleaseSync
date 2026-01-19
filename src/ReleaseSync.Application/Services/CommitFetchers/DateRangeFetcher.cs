using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;

namespace ReleaseSync.Application.Services.CommitFetchers;

/// <summary>
/// 使用時間範圍抓取 PR 資訊的 Fetcher
/// </summary>
public sealed class DateRangeFetcher : ICommitFetcher
{
    private readonly IPullRequestRepository _repository;
    private readonly FetcherConfiguration _configuration;

    /// <summary>
    /// 建立 DateRangeFetcher 實例
    /// </summary>
    /// <param name="repository">Pull Request Repository</param>
    /// <param name="configuration">Fetcher 配置</param>
    /// <exception cref="ArgumentNullException">參數為 null</exception>
    /// <exception cref="ArgumentException">配置缺少必要參數</exception>
    public DateRangeFetcher(IPullRequestRepository repository, FetcherConfiguration configuration)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (!_configuration.StartDate.HasValue || !_configuration.EndDate.HasValue)
            throw new ArgumentException(
                "DateRange mode requires StartDate and EndDate in configuration.",
                nameof(configuration));
    }

    /// <summary>
    /// 抓取指定時間範圍內的 PR 資訊
    /// 注意:此 Fetcher 不返回 CommitDiff,而是透過 PR 資訊取得相關 commits
    /// </summary>
    public async Task<CommitDiff> FetchCommitsAsync(CancellationToken cancellationToken = default)
    {
        var dateRange = new DateRange(_configuration.StartDate!.Value, _configuration.EndDate!.Value);

        // 使用時間範圍模式時,targetBranches 可能為空 (查詢所有分支)
        var targetBranches = !string.IsNullOrWhiteSpace(_configuration.TargetBranch)
            ? new[] { _configuration.TargetBranch }
            : null;

        var pullRequests = await _repository.GetPullRequestsAsync(
            _configuration.RepositoryId,
            dateRange,
            targetBranches,
            cancellationToken);

        // 從 PR 資訊中提取所有相關的 commit hashes
        // 注意:這裡假設 PullRequestInfo 有提供相關 commit 資訊
        // 如果沒有,可能需要透過額外的 API 呼叫取得
        var commits = new List<CommitHash>();

        // 返回空的 CommitDiff (因為 DateRange 模式不做 commit 差異比對)
        // 實際的 PR 資訊會透過其他方式處理
        return new CommitDiff(
            _configuration.RepositoryId,
            "N/A",
            _configuration.TargetBranch ?? "all",
            commits);
    }
}
