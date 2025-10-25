using Microsoft.Extensions.Options;
using ReleaseSync.Application.Services;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.Services;

/// <summary>
/// 使用者映射服務實作
/// </summary>
public class UserMappingService : IUserMappingService
{
    private readonly UserMappingSettings _settings;
    private readonly HashSet<string> _gitLabUsers;
    private readonly HashSet<string> _bitBucketUsers;

    public UserMappingService(IOptions<UserMappingSettings> options)
    {
        _settings = options.Value;

        // 使用 HashSet 快速查找 (O(1))
        _gitLabUsers = new HashSet<string>(
            _settings.Mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.GitLabUserId))
                .Select(m => m.GitLabUserId!),
            StringComparer.OrdinalIgnoreCase
        );

        _bitBucketUsers = new HashSet<string>(
            _settings.Mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.BitBucketUserId))
                .Select(m => m.BitBucketUserId!),
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <inheritdoc/>
    public string GetDisplayName(string platform, string username, string? defaultDisplayName = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return defaultDisplayName ?? "Unknown";
        }

        // 根據平台查找對應的映射
        var mapping = platform.ToLowerInvariant() switch
        {
            "gitlab" => _settings.Mappings.FirstOrDefault(m =>
                m.GitLabUserId != null &&
                m.GitLabUserId.Equals(username, StringComparison.OrdinalIgnoreCase)),

            "bitbucket" => _settings.Mappings.FirstOrDefault(m =>
                m.BitBucketUserId != null &&
                m.BitBucketUserId.Equals(username, StringComparison.OrdinalIgnoreCase)),

            _ => null
        };

        // 若找到映射則使用映射的 DisplayName，否則使用預設值
        return mapping?.DisplayName ?? defaultDisplayName ?? username;
    }

    /// <inheritdoc/>
    public bool HasMapping(string platform, string? username)
    {
        // 向後相容: 空 UserMapping 時返回 true (不過濾)
        if (_settings.Mappings.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        // 使用 HashSet 快速查找 (O(1))
        return platform.ToLowerInvariant() switch
        {
            "gitlab" => _gitLabUsers.Contains(username),
            "bitbucket" => _bitBucketUsers.Contains(username),
            _ => false
        };
    }

    /// <inheritdoc/>
    public bool IsFilteringEnabled()
    {
        return _settings.Mappings.Count > 0;
    }
}
