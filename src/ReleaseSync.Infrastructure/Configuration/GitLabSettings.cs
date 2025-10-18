namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// GitLab 平台組態
/// </summary>
public class GitLabSettings
{
    /// <summary>
    /// Personal Access Token (從 appsettings.secure.json 讀取)
    /// </summary>
    public required string PersonalAccessToken { get; init; }

    /// <summary>
    /// GitLab API 端點 URL
    /// </summary>
    public required string ApiUrl { get; init; }

    /// <summary>
    /// 需要同步的 GitLab 專案清單
    /// </summary>
    public List<GitLabProjectSettings> Projects { get; init; } = new();
}

/// <summary>
/// GitLab 專案組態
/// </summary>
public class GitLabProjectSettings
{
    /// <summary>
    /// 專案路徑 (格式: group/project)
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// 需要同步的目標分支清單 (空陣列表示所有分支)
    /// </summary>
    public List<string> TargetBranches { get; init; } = new();
}
