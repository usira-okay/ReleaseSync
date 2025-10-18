using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;

namespace ReleaseSync.Application.Exporters;

/// <summary>
/// JSON 檔案匯出器
/// </summary>
public class JsonFileExporter : IResultExporter
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<JsonFileExporter> _logger;

    public JsonFileExporter(ILogger<JsonFileExporter> logger)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExportAsync(
        SyncResultDto syncResult,
        string outputPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("開始匯出 JSON 檔案: {OutputPath}, Overwrite={Overwrite}", outputPath, overwrite);

        // 檢查檔案是否存在
        if (File.Exists(outputPath) && !overwrite)
        {
            _logger.LogWarning("輸出檔案已存在: {OutputPath}", outputPath);
            throw new InvalidOperationException(
                $"輸出檔案已存在: {outputPath}。請使用 --force 參數強制覆蓋。");
        }

        try
        {
            // 序列化為 JSON
            _logger.LogDebug("序列化 SyncResult 為 JSON - PR/MR 數量: {Count}", syncResult.TotalPullRequestCount);
            var json = JsonSerializer.Serialize(syncResult, _jsonOptions);

            // 確保目錄存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                _logger.LogDebug("建立輸出目錄: {Directory}", directory);
                Directory.CreateDirectory(directory);
            }

            // 寫入檔案
            await File.WriteAllTextAsync(outputPath, json, cancellationToken);

            _logger.LogInformation("成功匯出 JSON 檔案: {OutputPath}, 檔案大小: {Size} bytes",
                outputPath, json.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "匯出 JSON 檔案失敗: {OutputPath}", outputPath);
            throw;
        }
    }

    public bool FileExists(string outputPath)
    {
        return File.Exists(outputPath);
    }
}
