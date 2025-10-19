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
/// GitLab 完整同步工作流程整合測試
/// </summary>
/// <remarks>
/// 此測試需要有效的 GitLab API Token 才能執行
/// 可透過環境變數 GITLAB_PAT 或 appsettings.test.secure.json 提供
/// </remarks>
public class GitLabSyncWorkflowTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isGitLabConfigured;

    public GitLabSyncWorkflowTests()
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

        // 註冊 GitLab 服務
        services.AddGitLabServices(configuration);
        services.AddUserMappingServices(configuration);

        // 註冊 Application 服務
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        _serviceProvider = services.BuildServiceProvider();

        // 檢查是否有有效的 GitLab Token
        var gitLabSettings = _serviceProvider.GetRequiredService<IOptions<GitLabSettings>>().Value;
        _isGitLabConfigured = !string.IsNullOrEmpty(gitLabSettings.PersonalAccessToken) &&
                              !gitLabSettings.PersonalAccessToken.StartsWith("test-") &&
                              gitLabSettings.Projects?.Any() == true;
    }

    /// <summary>
    /// 測試完整的 GitLab 同步流程 (需要有效的 API Token)
    /// </summary>
    [Fact(Skip = "需要有效的 GitLab API Token,僅在本機手動測試時啟用")]
    public async Task Should_Sync_GitLab_MergeRequests_Successfully()
    {
        // Arrange
        if (!_isGitLabConfigured)
        {
            Assert.Fail("GitLab 組態未設定或 Token 無效,無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue("所有平台應成功完成同步");
        result.PlatformStatuses.Should().HaveCount(1);

        var gitLabStatus = result.PlatformStatuses.First();
        gitLabStatus.PlatformName.Should().Be("GitLab");
        gitLabStatus.IsSuccess.Should().BeTrue();
        gitLabStatus.ErrorMessage.Should().BeNullOrEmpty();

        result.PullRequests.Should().NotBeNull();
        // 注意:實際數量取決於專案的 MR 數量,可能為 0
    }

    /// <summary>
    /// 測試 GitLab 同步流程當 Token 無效時應回傳錯誤
    /// </summary>
    [Fact(Skip = "需要實際的 GitLab API 連接")]
    public async Task Should_Fail_Gracefully_When_GitLab_Token_Invalid()
    {
        // Arrange - 使用無效的 Token
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "invalid-token",
                ["GitLab:Projects:0:ProjectPath"] = "non-existent/project",
                ["GitLab:Projects:0:TargetBranches:0"] = "main"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddUserMappingServices(configuration);
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
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse("無效的 Token 應導致同步失敗");
        result.PlatformStatuses.Should().HaveCount(1);

        var gitLabStatus = result.PlatformStatuses.First();
        gitLabStatus.PlatformName.Should().Be("GitLab");
        gitLabStatus.IsSuccess.Should().BeFalse();
        gitLabStatus.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 測試 GitLab 同步流程當專案不存在時應回傳錯誤
    /// </summary>
    [Fact(Skip = "需要實際的 GitLab API 連接")]
    public async Task Should_Fail_When_GitLab_Project_Not_Found()
    {
        // Arrange - 使用不存在的專案
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "glpat-test",
                ["GitLab:Projects:0:ProjectPath"] = "non-existent-group/non-existent-project-12345",
                ["GitLab:Projects:0:TargetBranches:0"] = "main"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddUserMappingServices(configuration);
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
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse();

        var gitLabStatus = result.PlatformStatuses.FirstOrDefault(s => s.PlatformName == "GitLab");
        gitLabStatus.Should().NotBeNull();
        gitLabStatus!.IsSuccess.Should().BeFalse();
        gitLabStatus.ErrorMessage.Should().NotBeNullOrEmpty();
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
