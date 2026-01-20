using ReleaseSync.Domain.Models;

namespace ReleaseSync.Application.DTOs;

/// <summary>
/// 同步請求 DTO
/// </summary>
public class SyncRequest
{
    /// <summary>
    /// 起始日期 (包含)
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// 結束日期 (包含)
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// 抓取模式
    /// </summary>
    public FetchMode FetchMode { get; init; } = FetchMode.DateRange;

    /// <summary>
    /// Release Branch 名稱（僅在 FetchMode = ReleaseBranch 時使用）
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 是否啟用 GitLab 平台同步
    /// </summary>
    public bool EnableGitLab { get; init; } = false;

    /// <summary>
    /// 是否啟用 BitBucket 平台同步
    /// </summary>
    public bool EnableBitBucket { get; init; } = false;

    /// <summary>
    /// 是否啟用 Azure DevOps Work Item 整合
    /// </summary>
    public bool EnableAzureDevOps { get; init; } = false;

    /// <summary>
    /// 特定的目標分支清單 (若為空則使用組態檔設定)
    /// </summary>
    public List<string> TargetBranches { get; init; } = new();
}
