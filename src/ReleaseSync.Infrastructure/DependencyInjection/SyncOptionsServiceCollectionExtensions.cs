using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.DependencyInjection;

/// <summary>
/// SyncOptions 服務註冊擴展方法
/// </summary>
public static class SyncOptionsServiceCollectionExtensions
{
    /// <summary>
    /// 註冊 SyncOptions 配置服務
    /// </summary>
    /// <param name="services">服務集合</param>
    /// <param name="configuration">組態物件</param>
    /// <returns>服務集合</returns>
    public static IServiceCollection AddSyncOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SyncOptionsSettings>(
            configuration.GetSection("SyncOptions"));

        return services;
    }
}
