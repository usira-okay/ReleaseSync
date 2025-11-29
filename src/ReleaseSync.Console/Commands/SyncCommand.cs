using System.CommandLine;

namespace ReleaseSync.Console.Commands;

/// <summary>
/// 同步 PR/MR 資訊的命令定義
/// </summary>
public static class SyncCommand
{
    /// <summary>
    /// 建立 sync 命令
    /// </summary>
    /// <returns>Sync 命令物件</returns>
    public static Command Create()
    {
        var command = new Command("sync", "從版控平台同步 PR/MR 資訊");

        // 必填參數
        var startDateOption = new Option<DateTime>(
            aliases: new[] { "--start-date", "-s" },
            description: "查詢起始日期 (包含)");
        startDateOption.IsRequired = true;

        var endDateOption = new Option<DateTime>(
            aliases: new[] { "--end-date", "-e" },
            description: "查詢結束日期 (包含)");
        endDateOption.IsRequired = true;

        // 平台啟用選項
        var enableGitLabOption = new Option<bool>(
            aliases: new[] { "--enable-gitlab", "--gitlab" },
            getDefaultValue: () => false,
            description: "啟用 GitLab 平台");

        var enableBitBucketOption = new Option<bool>(
            aliases: new[] { "--enable-bitbucket", "--bitbucket" },
            getDefaultValue: () => false,
            description: "啟用 BitBucket 平台");

        var enableAzureDevOpsOption = new Option<bool>(
            aliases: new[] { "--enable-azure-devops", "--azdo" },
            getDefaultValue: () => false,
            description: "啟用 Azure DevOps Work Item 整合");

        // 匯出選項
        var enableExportOption = new Option<bool>(
            aliases: new[] { "--enable-export", "--export" },
            getDefaultValue: () => false,
            description: "啟用 JSON 匯出功能");

        var outputFileOption = new Option<string?>(
            aliases: new[] { "--output-file", "-o" },
            description: "JSON 匯出檔案路徑");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            getDefaultValue: () => false,
            description: "強制覆蓋已存在的輸出檔案");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            getDefaultValue: () => false,
            description: "啟用詳細日誌輸出");

        var enableGoogleSheetOption = new Option<bool>(
            aliases: new[] { "--enable-google-sheet", "--google-sheet" },
            getDefaultValue: () => false,
            description: "啟用 Google Sheet 同步功能");

        var googleSheetIdOption = new Option<string?>(
            aliases: new[] { "--google-sheet-id", "--sheet-id" },
            description: "Google Sheet ID (覆蓋 appsettings.json 設定)");

        var googleSheetNameOption = new Option<string?>(
            aliases: new[] { "--google-sheet-name", "--sheet-name" },
            description: "Google Sheet 工作表名稱 (預設: Sheet1)");

        command.AddOption(startDateOption);
        command.AddOption(endDateOption);
        command.AddOption(enableGitLabOption);
        command.AddOption(enableBitBucketOption);
        command.AddOption(enableAzureDevOpsOption);
        command.AddOption(enableExportOption);
        command.AddOption(outputFileOption);
        command.AddOption(forceOption);
        command.AddOption(verboseOption);
        command.AddOption(enableGoogleSheetOption);
        command.AddOption(googleSheetIdOption);
        command.AddOption(googleSheetNameOption);

        return command;
    }
}
