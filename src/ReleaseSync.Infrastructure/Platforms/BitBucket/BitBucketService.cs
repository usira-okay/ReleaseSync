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
    protected override List<string> GetTargetBranches(BitBucketProjectSettings project) => project.TargetBranches;
}
