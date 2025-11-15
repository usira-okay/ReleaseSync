using ReleaseSync.Application.DTOs;

namespace ReleaseSync.Application.Importers;

/// <summary>
/// 結果匯入器介面
/// </summary>
public interface IResultImporter
{
    /// <summary>
    /// 從檔案匯入 Repository 為中心的資料
    /// </summary>
    /// <param name="inputPath">輸入檔案路徑</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Repository 為中心的資料物件</returns>
    Task<RepositoryBasedOutputDto> ImportAsync(
        string inputPath,
        CancellationToken cancellationToken = default
    );
}
