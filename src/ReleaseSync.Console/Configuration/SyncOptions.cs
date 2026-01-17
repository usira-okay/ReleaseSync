using ReleaseSync.Console.Handlers;

namespace ReleaseSync.Console.Configuration;

/// <summary>
/// 同步命令執行選項組態
/// </summary>
public class SyncOptions
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
    /// 輸出檔案路徑 (當 EnableExport 為 true 時必須提供有效路徑)
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
    /// 是否啟用 Google Sheet 同步
    /// </summary>
    public bool EnableGoogleSheet { get; init; }

    /// <summary>
    /// Google Sheet ID (可選,用於覆蓋 appsettings.json 設定)
    /// </summary>
    public string? GoogleSheetId { get; init; }

    /// <summary>
    /// Google Sheet 工作表名稱 (可選,用於覆蓋 appsettings.json 設定)
    /// </summary>
    public string? GoogleSheetName { get; init; }

    /// <summary>
    /// 驗證組態有效性
    /// </summary>
    /// <exception cref="ArgumentException">當組態無效時拋出</exception>
    public void Validate()
    {
        // 驗證日期範圍
        if (StartDate > EndDate)
        {
            throw new ArgumentException($"起始日期 ({StartDate:yyyy-MM-dd}) 不能晚於結束日期 ({EndDate:yyyy-MM-dd})");
        }

        // 驗證至少啟用一個平台
        if (!EnableGitLab && !EnableBitBucket && !EnableAzureDevOps)
        {
            throw new ArgumentException("至少必須啟用一個平台 (GitLab, BitBucket 或 Azure DevOps)");
        }

        // 驗證匯出設定
        if (EnableExport && string.IsNullOrWhiteSpace(OutputFile))
        {
            throw new ArgumentException("輸出檔案路徑不能為空白");
        }
    }

    /// <summary>
    /// 轉換為 SyncCommandOptions
    /// </summary>
    /// <returns>對映後的 SyncCommandOptions 物件</returns>
    public SyncCommandOptions ToCommandOptions()
    {
        return new SyncCommandOptions
        {
            StartDate = StartDate,
            EndDate = EndDate,
            EnableGitLab = EnableGitLab,
            EnableBitBucket = EnableBitBucket,
            EnableAzureDevOps = EnableAzureDevOps,
            EnableExport = EnableExport,
            OutputFile = OutputFile,
            Force = Force,
            Verbose = Verbose,
            EnableGoogleSheet = EnableGoogleSheet,
            GoogleSheetId = GoogleSheetId,
            GoogleSheetName = GoogleSheetName
        };
    }
}
