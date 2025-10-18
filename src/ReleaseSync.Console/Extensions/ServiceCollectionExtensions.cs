using Microsoft.Extensions.DependencyInjection;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.Extensions;

/// <summary>
/// IServiceCollection 擴充方法,用於註冊應用程式服務
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 註冊 ReleaseSync 應用程式服務
    /// </summary>
    /// <param name="services">服務集合</param>
    /// <returns>服務集合 (支援鏈式呼叫)</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICommandLineParserService, CommandLineParserService>();
        services.AddScoped<IDataFetchingService, DataFetchingService>();
        services.AddScoped<IApplicationRunner, ApplicationRunner>();

        return services;
    }
}
