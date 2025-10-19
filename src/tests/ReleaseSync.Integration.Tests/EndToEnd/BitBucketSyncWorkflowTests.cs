using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Services;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.DependencyInjection;
using FluentAssertions;

namespace ReleaseSync.Integration.Tests.EndToEnd;

/// <summary>
/// BitBucket 完整同步工作流程整合測試
/// </summary>
/// <remarks>
/// 此測試需要有效的 BitBucket API Token 才能執行
/// 可透過環境變數 BITBUCKET_TOKEN 或 appsettings.test.secure.json 提供
/// </remarks>
public class BitBucketSyncWorkflowTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isBitBucketConfigured;

    public BitBucketSyncWorkflowTests()
    {
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        // 設定 Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // 載入組態
        services.AddSingleton<IConfiguration>(configuration);

        // 註冊 BitBucket 服務
        services.AddBitBucketServices(configuration);

        // 註冊 Application 服務
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        _serviceProvider = services.BuildServiceProvider();

        // 檢查是否有有效的 BitBucket Token
        var bitBucketSettings = _serviceProvider.GetRequiredService<IOptions<BitBucketSettings>>().Value;
        _isBitBucketConfigured = !string.IsNullOrEmpty(bitBucketSettings.AccessToken) &&
                                 !bitBucketSettings.AccessToken.StartsWith("test-") &&
                                 bitBucketSettings.Projects?.Any() == true;
    }

    /// <summary>
    /// 測試完整的 BitBucket 同步流程 (需要有效的 API Token)
    /// </summary>
    [Fact(Skip = "需要有效的 BitBucket API Token,僅在本機手動測試時啟用")]
    public async Task Should_Sync_BitBucket_PullRequests_Successfully()
    {
        // Arrange
        if (!_isBitBucketConfigured)
        {
            Assert.Fail("BitBucket 組態未設定或 Token 無效,無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue("所有平台應成功完成同步");
        result.PlatformStatuses.Should().HaveCount(1);

        var bitBucketStatus = result.PlatformStatuses.First();
        bitBucketStatus.PlatformName.Should().Be("BitBucket");
        bitBucketStatus.IsSuccess.Should().BeTrue();
        bitBucketStatus.ErrorMessage.Should().BeNullOrEmpty();

        result.PullRequests.Should().NotBeNull();
        // 注意:實際數量取決於專案的 PR 數量,可能為 0
    }

    /// <summary>
    /// 測試 BitBucket 同步流程當 Token 無效時應回傳錯誤
    /// </summary>
    [Fact(Skip = "需要實際的 BitBucket API 連接")]
    public async Task Should_Fail_Gracefully_When_BitBucket_Token_Invalid()
    {
        // Arrange - 使用無效的 Token
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:Email"] = "test@example.com",
                ["BitBucket:AccessToken"] = "invalid-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "non-existent-workspace/non-existent-repo",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddBitBucketServices(configuration);
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse("無效的 Token 應導致同步失敗");
        result.PlatformStatuses.Should().HaveCount(1);

        var bitBucketStatus = result.PlatformStatuses.First();
        bitBucketStatus.PlatformName.Should().Be("BitBucket");
        bitBucketStatus.IsSuccess.Should().BeFalse();
        bitBucketStatus.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 測試 BitBucket 同步流程當 Repository 不存在時應回傳錯誤
    /// </summary>
    [Fact(Skip = "需要實際的 BitBucket API 連接")]
    public async Task Should_Fail_When_BitBucket_Repository_Not_Found()
    {
        // Arrange - 使用不存在的 Repository
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:Email"] = "test@example.com",
                ["BitBucket:AccessToken"] = "test-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "non-existent-workspace/non-existent-repo-12345",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddBitBucketServices(configuration);
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse();

        var bitBucketStatus = result.PlatformStatuses.FirstOrDefault(s => s.PlatformName == "BitBucket");
        bitBucketStatus.Should().NotBeNull();
        bitBucketStatus!.IsSuccess.Should().BeFalse();
        bitBucketStatus.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 測試 GitLab 與 BitBucket 同時啟用時應同時回傳結果
    /// </summary>
    [Fact]
    public async Task Should_Sync_Both_GitLab_And_BitBucket_When_Enabled()
    {
        // Arrange - 使用測試組態
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "test-gitlab-token",
                ["GitLab:Projects:0:ProjectPath"] = "test-group/test-project",
                ["GitLab:Projects:0:TargetBranches:0"] = "main",
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:Email"] = "test@example.com",
                ["BitBucket:AccessToken"] = "test-bitbucket-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "test-workspace/test-repo",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            EnableGitLab = true,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.PlatformStatuses.Should().HaveCount(2, "應包含 GitLab 與 BitBucket 兩個平台的狀態");

        var gitLabStatus = result.PlatformStatuses.FirstOrDefault(s => s.PlatformName == "GitLab");
        var bitBucketStatus = result.PlatformStatuses.FirstOrDefault(s => s.PlatformName == "BitBucket");

        gitLabStatus.Should().NotBeNull();
        bitBucketStatus.Should().NotBeNull();
    }

    /// <summary>
    /// 建立測試用的 Configuration
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var configPath = GetTestConfigurationPath();
        return new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile("appsettings.test.json", optional: false)
            .AddJsonFile("appsettings.test.secure.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// 取得測試組態檔路徑
    /// </summary>
    private static string GetTestConfigurationPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var testConfigPath = Path.Combine(currentDirectory, "TestData");

        if (!Directory.Exists(testConfigPath))
        {
            Directory.CreateDirectory(testConfigPath);
        }

        return testConfigPath;
    }
}
