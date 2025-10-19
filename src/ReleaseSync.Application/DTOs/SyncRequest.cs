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
    /// 是否啟用 GitLab 平台同步
    /// </summary>
    public bool EnableGitLab { get; init; } = true;

    /// <summary>
    /// 是否啟用 BitBucket 平台同步
    /// </summary>
    public bool EnableBitBucket { get; init; } = true;

    /// <summary>
    /// 是否啟用 Azure DevOps Work Item 整合
    /// </summary>
    public bool EnableAzureDevOps { get; init; } = false;

    /// <summary>
    /// 是否包含未合併的 PR/MR
    /// </summary>
    public bool IncludeUnmerged { get; init; } = true;

    /// <summary>
    /// 特定的目標分支清單 (若為空則使用組態檔設定)
    /// </summary>
    public List<string> TargetBranches { get; init; } = new();
}
