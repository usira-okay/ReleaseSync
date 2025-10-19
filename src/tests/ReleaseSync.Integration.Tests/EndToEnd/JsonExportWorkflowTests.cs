using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Application.Services;
using ReleaseSync.Infrastructure.DependencyInjection;
using ReleaseSync.Integration.Tests.Helpers;
using FluentAssertions;

namespace ReleaseSync.Integration.Tests.EndToEnd;

/// <summary>
/// 端到端 JSON 匯出工作流程整合測試
/// </summary>
public class JsonExportWorkflowTests
{
    /// <summary>
    /// 測試完整的同步與匯出流程
    /// </summary>
    [Fact]
    public async Task Should_Complete_Full_Sync_And_Export_Workflow()
    {
        // Arrange
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
        services.AddUserMappingServices(configuration);
        services.AddBitBucketServices(configuration);
        services.AddUserMappingServices(configuration);
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();
        services.AddScoped<IResultExporter, JsonFileExporter>();

        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<ISyncOrchestrator>();
        var exporter = serviceProvider.GetRequiredService<IResultExporter>();

        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act - Step 1: 執行同步
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

            var syncResultDto = await orchestrator.SyncAsync(syncRequest);

            // Act - Step 2: 匯出 JSON
            await exporter.ExportAsync(syncResultDto, tempFilePath, overwrite: true);

            // Assert - 驗證檔案存在
            File.Exists(tempFilePath).Should().BeTrue();

            // Assert - 驗證 JSON 內容
            var jsonContent = await File.ReadAllTextAsync(tempFilePath);
            jsonContent.Should().NotBeNullOrEmpty();

            var jsonDocument = JsonDocument.Parse(jsonContent);
            var root = jsonDocument.RootElement;

            // 驗證基本結構
            root.TryGetProperty("syncStartedAt", out _).Should().BeTrue();
            root.TryGetProperty("syncCompletedAt", out _).Should().BeTrue();

            // 驗證 PlatformStatuses
            var platformStatuses = root.GetProperty("platformStatuses");
            platformStatuses.GetArrayLength().Should().BeGreaterThanOrEqualTo(2, "應包含 GitLab 與 BitBucket 兩個平台");

            // 驗證 PullRequests
            var pullRequests = root.GetProperty("pullRequests");
            pullRequests.ValueKind.Should().Be(JsonValueKind.Array);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 測試當輸出檔案已存在且未指定 --force 時應失敗
    /// </summary>
    [Fact]
    public async Task Should_Fail_When_Output_File_Exists_And_Force_Not_Specified()
    {
        // Arrange
        var exporter = TestHelper.CreateJsonFileExporter();
        var syncResult = TestHelper.CreateSampleSyncResultDto();

        var tempFilePath = Path.GetTempFileName();

        try
        {
            // 先建立檔案
            await File.WriteAllTextAsync(tempFilePath, "existing content");

            // Act & Assert - 當 overwrite: false 時應拋出例外
            var act = async () => await exporter.ExportAsync(syncResult, tempFilePath, overwrite: false);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*已存在*");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 測試當指定 --force 時應覆寫現有檔案
    /// </summary>
    [Fact]
    public async Task Should_Overwrite_When_Force_Specified()
    {
        // Arrange
        var exporter = TestHelper.CreateJsonFileExporter();
        var syncResult = TestHelper.CreateSampleSyncResultDto();

        var tempFilePath = Path.GetTempFileName();

        try
        {
            // 先建立檔案
            await File.WriteAllTextAsync(tempFilePath, "existing content");

            // Act - 使用 overwrite: true 應成功覆寫
            await exporter.ExportAsync(syncResult, tempFilePath, overwrite: true);

            // Assert
            var content = await File.ReadAllTextAsync(tempFilePath);
            content.Should().NotBe("existing content");
            content.Should().Contain("\"pullRequests\"");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
