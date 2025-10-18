using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Platforms.BitBucket;

namespace ReleaseSync.Infrastructure.DependencyInjection;

/// <summary>
/// BitBucket 服務 DI 擴充方法
/// </summary>
public static class BitBucketServiceExtensions
{
    /// <summary>
    /// 註冊 BitBucket 服務
    /// </summary>
    public static IServiceCollection AddBitBucketServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 綁定設定
        services.Configure<BitBucketSettings>(configuration.GetSection("BitBucket"));

        // 註冊 HttpClient for BitBucket
        services.AddHttpClient("BitBucket");

        // 註冊 BitBucketApiClient
        services.AddScoped<BitBucketApiClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<BitBucketSettings>>().Value;
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("BitBucket");
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BitBucketApiClient>>();

            // 注意: BitBucket 需要 username,這裡假設從環境變數或另一個設定取得
            // 或者可以在 BitBucketSettings 加入 Username 欄位
            return new BitBucketApiClient(httpClient, settings.AppPassword, null, logger);
        });

        // 註冊 Repository
        services.AddScoped<IPullRequestRepository, BitBucketPullRequestRepository>();

        // 註冊 Service 並註冊到 IPlatformService
        services.AddScoped<BitBucketService>();
        services.AddScoped<IPlatformService, BitBucketService>(sp => sp.GetRequiredService<BitBucketService>());

        return services;
    }
}
