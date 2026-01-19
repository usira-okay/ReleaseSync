using ReleaseSync.Domain.Repositories;
using ReleaseSync.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ReleaseSync.Infrastructure.Platforms.GitLab;

/// <summary>
/// GitLab 平台服務
/// 協調多個 GitLab 專案的 Merge Request 查詢
/// </summary>
public class GitLabService : BasePlatformService<GitLabProjectSettings>
{
    private readonly GitLabSettings _settings;

    /// <summary>
    /// 平台名稱
    /// </summary>
    public override string PlatformName => "GitLab";

    /// <summary>
    /// 建立 GitLabService
    /// </summary>
    public GitLabService(
        [FromKeyedServices("GitLab")] IPullRequestRepository repository,
        IOptions<GitLabSettings> settings,
        ILogger<GitLabService> logger)
        : base(repository, logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// 取得所有專案設定
    /// </summary>
    protected override IEnumerable<GitLabProjectSettings> GetProjects() => _settings.Projects;

    /// <summary>
    /// 取得專案識別字串
    /// </summary>
    protected override string GetProjectIdentifier(GitLabProjectSettings project) => project.ProjectPath;

    /// <summary>
    /// 取得專案的 Repository 路徑
    /// </summary>
    protected override string GetRepositoryPath(GitLabProjectSettings project) => project.ProjectPath;

    /// <summary>
    /// 取得專案的目標分支清單
    /// </summary>
    protected override List<string> GetTargetBranches(GitLabProjectSettings project)
    {
        // 將單一 TargetBranch 轉換為清單
        return string.IsNullOrWhiteSpace(project.TargetBranch)
            ? new List<string>()
            : new List<string> { project.TargetBranch };
    }
}
