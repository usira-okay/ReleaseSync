using ReleaseSync.Application.DTOs;

namespace ReleaseSync.Application.Exporters;

/// <summary>
/// 結果匯出器介面
/// </summary>
public interface IResultExporter
{
    /// <summary>
    /// 將同步結果匯出至檔案
    /// </summary>
    /// <param name="result">同步結果</param>
    /// <param name="outputPath">輸出檔案路徑</param>
    /// <param name="overwrite">是否覆寫現有檔案</param>
    /// <param name="useWorkItemCentricFormat">是否使用 Work Item 為中心的格式 (預設 true)</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Task</returns>
    Task ExportAsync(
        SyncResultDto result,
        string outputPath,
        bool overwrite = false,
        bool useWorkItemCentricFormat = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 檢查檔案是否已存在
    /// </summary>
    /// <param name="outputPath">輸出檔案路徑</param>
    /// <returns>檔案是否存在</returns>
    bool FileExists(string outputPath);
}
