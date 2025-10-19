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

    public UserMappingService(IOptions<UserMappingSettings> options)
    {
        _settings = options.Value;
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
}
