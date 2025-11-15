using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;

namespace ReleaseSync.Application.Importers;

/// <summary>
/// JSON 檔案匯入器
/// </summary>
public class JsonFileImporter : IResultImporter
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<JsonFileImporter> _logger;

    public JsonFileImporter(ILogger<JsonFileImporter> logger)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RepositoryBasedOutputDto> ImportAsync(
        string inputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        // 檢查檔案是否存在
        if (!File.Exists(inputPath))
        {
            _logger.LogError("輸入檔案不存在: {InputPath}", inputPath);
            throw new FileNotFoundException($"輸入檔案不存在: {inputPath}", inputPath);
        }

        try
        {
            _logger.LogInformation("開始讀取 JSON 檔案: {InputPath}", inputPath);

            // 讀取檔案內容
            var json = await File.ReadAllTextAsync(inputPath, cancellationToken);

            // 反序列化為物件
            var data = JsonSerializer.Deserialize<RepositoryBasedOutputDto>(json, _jsonOptions);

            if (data == null)
            {
                _logger.LogError("無法解析 JSON 檔案: {InputPath}", inputPath);
                throw new InvalidOperationException($"無法解析 JSON 檔案: {inputPath}");
            }

            _logger.LogInformation("成功讀取 JSON 檔案: {InputPath}, Repositories 數量: {Count}",
                inputPath, data.Repositories.Count);

            return data;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON 格式錯誤: {InputPath}", inputPath);
            throw new InvalidOperationException($"JSON 格式錯誤: {inputPath}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取 JSON 檔案失敗: {InputPath}", inputPath);
            throw;
        }
    }
}
