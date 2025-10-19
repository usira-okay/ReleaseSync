using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReleaseSync.Application.Services;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Services;

namespace ReleaseSync.Infrastructure.DependencyInjection;

/// <summary>
/// UserMapping 服務的依賴注入擴展方法
/// </summary>
public static class UserMappingServiceExtensions
{
    /// <summary>
    /// 註冊 UserMapping 相關服務
    /// </summary>
    /// <param name="services">服務集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服務集合</returns>
    public static IServiceCollection AddUserMappingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 綁定設定
        services.Configure<UserMappingSettings>(
            configuration.GetSection("UserMapping"));

        // 註冊服務
        services.AddSingleton<IUserMappingService, UserMappingService>();

        return services;
    }
}
