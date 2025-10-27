using System.Text.Encodings.Web;
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExportAsync(
        SyncResultDto syncResult,
        string outputPath,
        bool overwrite = false,
        bool useWorkItemCentricFormat = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("開始匯出 JSON 檔案: {OutputPath}, Overwrite={Overwrite}, UseWorkItemCentricFormat={UseWorkItemCentricFormat}",
            outputPath, overwrite, useWorkItemCentricFormat);

        // 檢查檔案是否存在
        if (File.Exists(outputPath) && !overwrite)
        {
            _logger.LogWarning("輸出檔案已存在: {OutputPath}", outputPath);
            throw new InvalidOperationException(
                $"輸出檔案已存在: {outputPath}。請使用 --force 參數強制覆蓋。");
        }

        try
        {
            // 根據格式選擇序列化的物件
            object dataToExport = useWorkItemCentricFormat
                ? WorkItemCentricOutputDto.FromSyncResult(syncResult)
                : syncResult;

            var formatType = useWorkItemCentricFormat ? "Work Item 為中心" : "PR 為中心(舊格式)";
            _logger.LogInformation("序列化 SyncResult 為 JSON - 格式: {Format}, PR/MR 數量: {Count}",
                formatType, syncResult.TotalPullRequestCount);

            // 序列化為 JSON
            var json = JsonSerializer.Serialize(dataToExport, _jsonOptions);

            // 確保目錄存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                _logger.LogInformation("建立輸出目錄: {Directory}", directory);
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
