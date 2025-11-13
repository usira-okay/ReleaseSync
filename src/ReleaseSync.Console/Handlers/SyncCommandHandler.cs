using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;
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
    private readonly ILogger<SyncCommandHandler> _logger;

    /// <summary>
    /// 建立 SyncCommandHandler
    /// </summary>
    public SyncCommandHandler(
        ISyncOrchestrator syncOrchestrator,
        IResultExporter resultExporter,
        IResultImporter resultImporter,
        IWorkItemEnricher workItemEnricher,
        ILogger<SyncCommandHandler> logger)
    {
        _syncOrchestrator = syncOrchestrator;
        _resultExporter = resultExporter;
        _resultImporter = resultImporter;
        _workItemEnricher = workItemEnricher;
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

            var workItemCentricData = options.ShouldFetchPullRequests
                ? await FetchAndProcessPullRequestsAsync(options, cancellationToken)
                : await LoadFromFileAsync(options, cancellationToken);

            if (workItemCentricData == null)
            {
                return 1;
            }

            await ExportResultIfNeededAsync(options, workItemCentricData, cancellationToken);

            return 0;
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
        _logger.LogInformation("時間範圍: {StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}, 平台: GitLab={GitLab}, BitBucket={BitBucket}, AzureDevOps={AzureDevOps}",
            options.StartDate, options.EndDate, options.EnableGitLab, options.EnableBitBucket, options.EnableAzureDevOps);
    }

    /// <summary>
    /// 抓取並處理 PR/MR 資料
    /// </summary>
    private async Task<WorkItemCentricOutputDto?> FetchAndProcessPullRequestsAsync(
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

        return WorkItemCentricOutputDto.FromSyncResult(result);
    }

    /// <summary>
    /// 從檔案載入資料
    /// </summary>
    private async Task<WorkItemCentricOutputDto?> LoadFromFileAsync(
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
        WorkItemCentricOutputDto workItemCentricData,
        CancellationToken cancellationToken)
    {
        if (!options.ShouldExportToFile)
        {
            return;
        }

        _logger.LogInformation("開始匯出 JSON 檔案...");
        await _resultExporter.ExportAsync(
            workItemCentricData,
            options.OutputFile!,
            overwrite: options.Force,
            cancellationToken);
        _logger.LogInformation("匯出完成: {OutputFile}", options.OutputFile);
    }
}
