using ReleaseSync.Application.DTOs;

namespace ReleaseSync.Application.Exporters;

/// <summary>
/// 結果匯出器介面
/// </summary>
public interface IResultExporter
{
    /// <summary>
    /// 將 Work Item 為中心的資料匯出至檔案
    /// </summary>
    /// <param name="data">要匯出的資料物件</param>
    /// <param name="outputPath">輸出檔案路徑</param>
    /// <param name="overwrite">是否覆寫現有檔案</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Task</returns>
    Task ExportAsync(
        WorkItemCentricOutputDto data,
        string outputPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 檢查檔案是否已存在
    /// </summary>
    /// <param name="outputPath">輸出檔案路徑</param>
    /// <returns>檔案是否存在</returns>
    bool FileExists(string outputPath);
}
