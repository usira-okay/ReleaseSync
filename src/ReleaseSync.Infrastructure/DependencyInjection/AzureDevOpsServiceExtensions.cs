using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Platforms.AzureDevOps;
using ReleaseSync.Infrastructure.Parsers;

namespace ReleaseSync.Infrastructure.DependencyInjection;

/// <summary>
/// Azure DevOps 服務 DI 擴充方法
/// </summary>
public static class AzureDevOpsServiceExtensions
{
    /// <summary>
    /// 註冊 Azure DevOps 服務 (完整實作)
    /// </summary>
    public static IServiceCollection AddAzureDevOpsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 綁定設定
        services.Configure<AzureDevOpsSettings>(configuration.GetSection("AzureDevOps"));

        // 註冊 API Client
        services.AddScoped<AzureDevOpsApiClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AzureDevOpsSettings>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzureDevOpsApiClient>>();
            return new AzureDevOpsApiClient(settings, logger);
        });

        // 註冊 Repository
        services.AddScoped<IWorkItemRepository, AzureDevOpsWorkItemRepository>();

        // 註冊 Parser
        services.AddScoped<IWorkItemIdParser, RegexWorkItemIdParser>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AzureDevOpsSettings>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RegexWorkItemIdParser>>();
            return new RegexWorkItemIdParser(settings, logger);
        });

        // 註冊 Service
        services.AddScoped<AzureDevOpsService>();
        services.AddScoped<IWorkItemService>(sp => sp.GetRequiredService<AzureDevOpsService>());

        return services;
    }

    /// <summary>
    /// 註冊 Azure DevOps 服務 (使用 Stub,用於測試)
    /// </summary>
    public static IServiceCollection AddAzureDevOpsServicesStub(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 綁定設定
        services.Configure<AzureDevOpsSettings>(configuration.GetSection("AzureDevOps"));

        // 註冊 Stub Repository
        services.AddScoped<IWorkItemRepository, ReleaseSync.Infrastructure.Repositories.StubWorkItemRepository>();

        // 註冊 Stub Parser
        services.AddScoped<IWorkItemIdParser, StubWorkItemIdParser>();

        // 註冊 Service
        services.AddScoped<AzureDevOpsService>();

        return services;
    }
}
