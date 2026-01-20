using ReleaseSync.Domain.Repositories;
using ReleaseSync.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ReleaseSync.Infrastructure.Platforms.BitBucket;

/// <summary>
/// BitBucket 平台服務
/// 協調多個 BitBucket Repository 的 Pull Request 查詢
/// </summary>
public class BitBucketService : BasePlatformService<BitBucketProjectSettings>
{
    private readonly BitBucketSettings _settings;

    /// <summary>
    /// 平台名稱
    /// </summary>
    public override string PlatformName => "BitBucket";

    /// <summary>
    /// 建立 BitBucketService
    /// </summary>
    public BitBucketService(
        [FromKeyedServices("BitBucket")] IPullRequestRepository repository,
        IOptions<BitBucketSettings> settings,
        ILogger<BitBucketService> logger)
        : base(repository, logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// 取得所有專案設定
    /// </summary>
    protected override IEnumerable<BitBucketProjectSettings> GetProjects() => _settings.Projects;

    /// <summary>
    /// 取得專案識別字串
    /// </summary>
    protected override string GetProjectIdentifier(BitBucketProjectSettings project) => project.WorkspaceAndRepo;

    /// <summary>
    /// 取得專案的 Repository 路徑
    /// </summary>
    protected override string GetRepositoryPath(BitBucketProjectSettings project) => project.WorkspaceAndRepo;

    /// <summary>
    /// 取得專案的目標分支清單
    /// </summary>
    /// <remarks>
    /// 優先使用新的 TargetBranch 單一值屬性，
    /// 若未設定則回退至舊的 TargetBranches 陣列（向後相容）
    /// </remarks>
#pragma warning disable CS0618 // 允許存取已標記為 Obsolete 的 TargetBranches 屬性以提供向後相容性
    protected override List<string> GetTargetBranches(BitBucketProjectSettings project)
    {
        // 優先使用 TargetBranch 單一值
        if (!string.IsNullOrWhiteSpace(project.TargetBranch))
        {
            return new List<string> { project.TargetBranch };
        }

        // 向後相容：使用舊的 TargetBranches 陣列
        return project.TargetBranches;
    }
#pragma warning restore CS0618

    /// <summary>
    /// 取得專案的目標分支（單一）
    /// </summary>
    /// <remarks>
    /// 用於 Release Branch 比對模式，需要單一目標分支
    /// </remarks>
#pragma warning disable CS0618 // 允許存取已標記為 Obsolete 的 TargetBranches 屬性以提供向後相容性
    protected override string GetTargetBranch(BitBucketProjectSettings project)
    {
        // 優先使用 TargetBranch 單一值
        if (!string.IsNullOrWhiteSpace(project.TargetBranch))
        {
            return project.TargetBranch;
        }

        // 向後相容：使用舊的 TargetBranches 陣列的第一個元素
        return project.TargetBranches.FirstOrDefault() ?? "main";
    }
#pragma warning restore CS0618
}
