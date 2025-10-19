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
            if (verbose)
            {
                System.Console.WriteLine("🔍 啟用詳細日誌輸出 (Debug 等級)");
                System.Console.WriteLine();
            }

            _logger.LogInformation("=== ReleaseSync 同步工具 ===");
            _logger.LogInformation("時間範圍: {StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}",
                startDate, endDate);
            _logger.LogInformation("平台: GitLab={GitLab}, BitBucket={BitBucket}, AzureDevOps={AzureDevOps}",
                enableGitLab, enableBitBucket, enableAzureDevOps);

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
            _logger.LogInformation("開始執行同步作業...");
            var result = await _syncOrchestrator.SyncAsync(request, cancellationToken);

            _logger.LogInformation("同步完成!");
            _logger.LogInformation("  - 總計 PR/MR: {Count} 筆", result.TotalPullRequestCount);
            _logger.LogInformation("  - 完全成功: {IsSuccess}", result.IsFullySuccessful);

            // 匯出 JSON
            if (!string.IsNullOrWhiteSpace(outputFile))
            {
                _logger.LogInformation("匯出結果至: {OutputFile}", outputFile);
                await _resultExporter.ExportAsync(result, outputFile, force, cancellationToken);
                _logger.LogInformation("匯出完成!");
            }

            return 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "認證失敗");
            System.Console.WriteLine();
            System.Console.WriteLine("❌ 認證失敗!");
            System.Console.WriteLine("請檢查以下項目:");
            System.Console.WriteLine("  1. 確認 appsettings.secure.json 中的 Token 正確");
            System.Console.WriteLine("  2. 確認 Token 未過期");
            System.Console.WriteLine("  3. 確認 Token 權限足夠 (GitLab: api, read_repository)");
            System.Console.WriteLine();
            System.Console.WriteLine($"錯誤訊息: {ex.Message}");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "網路連線失敗");
            System.Console.WriteLine();
            System.Console.WriteLine("❌ 網路連線失敗!");
            System.Console.WriteLine("請檢查:");
            System.Console.WriteLine("  1. 網路連線是否正常");
            System.Console.WriteLine("  2. API URL 是否正確 (appsettings.json)");
            System.Console.WriteLine($"  3. 錯誤訊息: {ex.Message}");
            return 1;
        }
        catch (FileNotFoundException ex) when (ex.Message.Contains("appsettings"))
        {
            System.Console.WriteLine("❌ 找不到組態檔!");
            System.Console.WriteLine("請確認以下檔案存在:");
            System.Console.WriteLine("  - appsettings.json");
            System.Console.WriteLine("  - appsettings.secure.json (可從 appsettings.secure.example.json 複製)");
            return 1;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("輸出檔案已存在"))
        {
            _logger.LogWarning("輸出檔案已存在");
            System.Console.WriteLine();
            System.Console.WriteLine("⚠️ 輸出檔案已存在!");
            System.Console.WriteLine($"檔案: {outputFile}");
            System.Console.WriteLine("請使用 --force 或 -f 參數強制覆蓋,或指定不同的輸出檔案。");
            return 1;
        }
        catch (ArgumentException ex) when (ex.Message.Contains("至少須啟用一個平台"))
        {
            _logger.LogError("未啟用任何平台");
            System.Console.WriteLine();
            System.Console.WriteLine("❌ 未啟用任何平台!");
            System.Console.WriteLine("請至少啟用一個平台:");
            System.Console.WriteLine("  --enable-gitlab   或  --gitlab");
            System.Console.WriteLine("  --enable-bitbucket 或 --bitbucket");
            System.Console.WriteLine();
            System.Console.WriteLine("範例: dotnet run -- sync -s 2025-01-01 -e 2025-01-31 --gitlab");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "執行失敗: {Message}", ex.Message);
            System.Console.WriteLine();
            System.Console.WriteLine("❌ 執行失敗!");
            System.Console.WriteLine($"錯誤訊息: {ex.Message}");
            System.Console.WriteLine();
            System.Console.WriteLine("使用 --verbose 或 -v 參數可查看詳細錯誤資訊");
            return 1;
        }
    }
}
