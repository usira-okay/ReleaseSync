using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Exceptions;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Application.Importers;
using ReleaseSync.Application.Services;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.Handlers;

/// <summary>
/// Sync 命令處理器
/// </summary>
public class SyncCommandHandler
{
    private readonly ISyncOrchestrator _syncOrchestrator;
    private readonly IResultExporter _resultExporter;
    private readonly IResultImporter _resultImporter;
    private readonly IWorkItemEnricher _workItemEnricher;
    private readonly IGoogleSheetSyncService? _googleSheetSyncService;
    private readonly ILogger<SyncCommandHandler> _logger;

    /// <summary>
    /// 建立 SyncCommandHandler
    /// </summary>
    public SyncCommandHandler(
        ISyncOrchestrator syncOrchestrator,
        IResultExporter resultExporter,
        IResultImporter resultImporter,
        IWorkItemEnricher workItemEnricher,
        ILogger<SyncCommandHandler> logger,
        IGoogleSheetSyncService? googleSheetSyncService = null)
    {
        _syncOrchestrator = syncOrchestrator;
        _resultExporter = resultExporter;
        _resultImporter = resultImporter;
        _workItemEnricher = workItemEnricher;
        _googleSheetSyncService = googleSheetSyncService;
        _logger = logger;
    }

    /// <summary>
    /// 處理 sync 命令
    /// </summary>
    public async Task<int> HandleAsync(
        SyncCommandOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            LogStartup(options);

            var repositoryBasedData = options.ShouldFetchPullRequests
                ? await FetchAndProcessPullRequestsAsync(options, cancellationToken)
                : await LoadFromFileAsync(options, cancellationToken);

            if (repositoryBasedData == null)
            {
                return 1;
            }

            await ExportResultIfNeededAsync(options, repositoryBasedData, cancellationToken);
            await SyncToGoogleSheetIfNeededAsync(options, repositoryBasedData, cancellationToken);

            return 0;
        }
        catch (GoogleSheetException ex)
        {
            _logger.LogError(ex, "Google Sheet 同步失敗: {Message}", ex.Message);
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "認證失敗 - 請檢查 User Secrets 或 appsettings.json 中的 Token 設定");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "網路連線失敗 - 請檢查網路連線與 API URL 設定");
            return 1;
        }
        catch (FileNotFoundException ex) when (ex.Message.Contains("appsettings"))
        {
            _logger.LogError("找不到組態檔 - 請確認 appsettings.json 存在,並已設定 User Secrets");
            return 1;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("輸出檔案已存在"))
        {
            _logger.LogWarning("輸出檔案已存在: {OutputFile} - 請使用 --force 參數強制覆蓋", options.OutputFile);
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "執行失敗 - 使用 --verbose 參數可查看詳細錯誤資訊");
            return 1;
        }
    }

    /// <summary>
    /// 記錄啟動日誌
    /// </summary>
    private void LogStartup(SyncCommandOptions options)
    {
        _logger.LogInformation("=== ReleaseSync 同步工具 ===");
        _logger.LogInformation(
            "時間範圍: {StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}, 平台: GitLab={GitLab}, BitBucket={BitBucket}, AzureDevOps={AzureDevOps}, GoogleSheet={GoogleSheet}",
            options.StartDate,
            options.EndDate,
            options.EnableGitLab,
            options.EnableBitBucket,
            options.EnableAzureDevOps,
            options.EnableGoogleSheet);
    }

    /// <summary>
    /// 抓取並處理 PR/MR 資料
    /// </summary>
    private async Task<RepositoryBasedOutputDto?> FetchAndProcessPullRequestsAsync(
        SyncCommandOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("開始抓取 PR/MR 資料...");

        var request = new SyncRequest
        {
            StartDate = options.StartDate,
            EndDate = options.EndDate,
            EnableGitLab = options.EnableGitLab,
            EnableBitBucket = options.EnableBitBucket,
            EnableAzureDevOps = options.EnableAzureDevOps
        };

        var result = await _syncOrchestrator.SyncAsync(request, cancellationToken);

        _logger.LogInformation("同步完成 - 總計 PR/MR: {Count} 筆, 完全成功: {IsSuccess}",
            result.TotalPullRequestCount, result.IsFullySuccessful);

        if (options.ShouldEnrichWithWorkItems)
        {
            await EnrichWithWorkItemsAsync(result, cancellationToken);
        }

        return RepositoryBasedOutputDto.FromSyncResult(result);
    }

    /// <summary>
    /// 從檔案載入資料
    /// </summary>
    private async Task<RepositoryBasedOutputDto?> LoadFromFileAsync(
        SyncCommandOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.OutputFile))
        {
            _logger.LogError("未啟用任何平台且未指定輸入檔案 - 請啟用至少一個平台 (--gitlab, --bitbucket) 或指定輸入檔案 (-o)");
            return null;
        }

        _logger.LogInformation("未啟用任何平台,從檔案讀取資料: {InputFile}", options.OutputFile);
        return await _resultImporter.ImportAsync(options.OutputFile, cancellationToken);
    }

    /// <summary>
    /// 整合 Azure DevOps Work Items
    /// </summary>
    private async Task EnrichWithWorkItemsAsync(
        SyncResultDto syncResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("開始整合 Azure DevOps Work Items...");
        await _workItemEnricher.EnrichAsync(syncResult, cancellationToken);
    }

    /// <summary>
    /// 匯出結果 (如果需要)
    /// </summary>
    private async Task ExportResultIfNeededAsync(
        SyncCommandOptions options,
        RepositoryBasedOutputDto repositoryBasedData,
        CancellationToken cancellationToken)
    {
        if (!options.ShouldExportToFile)
        {
            return;
        }

        _logger.LogInformation("開始匯出 JSON 檔案...");
        await _resultExporter.ExportAsync(
            repositoryBasedData,
            options.OutputFile!,
            overwrite: options.Force,
            cancellationToken);
        _logger.LogInformation("匯出完成: {OutputFile}", options.OutputFile);
    }

    /// <summary>
    /// 同步至 Google Sheet (如果需要)
    /// </summary>
    private async Task SyncToGoogleSheetIfNeededAsync(
        SyncCommandOptions options,
        RepositoryBasedOutputDto repositoryBasedData,
        CancellationToken cancellationToken)
    {
        if (!options.ShouldSyncToGoogleSheet)
        {
            return;
        }

        if (_googleSheetSyncService == null)
        {
            _logger.LogWarning("Google Sheet 同步服務未註冊，跳過同步");
            return;
        }

        _logger.LogInformation("開始同步至 Google Sheet...");

        // 使用覆蓋的設定 (若有提供命令列參數)
        var result = await _googleSheetSyncService.SyncAsync(
            repositoryBasedData,
            spreadsheetIdOverride: options.GoogleSheetId,
            sheetNameOverride: options.GoogleSheetName,
            cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Google Sheet 同步完成 - 更新: {UpdatedCount} rows, 新增: {InsertedCount} rows, 處理 PR/MR: {ProcessedCount}, 執行時間: {Duration:F1} 秒",
                result.UpdatedRowCount,
                result.InsertedRowCount,
                result.ProcessedPullRequestCount,
                result.ExecutionDuration.TotalSeconds);

            if (!string.IsNullOrWhiteSpace(result.SpreadsheetUrl))
            {
                _logger.LogInformation("Google Sheet URL: {Url}", result.SpreadsheetUrl);
            }
        }
        else
        {
            _logger.LogWarning("Google Sheet 同步失敗: {ErrorMessage}", result.ErrorMessage);
        }
    }
}
