using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Services;
using ReleaseSync.Infrastructure.DependencyInjection;
using FluentAssertions;

namespace ReleaseSync.Integration.Tests.EndToEnd;

/// <summary>
/// Work Item 解析失敗與錯誤處理測試
/// </summary>
public class WorkItemParsingFailureTests
{
    /// <summary>
    /// 測試當 Branch 名稱無法解析 Work Item ID 時應優雅處理
    /// </summary>
    [Fact]
    public async Task Should_Handle_Gracefully_When_Branch_Name_Cannot_Be_Parsed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "test-token",
                ["GitLab:Projects:0:ProjectPath"] = "test/project",
                ["GitLab:Projects:0:TargetBranches:0"] = "main",
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:AppPassword"] = "test-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "test/repo",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main",
                ["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/test",
                ["AzureDevOps:ProjectName"] = "TestProject",
                ["AzureDevOps:PersonalAccessToken"] = "test-token",
                // 設定一個無法匹配 main/develop 等常見分支的 Pattern
                ["AzureDevOps:WorkItemIdPatterns:0:Name"] = "Strict Pattern",
                ["AzureDevOps:WorkItemIdPatterns:0:Regex"] = @"feature/(\d+)-",
                ["AzureDevOps:WorkItemIdPatterns:0:CaptureGroup"] = "1",
                ["AzureDevOps:ParsingBehavior:OnParseFailure"] = "LogWarningAndContinue"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddAzureDevOpsServices(configuration);
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
            EnableAzureDevOps = true
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        // 即使無法解析 Work Item ID,同步流程仍應成功完成
        result.PullRequests.Should().NotBeNull();

        // 無法解析的 Branch 名稱對應的 PR 應該沒有 Work Item 資訊
        var pullRequestsFromMainBranch = result.PullRequests
            .Where(pr => pr.SourceBranch.Contains("main") || pr.SourceBranch.Contains("develop"))
            .ToList();

        pullRequestsFromMainBranch.Should().AllSatisfy(pr =>
            pr.AssociatedWorkItem.Should().BeNull("無法解析 Work Item ID 的分支不應有 Work Item 資訊"));
    }

    /// <summary>
    /// 測試當 Work Item API 無法存取時應優雅處理
    /// </summary>
    [Fact]
    public async Task Should_Handle_Gracefully_When_WorkItem_API_Unavailable()
    {
        // Arrange - 使用無效的 Organization URL
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "test-token",
                ["GitLab:Projects:0:ProjectPath"] = "test/project",
                ["GitLab:Projects:0:TargetBranches:0"] = "main",
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:AppPassword"] = "test-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "test/repo",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main",
                ["AzureDevOps:OrganizationUrl"] = "https://invalid-org-url.com",
                ["AzureDevOps:ProjectName"] = "TestProject",
                ["AzureDevOps:PersonalAccessToken"] = "test-token",
                ["AzureDevOps:WorkItemIdPatterns:0:Name"] = "Standard",
                ["AzureDevOps:WorkItemIdPatterns:0:Regex"] = @"feature/(\d+)-",
                ["AzureDevOps:WorkItemIdPatterns:0:CaptureGroup"] = "1"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddAzureDevOpsServices(configuration);
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
            EnableAzureDevOps = true
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        // 即使 Work Item API 無法存取,主要同步流程仍應成功
        result.PullRequests.Should().NotBeNull();
    }

    /// <summary>
    /// 測試當 Work Item 不存在時應優雅處理
    /// </summary>
    [Fact]
    public async Task Should_Handle_Gracefully_When_WorkItem_Not_Found()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "test-token",
                ["GitLab:Projects:0:ProjectPath"] = "test/project",
                ["GitLab:Projects:0:TargetBranches:0"] = "main",
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:AppPassword"] = "test-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "test/repo",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main",
                ["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/test",
                ["AzureDevOps:ProjectName"] = "TestProject",
                ["AzureDevOps:PersonalAccessToken"] = "test-token",
                ["AzureDevOps:WorkItemIdPatterns:0:Name"] = "Standard",
                ["AzureDevOps:WorkItemIdPatterns:0:Regex"] = @"feature/(\d+)-",
                ["AzureDevOps:WorkItemIdPatterns:0:CaptureGroup"] = "1"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddAzureDevOpsServices(configuration);
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
            EnableAzureDevOps = true
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        // 即使某些 Work Item 不存在,同步流程仍應成功完成
        result.PullRequests.Should().NotBeNull();
    }

    /// <summary>
    /// 測試當無 Work Item Pattern 設定時應優雅處理
    /// </summary>
    [Fact]
    public async Task Should_Handle_Gracefully_When_No_WorkItem_Patterns_Configured()
    {
        // Arrange - 沒有設定任何 Work Item Pattern
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "test-token",
                ["GitLab:Projects:0:ProjectPath"] = "test/project",
                ["GitLab:Projects:0:TargetBranches:0"] = "main",
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:AppPassword"] = "test-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "test/repo",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main",
                ["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/test",
                ["AzureDevOps:ProjectName"] = "TestProject",
                ["AzureDevOps:PersonalAccessToken"] = "test-token"
                // 沒有設定 WorkItemIdPatterns
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddAzureDevOpsServices(configuration);
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
            EnableAzureDevOps = true
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.PullRequests.Should().NotBeNull();

        // 沒有 Pattern 設定時,所有 PR 都不應有 Work Item 資訊
        result.PullRequests.Should().AllSatisfy(pr =>
            pr.AssociatedWorkItem.Should().BeNull("無 Pattern 設定時無法解析 Work Items"));

        result.LinkedWorkItemCount.Should().Be(0, "無 Pattern 設定時 Work Item 數量應為 0");
    }

    /// <summary>
    /// 測試無效的 Regex Pattern 應優雅處理
    /// </summary>
    [Fact]
    public async Task Should_Handle_Gracefully_When_Regex_Pattern_Is_Invalid()
    {
        // Arrange - 設定無效的 Regex Pattern
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["GitLab:ApiUrl"] = "https://gitlab.com/api/v4",
                ["GitLab:PersonalAccessToken"] = "test-token",
                ["GitLab:Projects:0:ProjectPath"] = "test/project",
                ["GitLab:Projects:0:TargetBranches:0"] = "main",
                ["BitBucket:ApiUrl"] = "https://api.bitbucket.org/2.0",
                ["BitBucket:AppPassword"] = "test-token",
                ["BitBucket:Projects:0:WorkspaceAndRepo"] = "test/repo",
                ["BitBucket:Projects:0:TargetBranches:0"] = "main",
                ["AzureDevOps:OrganizationUrl"] = "https://dev.azure.com/test",
                ["AzureDevOps:ProjectName"] = "TestProject",
                ["AzureDevOps:PersonalAccessToken"] = "test-token",
                ["AzureDevOps:WorkItemIdPatterns:0:Name"] = "Invalid Pattern",
                ["AzureDevOps:WorkItemIdPatterns:0:Regex"] = "[invalid(regex",  // 無效的 Regex
                ["AzureDevOps:WorkItemIdPatterns:0:CaptureGroup"] = "1"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddGitLabServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddAzureDevOpsServices(configuration);
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
            EnableAzureDevOps = true
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        // 即使 Regex 無效,同步流程仍應成功完成
        result.PullRequests.Should().NotBeNull();
    }
}
