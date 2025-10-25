using Microsoft.Extensions.DependencyInjection;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Services;

namespace ReleaseSync.Infrastructure.DependencyInjection;

/// <summary>
/// TeamMapping 服務的依賴注入擴展方法
/// </summary>
public static class TeamMappingServiceExtensions
{
    /// <summary>
    /// 註冊 TeamMapping 相關服務
    /// </summary>
    /// <param name="services">服務集合</param>
    /// <returns>服務集合</returns>
    /// <remarks>
    /// TeamMapping 配置從 AzureDevOpsSettings.TeamMapping 載入,
    /// 因此需要先註冊 AzureDevOps 服務。
    /// </remarks>
    public static IServiceCollection AddTeamMappingServices(
        this IServiceCollection services)
    {
        // 註冊 TeamMappingService 為 Scoped (與 Repository 生命週期一致)
        services.AddScoped<ITeamMappingService, TeamMappingService>();

        return services;
    }
}
