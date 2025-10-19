using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ReleaseSync.Infrastructure.Platforms.GitLab;

/// <summary>
/// GitLab 平台服務
/// 協調多個 GitLab 專案的 Merge Request 查詢
/// </summary>
public class GitLabService : IPlatformService
{
    private readonly IPullRequestRepository _repository;
    private readonly GitLabSettings _settings;
    private readonly ILogger<GitLabService> _logger;

    /// <summary>
    /// 平台名稱
    /// </summary>
    public string PlatformName => "GitLab";

    /// <summary>
    /// 建立 GitLabService
    /// </summary>
    public GitLabService(
        [FromKeyedServices("GitLab")] IPullRequestRepository repository,
        IOptions<GitLabSettings> settings,
        ILogger<GitLabService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 取得指定時間範圍內所有專案的 Merge Requests
    /// </summary>
    public async Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dateRange, nameof(dateRange));

        _logger.LogInformation("開始從 GitLab 抓取 MR - 時間範圍: {StartDate} ~ {EndDate}, 專案數: {ProjectCount}",
            dateRange.StartDate, dateRange.EndDate, _settings.Projects.Count);

        if (!_settings.Projects.Any())
        {
            _logger.LogWarning("未設定任何 GitLab 專案");
            return Enumerable.Empty<PullRequestInfo>();
        }

        var allPullRequests = new List<PullRequestInfo>();

        // 並行查詢所有專案
        var tasks = _settings.Projects.Select(async project =>
        {
            try
            {
                _logger.LogDebug("開始查詢 GitLab 專案: {ProjectPath}", project.ProjectPath);

                var pullRequests = await _repository.GetPullRequestsAsync(
                    project.ProjectPath,
                    dateRange,
                    project.TargetBranches,
                    cancellationToken);

                var prList = pullRequests.ToList();

                _logger.LogInformation("成功抓取 GitLab 專案 {ProjectPath}: {Count} 筆 MR",
                    project.ProjectPath, prList.Count);

                return prList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "抓取 GitLab 專案 {ProjectPath} 失敗", project.ProjectPath);
                // 不要因為單一專案失敗而中斷其他專案
                return Enumerable.Empty<PullRequestInfo>();
            }
        });

        var results = await Task.WhenAll(tasks);

        // 合併所有專案的結果
        foreach (var projectPullRequests in results)
        {
            allPullRequests.AddRange(projectPullRequests);
        }

        _logger.LogInformation("GitLab 抓取完成 - 總共 {TotalCount} 筆 MR",
            allPullRequests.Count);

        return allPullRequests;
    }
}
