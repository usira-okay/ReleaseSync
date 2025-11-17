using Microsoft.Extensions.Logging;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;

namespace ReleaseSync.Infrastructure.Platforms;

/// <summary>
/// 平台服務抽象基礎類別
/// 提供共同的 Repository 查詢邏輯
/// </summary>
/// <typeparam name="TProjectSettings">專案設定型別</typeparam>
public abstract class BasePlatformService<TProjectSettings> : IPlatformService
{
    protected readonly IPullRequestRepository _repository;
    protected readonly ILogger _logger;

    /// <summary>
    /// 平台名稱
    /// </summary>
    public abstract string PlatformName { get; }

    /// <summary>
    /// 取得所有專案設定
    /// </summary>
    protected abstract IEnumerable<TProjectSettings> GetProjects();

    /// <summary>
    /// 取得專案識別字串 (用於 Log)
    /// </summary>
    protected abstract string GetProjectIdentifier(TProjectSettings project);

    /// <summary>
    /// 取得專案的 Repository 路徑
    /// </summary>
    protected abstract string GetRepositoryPath(TProjectSettings project);

    /// <summary>
    /// 取得專案的目標分支清單
    /// </summary>
    protected abstract List<string> GetTargetBranches(TProjectSettings project);

    /// <summary>
    /// 建立 BasePlatformService
    /// </summary>
    protected BasePlatformService(
        IPullRequestRepository repository,
        ILogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 取得指定時間範圍內所有專案的 Pull Requests
    /// </summary>
    public async Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dateRange, nameof(dateRange));

        var projects = GetProjects().ToList();

        if (!projects.Any())
        {
            _logger.LogWarning("未設定任何 {Platform} 專案", PlatformName);
            return Enumerable.Empty<PullRequestInfo>();
        }

        // 並行查詢所有專案
        var projectTasks = projects.Select(async project =>
        {
            try
            {
                var projectId = GetProjectIdentifier(project);

                var pullRequests = await _repository.GetPullRequestsAsync(
                    GetRepositoryPath(project),
                    dateRange,
                    GetTargetBranches(project),
                    cancellationToken);

                var prList = pullRequests.ToList();

                _logger.LogInformation("{Platform} - {ProjectId}: {Count} 筆 PR/MR",
                    PlatformName, projectId, prList.Count);

                return prList;
            }
            catch (Exception ex)
            {
                var projectId = GetProjectIdentifier(project);
                _logger.LogError(ex, "抓取 {Platform} 專案 {ProjectId} 失敗", PlatformName, projectId);
                // 不要因為單一專案失敗而中斷其他專案
                return new List<PullRequestInfo>();
            }
        });

        // 等待所有專案完成並彙整結果
        var projectResults = await Task.WhenAll(projectTasks);
        var allPullRequests = projectResults.SelectMany(pr => pr).ToList();

        _logger.LogInformation("{Platform} 完成 - 總共 {TotalCount} 筆 PR/MR",
            PlatformName, allPullRequests.Count);

        return allPullRequests;
    }
}
