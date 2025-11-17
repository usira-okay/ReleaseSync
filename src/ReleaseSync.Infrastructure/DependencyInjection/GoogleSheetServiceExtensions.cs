// <copyright file="GoogleSheetServiceExtensions.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReleaseSync.Application.Configuration;
using ReleaseSync.Application.Mappers;
using ReleaseSync.Application.Services;
using ReleaseSync.Infrastructure.GoogleSheet;

namespace ReleaseSync.Infrastructure.DependencyInjection;

/// <summary>
/// Google Sheet 服務的相依性注入擴展方法。
/// </summary>
public static class GoogleSheetServiceExtensions
{
    /// <summary>
    /// 新增 Google Sheet 相關服務至服務容器。
    /// </summary>
    /// <param name="services">服務容器。</param>
    /// <param name="configuration">組態設定。</param>
    /// <returns>服務容器 (支援鏈結呼叫)。</returns>
    public static IServiceCollection AddGoogleSheetServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 註冊組態
        services.Configure<GoogleSheetSettings>(configuration.GetSection(GoogleSheetSettings.SectionName));

        // 註冊 Infrastructure 層服務
        services.AddSingleton<IGoogleSheetRowParser, GoogleSheetRowParser>();
        services.AddScoped<IGoogleSheetApiClient, GoogleSheetApiClient>();

        // 註冊 Application 層服務
        // 注意: GoogleSheetDataMapper 需要 IWorkItemIdParser (Scoped)，因此改為 Scoped
        services.AddScoped<IGoogleSheetDataMapper, GoogleSheetDataMapper>();
        services.AddScoped<IGoogleSheetSyncService, GoogleSheetSyncService>();

        return services;
    }
}
