namespace ReleaseSync.Console.Configuration;

/// <summary>
/// Sync 命令執行選項設定
/// </summary>
public class SyncOptions
{
    /// <summary>
    /// 查詢起始日期
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// 查詢結束日期
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Release Branch 名稱 (選填,格式: release/yyyyMMdd)
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 啟用的平台設定
    /// </summary>
    public required EnabledPlatformsSettings EnabledPlatforms { get; init; }

    /// <summary>
    /// 匯出設定
    /// </summary>
    public required ExportSettings Export { get; init; }

    /// <summary>
    /// Google Sheet 同步設定
    /// </summary>
    public required GoogleSheetSyncSettings GoogleSheet { get; init; }

    /// <summary>
    /// 是否啟用詳細日誌輸出
    /// </summary>
    public bool Verbose { get; init; }
}

/// <summary>
/// 啟用的平台設定
/// </summary>
public class EnabledPlatformsSettings
{
    /// <summary>
    /// 是否啟用 GitLab
    /// </summary>
    public bool GitLab { get; init; }

    /// <summary>
    /// 是否啟用 BitBucket
    /// </summary>
    public bool BitBucket { get; init; }

    /// <summary>
    /// 是否啟用 Azure DevOps
    /// </summary>
    public bool AzureDevOps { get; init; }
}

/// <summary>
/// 匯出設定
/// </summary>
public class ExportSettings
{
    /// <summary>
    /// 是否啟用 JSON 匯出
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// 輸出檔案路徑
    /// </summary>
    public string OutputFile { get; init; } = "output.json";

    /// <summary>
    /// 是否強制覆寫現有檔案
    /// </summary>
    public bool Force { get; init; }
}

/// <summary>
/// Google Sheet 同步設定
/// </summary>
public class GoogleSheetSyncSettings
{
    /// <summary>
    /// 是否啟用 Google Sheet 同步
    /// </summary>
    public bool Enabled { get; init; }
}
