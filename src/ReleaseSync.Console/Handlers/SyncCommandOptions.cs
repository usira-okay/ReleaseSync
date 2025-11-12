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
}
