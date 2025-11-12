using ReleaseSync.Application.DTOs;

namespace ReleaseSync.Console.Services;

/// <summary>
/// Work Item 整合服務介面
/// </summary>
public interface IWorkItemEnricher
{
    /// <summary>
    /// 從 Work Item 服務抓取資訊並關聯到 PR/MR
    /// </summary>
    /// <param name="result">同步結果</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task EnrichAsync(SyncResultDto result, CancellationToken cancellationToken = default);
}
