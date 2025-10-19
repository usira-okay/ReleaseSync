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
/// Work Item 解析與整合工作流程測試
/// </summary>
/// <remarks>
/// 此測試需要有效的 Azure DevOps PAT 才能執行
/// 可透過環境變數 AZURE_DEVOPS_PAT 或 appsettings.test.secure.json 提供
/// </remarks>
public class WorkItemIntegrationWorkflowTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isAzureDevOpsConfigured;

    public WorkItemIntegrationWorkflowTests()
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

        // 註冊所有平台服務
        services.AddGitLabServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddAzureDevOpsServices(configuration);

        // 註冊 Application 服務
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        _serviceProvider = services.BuildServiceProvider();

        // 檢查是否有有效的 Azure DevOps Token
        var azureDevOpsSettings = _serviceProvider.GetRequiredService<IOptions<AzureDevOpsSettings>>().Value;
        _isAzureDevOpsConfigured = !string.IsNullOrEmpty(azureDevOpsSettings.PersonalAccessToken) &&
                                   !azureDevOpsSettings.PersonalAccessToken.StartsWith("test-");
    }

    /// <summary>
    /// 測試完整的 Work Item 解析工作流程 (需要有效的 PAT)
    /// </summary>
    [Fact(Skip = "需要有效的 Azure DevOps PAT,僅在本機手動測試時啟用")]
    public async Task Should_Parse_WorkItems_From_Branch_Names_And_Enrich_PullRequests()
    {
        // Arrange
        if (!_isAzureDevOpsConfigured)
        {
            Assert.Fail("Azure DevOps 組態未設定或 Token 無效,無法執行此測試");
        }

        var orchestrator = _serviceProvider.GetRequiredService<ISyncOrchestrator>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);

        var syncRequest = new SyncRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            EnableGitLab = true,
            EnableBitBucket = true,
            EnableAzureDevOps = true  // 啟用 Azure DevOps Work Item 整合
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.PullRequests.Should().NotBeNull();

        // 驗證有成功解析 Work Item 的 PR (視實際資料而定,可能為 0)
        var pullRequestsWithWorkItems = result.PullRequests
            .Where(pr => pr.AssociatedWorkItem != null)
            .ToList();

        // 如果有成功解析的 Work Items,驗證其結構
        foreach (var pr in pullRequestsWithWorkItems)
        {
            pr.AssociatedWorkItem.Should().NotBeNull();
            pr.AssociatedWorkItem!.Id.Should().BeGreaterThan(0);
            pr.AssociatedWorkItem.Title.Should().NotBeNullOrEmpty();
            pr.AssociatedWorkItem.Type.Should().NotBeNullOrEmpty();
            pr.AssociatedWorkItem.State.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>
    /// 測試當 Azure DevOps 未啟用時不應解析 Work Items
    /// </summary>
    [Fact(Skip = "需要實際的 Azure DevOps API 連接")]
    public async Task Should_Not_Parse_WorkItems_When_AzureDevOps_Disabled()
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
            EnableAzureDevOps = false  // 停用 Azure DevOps
        };

        // Act
        var result = await orchestrator.SyncAsync(syncRequest);

        // Assert
        result.Should().NotBeNull();
        result.PullRequests.Should().NotBeNull();

        // 當 Azure DevOps 停用時,所有 PR 都不應有 Work Item 資訊
        result.PullRequests.Should().AllSatisfy(pr =>
            pr.AssociatedWorkItem.Should().BeNull("Azure DevOps 停用時不應解析 Work Items"));
    }

    /// <summary>
    /// 測試 Work Item 解析統計資訊
    /// </summary>
    [Fact(Skip = "需要實際的 Azure DevOps API 連接")]
    public async Task Should_Include_WorkItem_Statistics_In_SyncResult()
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
        result.LinkedWorkItemCount.Should().BeGreaterThanOrEqualTo(0, "應包含 Work Item 統計資訊");
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
