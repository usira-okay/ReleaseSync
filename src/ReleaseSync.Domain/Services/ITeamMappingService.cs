namespace ReleaseSync.Domain.Services;

/// <summary>
/// 團隊對應服務介面
/// </summary>
/// <remarks>
/// 提供 Azure DevOps Work Item 的團隊名稱對應功能,用於過濾和顯示團隊資訊。
/// </remarks>
public interface ITeamMappingService
{
    /// <summary>
    /// 檢查指定的團隊名稱是否在對應清單中
    /// </summary>
    /// <param name="originalTeamName">原始團隊名稱 (來自 Azure DevOps Area Path)</param>
    /// <returns>
    /// 如果團隊在對應清單中返回 true;
    /// 如果團隊不在清單中返回 false;
    /// 如果對應清單為空 (未啟用過濾) 返回 true (向後相容)
    /// </returns>
    bool HasMapping(string? originalTeamName);

    /// <summary>
    /// 根據原始團隊名稱取得對應的顯示名稱
    /// </summary>
    /// <param name="originalTeamName">原始團隊名稱</param>
    /// <returns>
    /// 對應的顯示名稱;如果無對應則返回原始名稱;如果輸入為 null 則返回 null
    /// </returns>
    string? GetDisplayName(string? originalTeamName);

    /// <summary>
    /// 檢查是否啟用團隊過濾 (是否有配置 TeamMapping)
    /// </summary>
    /// <returns>如果 TeamMapping 非空返回 true</returns>
    bool IsFilteringEnabled();
}
