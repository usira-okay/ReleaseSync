using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Platforms.GitLab;

namespace ReleaseSync.Infrastructure.DependencyInjection;

/// <summary>
/// GitLab 服務 DI 擴充方法
/// </summary>
public static class GitLabServiceExtensions
{
    /// <summary>
    /// 註冊 GitLab 服務
    /// </summary>
    public static IServiceCollection AddGitLabServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 綁定設定
        services.Configure<GitLabSettings>(configuration.GetSection("GitLab"));

        // 註冊 GitLabApiClient
        services.AddScoped<GitLabApiClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<GitLabSettings>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GitLabApiClient>>();
            return new GitLabApiClient(settings.ApiUrl, settings.PersonalAccessToken, logger);
        });

        // 註冊 Repository
        services.AddScoped<IPullRequestRepository, GitLabPullRequestRepository>();

        // 註冊 Service 並註冊到 IPlatformService
        services.AddScoped<GitLabService>();
        services.AddScoped<IPlatformService, GitLabService>(sp => sp.GetRequiredService<GitLabService>());

        return services;
    }
}
