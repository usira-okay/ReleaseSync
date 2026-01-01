namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// Sync 命令執行參數設定
/// </summary>
public class SyncOptionsSettings
{
    /// <summary>
    /// 查詢起始日期 (包含)
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 查詢結束日期 (包含)
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 是否啟用 GitLab 平台
    /// </summary>
    public bool EnableGitLab { get; set; }

    /// <summary>
    /// 是否啟用 BitBucket 平台
    /// </summary>
    public bool EnableBitBucket { get; set; }

    /// <summary>
    /// 是否啟用 Azure DevOps Work Item 整合
    /// </summary>
    public bool EnableAzureDevOps { get; set; }

    /// <summary>
    /// 是否啟用 JSON 匯出功能
    /// </summary>
    public bool EnableExport { get; set; }

    /// <summary>
    /// JSON 匯出檔案路徑
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// 是否強制覆蓋已存在的輸出檔案
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// 是否啟用詳細日誌輸出
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// 是否啟用 Google Sheet 同步功能
    /// </summary>
    public bool EnableGoogleSheet { get; set; }

    /// <summary>
    /// Google Sheet ID (覆蓋 GoogleSheet:SpreadsheetId 設定)
    /// </summary>
    public string? GoogleSheetId { get; set; }

    /// <summary>
    /// Google Sheet 工作表名稱 (預設: Sheet1)
    /// </summary>
    public string? GoogleSheetName { get; set; }
}
