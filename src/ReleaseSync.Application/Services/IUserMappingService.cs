namespace ReleaseSync.Application.Services;

/// <summary>
/// 使用者映射服務介面
/// </summary>
public interface IUserMappingService
{
    /// <summary>
    /// 根據平台和使用者名稱取得對應的顯示名稱
    /// </summary>
    /// <param name="platform">平台名稱 (GitLab 或 BitBucket)</param>
    /// <param name="username">使用者名稱</param>
    /// <param name="defaultDisplayName">預設顯示名稱（當無映射時使用）</param>
    /// <returns>映射後的顯示名稱，若無映射則返回預設值</returns>
    string GetDisplayName(string platform, string username, string? defaultDisplayName = null);
}
