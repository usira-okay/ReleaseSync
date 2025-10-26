namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// 團隊對應設定
/// </summary>
/// <remarks>
/// 用於定義 Azure DevOps Work Item 的團隊欄位與顯示名稱的對應關係
/// </remarks>
public class TeamMappingSettings
{
    /// <summary>
    /// 團隊對應清單
    /// </summary>
    public List<TeamMapping> Mappings { get; init; } = new();
}

/// <summary>
/// 團隊對應
/// </summary>
public class TeamMapping
{
    /// <summary>
    /// Azure DevOps Work Item 的原始團隊名稱 (來自 System.AreaPath)
    /// </summary>
    public required string OriginalTeamName { get; init; }

    /// <summary>
    /// 顯示名稱 (用於報表)
    /// </summary>
    public required string DisplayName { get; init; }
}
