namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// GitLab 平台組態
/// </summary>
public class GitLabSettings
{
    /// <summary>
    /// Personal Access Token (從 User Secrets 或 appsettings.json 讀取)
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
    /// 目標分支 (單一分支)
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// PR 資料抓取模式 (預設: DateRange)
    /// </summary>
    public string FetchMode { get; init; } = "DateRange";

    /// <summary>
    /// Release Branch 名稱 (選填,覆寫全域設定)
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 查詢起始日期 (選填,覆寫全域設定)
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 查詢結束日期 (選填,覆寫全域設定)
    /// </summary>
    public DateTime? EndDate { get; init; }
}
