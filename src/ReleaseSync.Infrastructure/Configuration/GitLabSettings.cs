using ReleaseSync.Domain.Models;

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
    /// 目標分支（單一值）
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 抓取模式（覆寫全域設定）
    /// </summary>
    public FetchMode? FetchMode { get; init; }

    /// <summary>
    /// Release Branch（覆寫全域設定）
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 開始日期（覆寫全域設定）
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 結束日期（覆寫全域設定）
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 需要同步的目標分支清單（已棄用，請使用 TargetBranch）
    /// </summary>
    [Obsolete("請使用 TargetBranch（單一值）")]
    public List<string> TargetBranches { get; init; } = new();
}
