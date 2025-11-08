namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// BitBucket 平台組態
/// </summary>
public class BitBucketSettings
{
    /// <summary>
    /// BitBucket 使用者 Email (用於 Basic Authentication,從 User Secrets 或 appsettings.json 讀取)
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// BitBucket App Password 或 Access Token (從 User Secrets 或 appsettings.json 讀取)
    /// </summary>
    public required string AccessToken { get; init; }

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
