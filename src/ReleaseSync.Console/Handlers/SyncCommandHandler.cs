using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Application.Services;

namespace ReleaseSync.Console.Handlers;

/// <summary>
/// Sync 命令處理器
/// </summary>
public class SyncCommandHandler
{
    private readonly ISyncOrchestrator _syncOrchestrator;
    private readonly IResultExporter _resultExporter;
    private readonly ILogger<SyncCommandHandler> _logger;

    /// <summary>
    /// 建立 SyncCommandHandler
    /// </summary>
    public SyncCommandHandler(
        ISyncOrchestrator syncOrchestrator,
        IResultExporter resultExporter,
        ILogger<SyncCommandHandler> logger)
    {
        _syncOrchestrator = syncOrchestrator ?? throw new ArgumentNullException(nameof(syncOrchestrator));
        _resultExporter = resultExporter ?? throw new ArgumentNullException(nameof(resultExporter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 處理 sync 命令
    /// </summary>
    public async Task<int> HandleAsync(
        DateTime startDate,
        DateTime endDate,
        bool enableGitLab,
        bool enableBitBucket,
        bool enableAzureDevOps,
        string? outputFile,
        bool force,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== ReleaseSync 同步工具 ===");
            _logger.LogInformation("時間範圍: {StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}, 平台: GitLab={GitLab}, BitBucket={BitBucket}, AzureDevOps={AzureDevOps}",
                startDate, endDate, enableGitLab, enableBitBucket, enableAzureDevOps);

            // 建立請求
            var request = new SyncRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                EnableGitLab = enableGitLab,
                EnableBitBucket = enableBitBucket,
                EnableAzureDevOps = enableAzureDevOps
            };

            // 執行同步
            var result = await _syncOrchestrator.SyncAsync(request, cancellationToken);

            _logger.LogInformation("同步完成 - 總計 PR/MR: {Count} 筆, 完全成功: {IsSuccess}", 
                result.TotalPullRequestCount, result.IsFullySuccessful);

            // 匯出 JSON
            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                await _resultExporter.ExportAsync(
                    result,
                    outputFile,
                    overwrite: force,
                    useWorkItemCentricFormat: true,
                    cancellationToken);
                _logger.LogInformation("匯出完成: {OutputFile}", outputFile);
            }

            return 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "認證失敗 - 請檢查 appsettings.secure.json 中的 Token 設定");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "網路連線失敗 - 請檢查網路連線與 API URL 設定");
            return 1;
        }
        catch (FileNotFoundException ex) when (ex.Message.Contains("appsettings"))
        {
            _logger.LogError("找不到組態檔 - 請確認 appsettings.json 與 appsettings.secure.json 存在");
            return 1;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("輸出檔案已存在"))
        {
            _logger.LogWarning("輸出檔案已存在: {OutputFile} - 請使用 --force 參數強制覆蓋", outputFile);
            return 1;
        }
        catch (ArgumentException ex) when (ex.Message.Contains("至少須啟用一個平台"))
        {
            _logger.LogError("未啟用任何平台 - 請使用 --gitlab 或 --bitbucket 參數");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "執行失敗 - 使用 --verbose 參數可查看詳細錯誤資訊");
            return 1;
        }
    }
}
