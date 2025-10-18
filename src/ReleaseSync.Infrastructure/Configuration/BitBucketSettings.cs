namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// BitBucket 平台組態
/// </summary>
public class BitBucketSettings
{
    /// <summary>
    /// App Password (從 appsettings.secure.json 讀取)
    /// </summary>
    public required string AppPassword { get; init; }

    /// <summary>
    /// BitBucket API 端點 URL
    /// </summary>
    public required string ApiUrl { get; init; }

    /// <summary>
    /// 需要同步的 BitBucket 專案清單
    /// </summary>
    public List<BitBucketProjectSettings> Projects { get; init; } = new();
}

/// <summary>
/// BitBucket 專案組態
/// </summary>
public class BitBucketProjectSettings
{
    /// <summary>
    /// 專案路徑 (格式: workspace/repository)
    /// </summary>
    public required string WorkspaceAndRepo { get; init; }

    /// <summary>
    /// 需要同步的目標分支清單 (空陣列表示所有分支)
    /// </summary>
    public List<string> TargetBranches { get; init; } = new();
}
