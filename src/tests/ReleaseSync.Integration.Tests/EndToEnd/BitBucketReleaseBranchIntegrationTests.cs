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
/// BitBucket Release Branch 比對整合測試
/// </summary>
/// <remarks>
/// 此測試驗證 FetchMode=ReleaseBranch 模式的完整工作流程。
/// 需要有效的 BitBucket API Token 才能執行實際 API 呼叫測試。
/// </remarks>
public class BitBucketReleaseBranchIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isBitBucketConfigured;

    public BitBucketReleaseBranchIntegrationTests()
    {
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSingleton<IConfiguration>(configuration);
        services.AddBitBucketServices(configuration);
        services.AddUserMappingServices(configuration);
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        _serviceProvider = services.BuildServiceProvider();

        var bitBucketSettings = _serviceProvider.GetRequiredService<IOptions<BitBucketSettings>>().Value;
        _isBitBucketConfigured = !string.IsNullOrEmpty(bitBucketSettings.Email) &&
                                  !string.IsNullOrEmpty(bitBucketSettings.AccessToken) &&
                                  !bitBucketSettings.AccessToken.StartsWith("test-") &&
                                  bitBucketSettings.Projects?.Any() == true;
    }

    /// <summary>
    /// 測試 ReleaseBranch 模式的同步流程
    /// </summary>
    [Fact(Skip = "需要有效的 BitBucket API Token 與 Release Branch，僅在本機手動測試時啟用")]
    public async Task Should_Sync_BitBucket_PullRequests_By_ReleaseBranch_Successfully()
    {
        // Arrange
        if (!_isBitBucketConfigured)
        {
            Assert.Fail("BitBucket 組態未設定或 Token 無效，無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var syncRequest = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120", // 需要實際存在的 Release Branch
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue("ReleaseBranch 模式應成功完成同步");
        result.PlatformStatuses.Should().HaveCount(1);

        var bitBucketStatus = result.PlatformStatuses.First();
        bitBucketStatus.PlatformName.Should().Be("BitBucket");
        bitBucketStatus.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// 測試當 ReleaseBranch 不存在時應回傳錯誤
    /// </summary>
    [Fact(Skip = "需要實際的 BitBucket API 連接")]
    public async Task Should_Fail_When_ReleaseBranch_Not_Found()
    {
        // Arrange
        if (!_isBitBucketConfigured)
        {
            Assert.Fail("BitBucket 組態未設定或 Token 無效，無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();

        var syncRequest = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/99991231", // 不存在的 Release Branch
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse("不存在的 ReleaseBranch 應導致同步失敗");

        var bitBucketStatus = result.PlatformStatuses.First();
        bitBucketStatus.IsSuccess.Should().BeFalse();
        bitBucketStatus.ErrorMessage.Should().NotBeNullOrEmpty();
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
            ReleaseBranch = "", // 空的 Release Branch
            EnableGitLab = false,
            EnableBitBucket = true,
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
    [Fact(Skip = "需要有效的 BitBucket API Token，僅在本機手動測試時啟用")]
    public async Task Should_Continue_Support_DateRange_Mode_For_Backward_Compatibility()
    {
        // Arrange
        if (!_isBitBucketConfigured)
        {
            Assert.Fail("BitBucket 組態未設定或 Token 無效，無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            FetchMode = FetchMode.DateRange, // 明確使用 DateRange 模式
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue("DateRange 模式應繼續正常運作");
    }

    /// <summary>
    /// 測試歷史版 Release Branch 比對
    /// </summary>
    [Fact(Skip = "需要有效的 BitBucket API Token 與多個 Release Branch，僅在本機手動測試時啟用")]
    public async Task Should_Compare_Historical_ReleaseBranch_With_Next_Version()
    {
        // Arrange
        if (!_isBitBucketConfigured)
        {
            Assert.Fail("BitBucket 組態未設定或 Token 無效，無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();

        // 使用較舊的 Release Branch，系統應自動找到下一版進行比對
        var syncRequest = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260113", // 較舊的版本
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue("歷史版 ReleaseBranch 比對應成功完成");
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
