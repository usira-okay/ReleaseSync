using ReleaseSync.Console.Configuration;
using ReleaseSync.Domain.Models;

namespace ReleaseSync.Console.Handlers;

/// <summary>
/// Sync 命令選項參數 (從 appsettings.json 讀取)
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
    /// 抓取模式 (DateRange 或 ReleaseBranch)
    /// </summary>
    public FetchMode FetchMode { get; init; } = FetchMode.DateRange;

    /// <summary>
    /// Release Branch 名稱 (當 FetchMode = ReleaseBranch 時使用)
    /// </summary>
    public string? ReleaseBranch { get; init; }

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
    /// 從 SyncOptions 設定建立 SyncCommandOptions
    /// </summary>
    /// <param name="syncOptions">同步選項設定</param>
    /// <param name="configuration">組態物件 (用於讀取 Google Sheet 覆蓋設定)</param>
    /// <returns>命令選項</returns>
    public static SyncCommandOptions FromConfiguration(SyncOptions syncOptions, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return new SyncCommandOptions
        {
            StartDate = syncOptions.StartDate,
            EndDate = syncOptions.EndDate,
            FetchMode = syncOptions.FetchMode,
            ReleaseBranch = syncOptions.ReleaseBranch,
            EnableGitLab = syncOptions.EnabledPlatforms.GitLab,
            EnableBitBucket = syncOptions.EnabledPlatforms.BitBucket,
            EnableAzureDevOps = syncOptions.EnabledPlatforms.AzureDevOps,
            EnableExport = syncOptions.Export.Enabled,
            OutputFile = syncOptions.Export.OutputFile,
            Force = syncOptions.Export.Force,
            Verbose = syncOptions.Verbose,
            EnableGoogleSheet = syncOptions.GoogleSheet.Enabled,
            // Google Sheet ID 與 Name 從 GoogleSheet 設定區塊讀取
            GoogleSheetId = configuration["GoogleSheet:SpreadsheetId"],
            GoogleSheetName = configuration["GoogleSheet:SheetName"]
        };
    }

    /// <summary>
    /// 是否為 Release Branch 抓取模式
    /// </summary>
    public bool IsReleaseBranchMode => FetchMode == FetchMode.ReleaseBranch;

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

    /// <summary>
    /// 是否需要同步至 Google Sheet
    /// 條件: EnableGoogleSheet = true 且 (有啟用任一平台或有 OutputFile)
    /// </summary>
    public bool ShouldSyncToGoogleSheet => EnableGoogleSheet && (ShouldFetchPullRequests || !string.IsNullOrWhiteSpace(OutputFile));
}
