namespace ReleaseSync.Console.Handlers;

/// <summary>
/// Sync 命令選項參數
/// </summary>
public class SyncCommandOptions
{
    /// <summary>
    /// 起始日期
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// 結束日期
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// 是否啟用 GitLab
    /// </summary>
    public bool EnableGitLab { get; init; }

    /// <summary>
    /// 是否啟用 BitBucket
    /// </summary>
    public bool EnableBitBucket { get; init; }

    /// <summary>
    /// 是否啟用 Azure DevOps
    /// </summary>
    public bool EnableAzureDevOps { get; init; }

    /// <summary>
    /// 是否啟用匯出功能
    /// </summary>
    public bool EnableExport { get; init; }

    /// <summary>
    /// 輸出檔案路徑
    /// </summary>
    public string? OutputFile { get; init; }

    /// <summary>
    /// 是否強制覆寫現有檔案
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// 是否啟用詳細日誌輸出
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// 是否需要執行 PR/MR 資料抓取
    /// 條件: 任一平台啟用 (EnableGitLab 或 EnableBitBucket)
    /// </summary>
    public bool ShouldFetchPullRequests => EnableGitLab || EnableBitBucket;

    /// <summary>
    /// 是否需要執行 Azure DevOps Work Item 整合
    /// 條件: EnableAzureDevOps = true 且有啟用任一平台
    /// </summary>
    public bool ShouldEnrichWithWorkItems => EnableAzureDevOps && ShouldFetchPullRequests;

    /// <summary>
    /// 是否需要匯出 JSON 至檔案
    /// 條件: EnableExport = true 且 OutputFile 有值
    /// </summary>
    public bool ShouldExportToFile => EnableExport && !string.IsNullOrWhiteSpace(OutputFile);
}
