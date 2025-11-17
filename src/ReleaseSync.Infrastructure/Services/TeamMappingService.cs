using Microsoft.Extensions.Options;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.Services;

/// <summary>
/// 團隊對應服務實作
/// </summary>
public class TeamMappingService : ITeamMappingService
{
    private readonly List<TeamMapping> _mappings;
    private readonly HashSet<string> _teamNames;
    private readonly Dictionary<string, string> _displayNames;
    private readonly Dictionary<string, int> _sortOrder;

    /// <summary>
    /// 建立 TeamMappingService
    /// </summary>
    public TeamMappingService(IOptions<AzureDevOpsSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        _mappings = options.Value.TeamMapping ?? new List<TeamMapping>();

        // 使用 HashSet 快速查找 (O(1))
        _teamNames = new HashSet<string>(
            _mappings.Select(m => m.OriginalTeamName),
            StringComparer.OrdinalIgnoreCase
        );

        // 建立顯示名稱查找字典 (O(1))
        _displayNames = _mappings.ToDictionary(
            m => m.OriginalTeamName,
            m => m.DisplayName,
            StringComparer.OrdinalIgnoreCase
        );

        // 建立團隊排序索引字典 (以 DisplayName 為 Key)
        _sortOrder = _mappings
            .Select(x => x.DisplayName)
            .Distinct()
            .Select((displayName, index) => new { DsiplayName = displayName, Index = index })
            .ToDictionary(
                x => x.DsiplayName,
                x => x.Index,
                StringComparer.OrdinalIgnoreCase
            );
    }

    /// <inheritdoc/>
    public bool HasMapping(string? originalTeamName)
    {
        // 向後相容: 空 TeamMapping 時返回 true (不過濾)
        if (_mappings.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(originalTeamName))
        {
            return false;
        }

        // 使用 HashSet 快速查找 (O(1))
        return _teamNames.Contains(originalTeamName);
    }

    /// <inheritdoc/>
    public string? GetDisplayName(string? originalTeamName)
    {
        // 保持原始值 (包括 null, empty, whitespace)
        if (string.IsNullOrWhiteSpace(originalTeamName))
        {
            return originalTeamName;
        }

        // 嘗試從字典查找顯示名稱
        if (_displayNames.TryGetValue(originalTeamName, out var displayName))
        {
            return displayName;
        }

        // 無對應時返回原始名稱
        return originalTeamName;
    }

    /// <inheritdoc/>
    public bool IsFilteringEnabled()
    {
        return _mappings.Count > 0;
    }

    /// <inheritdoc/>
    public int GetTeamSortOrder(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return int.MaxValue;
        }

        // 嘗試從字典查找排序索引
        if (_sortOrder.TryGetValue(displayName, out var order))
        {
            return order;
        }

        // 未找到對應的團隊，排在最後
        return int.MaxValue;
    }
}
