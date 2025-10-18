using ReleaseSync.Application.DTOs;

namespace ReleaseSync.Application.Services;

/// <summary>
/// 同步協調器介面
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// 執行多平台同步
    /// </summary>
    /// <param name="request">同步請求</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>同步結果</returns>
    Task<SyncResultDto> SyncAsync(SyncRequest request, CancellationToken cancellationToken = default);
}
