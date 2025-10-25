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

    /// <summary>
    /// 檢查指定平台的使用者是否在對應清單中
    /// </summary>
    /// <param name="platform">平台名稱 (GitLab 或 BitBucket)</param>
    /// <param name="username">使用者名稱</param>
    /// <returns>
    /// 如果使用者在對應清單中返回 true;
    /// 如果使用者不在清單中返回 false;
    /// 如果對應清單為空 (未啟用過濾) 返回 true (向後相容)
    /// </returns>
    bool HasMapping(string platform, string? username);

    /// <summary>
    /// 檢查是否啟用使用者過濾 (是否有配置 UserMapping)
    /// </summary>
    /// <returns>如果 UserMapping 非空返回 true</returns>
    bool IsFilteringEnabled();
}
