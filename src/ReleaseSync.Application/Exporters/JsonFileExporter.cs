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
        RepositoryBasedOutputDto data,
        string outputPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

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
            var json = JsonSerializer.Serialize(data, _jsonOptions);

            // 確保目錄存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 寫入檔案
            await File.WriteAllTextAsync(outputPath, json, cancellationToken);

            _logger.LogInformation("匯出完成: {OutputPath}, {Size} bytes",
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
