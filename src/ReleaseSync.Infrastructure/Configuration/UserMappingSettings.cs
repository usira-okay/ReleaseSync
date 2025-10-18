namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// 使用者對應設定
/// </summary>
public class UserMappingSettings
{
    /// <summary>
    /// GitLab 與 BitBucket 的使用者對應清單
    /// </summary>
    public List<UserMapping> Mappings { get; init; } = new();
}

/// <summary>
/// 使用者對應
/// </summary>
public class UserMapping
{
    /// <summary>
    /// GitLab 使用者 ID 或 username
    /// </summary>
    public string? GitLabUserId { get; init; }

    /// <summary>
    /// BitBucket 使用者 ID 或 username
    /// </summary>
    public string? BitBucketUserId { get; init; }

    /// <summary>
    /// 顯示名稱 (用於報表)
    /// </summary>
    public required string DisplayName { get; init; }
}
