using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.DependencyInjection;
using FluentAssertions;

namespace ReleaseSync.Integration.Tests.EndToEnd;

/// <summary>
/// GitLab Release Branch 比對整合測試
/// </summary>
/// <remarks>
/// 此測試驗證 FetchMode=ReleaseBranch 模式的完整工作流程。
/// 需要有效的 GitLab API Token 才能執行實際 API 呼叫測試。
/// </remarks>
public class GitLabReleaseBranchIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isGitLabConfigured;

    public GitLabReleaseBranchIntegrationTests()
    {
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddUserMappingServices(configuration);
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        _serviceProvider = services.BuildServiceProvider();

        var gitLabSettings = _serviceProvider.GetRequiredService<IOptions<GitLabSettings>>().Value;
        _isGitLabConfigured = !string.IsNullOrEmpty(gitLabSettings.PersonalAccessToken) &&
                              !gitLabSettings.PersonalAccessToken.StartsWith("test-") &&
                              gitLabSettings.Projects?.Any() == true;
    }

    /// <summary>
    /// 測試 ReleaseBranch 模式的同步流程
    /// </summary>
    [Fact(Skip = "需要有效的 GitLab API Token 與 Release Branch，僅在本機手動測試時啟用")]
    public async Task Should_Sync_GitLab_MergeRequests_By_ReleaseBranch_Successfully()
    {
        // Arrange
        if (!_isGitLabConfigured)
        {
            Assert.Fail("GitLab 組態未設定或 Token 無效，無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var syncRequest = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120", // 需要實際存在的 Release Branch
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue("ReleaseBranch 模式應成功完成同步");
        result.PlatformStatuses.Should().HaveCount(1);

        var gitLabStatus = result.PlatformStatuses.First();
        gitLabStatus.PlatformName.Should().Be("GitLab");
        gitLabStatus.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// 測試當 ReleaseBranch 不存在時應回傳錯誤
    /// </summary>
    [Fact(Skip = "需要實際的 GitLab API 連接")]
    public async Task Should_Fail_When_ReleaseBranch_Not_Found()
    {
        // Arrange
        if (!_isGitLabConfigured)
        {
            Assert.Fail("GitLab 組態未設定或 Token 無效，無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var syncRequest = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/99991231", // 不存在的 Release Branch
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse("不存在的 ReleaseBranch 應導致同步失敗");

        var gitLabStatus = result.PlatformStatuses.First();
        gitLabStatus.IsSuccess.Should().BeFalse();
        gitLabStatus.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 測試 ReleaseBranch 模式要求必須指定 ReleaseBranch 參數
    /// </summary>
    [Fact]
    public async Task Should_Fail_When_ReleaseBranch_Mode_Without_Branch_Name()
    {
        // Arrange
        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var syncRequest = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = null, // 未指定 Release Branch
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act & Assert
        var act = async () => await orchestrator.SyncAsync(syncRequest);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ReleaseBranch*");
    }

    /// <summary>
    /// 測試 DateRange 模式仍可正常運作（向後相容性）
    /// </summary>
    [Fact(Skip = "需要有效的 GitLab API Token，僅在本機手動測試時啟用")]
    public async Task Should_Continue_Support_DateRange_Mode_For_Backward_Compatibility()
    {
        // Arrange
        if (!_isGitLabConfigured)
        {
            Assert.Fail("GitLab 組態未設定或 Token 無效，無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            FetchMode = FetchMode.DateRange, // 明確使用 DateRange 模式
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue("DateRange 模式應繼續正常運作");
    }

    /// <summary>
    /// 測試預設 FetchMode 為 DateRange（向後相容性）
    /// </summary>
    [Fact]
    public void SyncRequest_Default_FetchMode_Should_Be_DateRange()
    {
        // Arrange & Act
        var syncRequest = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Assert
        syncRequest.FetchMode.Should().Be(FetchMode.DateRange, "預設 FetchMode 應為 DateRange 以確保向後相容");
    }

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
