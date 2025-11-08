using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReleaseSync.Infrastructure.Configuration;
using FluentAssertions;

namespace ReleaseSync.Integration.Tests.Configuration;

/// <summary>
/// 組態檔載入整合測試
/// </summary>
public class ConfigurationLoadingTests
{
    /// <summary>
    /// 測試 appsettings.json 是否能正確載入 GitLab 設定
    /// </summary>
    [Fact]
    public void Should_Load_GitLab_Settings_From_AppSettings()
    {
        // Arrange
        var configuration = BuildTestConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<GitLabSettings>(configuration.GetSection("GitLab"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var gitLabSettings = serviceProvider.GetRequiredService<IOptions<GitLabSettings>>().Value;

        // Assert
        gitLabSettings.Should().NotBeNull();
        gitLabSettings.ApiUrl.Should().NotBeNullOrEmpty();
        gitLabSettings.PersonalAccessToken.Should().NotBeNullOrEmpty();
        gitLabSettings.Projects.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 測試 appsettings.json 是否能正確載入 BitBucket 設定
    /// </summary>
    [Fact]
    public void Should_Load_BitBucket_Settings_From_AppSettings()
    {
        // Arrange
        var configuration = BuildTestConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<BitBucketSettings>(configuration.GetSection("BitBucket"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var bitBucketSettings = serviceProvider.GetRequiredService<IOptions<BitBucketSettings>>().Value;

        // Assert
        bitBucketSettings.Should().NotBeNull();
        bitBucketSettings.ApiUrl.Should().NotBeNullOrEmpty();
        bitBucketSettings.Email.Should().NotBeNullOrEmpty();
        bitBucketSettings.AccessToken.Should().NotBeNullOrEmpty();
        bitBucketSettings.Projects.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 測試 appsettings.json 是否能正確載入 Azure DevOps 設定
    /// </summary>
    [Fact]
    public void Should_Load_AzureDevOps_Settings_From_AppSettings()
    {
        // Arrange
        var configuration = BuildTestConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<AzureDevOpsSettings>(configuration.GetSection("AzureDevOps"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureDevOpsSettings = serviceProvider.GetRequiredService<IOptions<AzureDevOpsSettings>>().Value;

        // Assert
        azureDevOpsSettings.Should().NotBeNull();
        azureDevOpsSettings.OrganizationUrl.Should().NotBeNullOrEmpty();
        azureDevOpsSettings.ProjectName.Should().NotBeNullOrEmpty();
        azureDevOpsSettings.PersonalAccessToken.Should().NotBeNullOrEmpty();
        azureDevOpsSettings.WorkItemIdPatterns.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 測試組態檔中的 GitLab Projects 設定格式是否正確
    /// </summary>
    [Fact]
    public void Should_Parse_GitLab_Projects_Correctly()
    {
        // Arrange
        var configuration = BuildTestConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<GitLabSettings>(configuration.GetSection("GitLab"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var gitLabSettings = serviceProvider.GetRequiredService<IOptions<GitLabSettings>>().Value;

        // Assert
        gitLabSettings.Projects.Should().HaveCountGreaterThan(0);
        foreach (var project in gitLabSettings.Projects)
        {
            project.ProjectPath.Should().NotBeNullOrEmpty();
            project.TargetBranches.Should().NotBeNull();
        }
    }

    /// <summary>
    /// 測試組態檔中的 Azure DevOps Work Item ID Patterns 設定格式是否正確
    /// </summary>
    [Fact]
    public void Should_Parse_AzureDevOps_WorkItemIdPatterns_Correctly()
    {
        // Arrange
        var configuration = BuildTestConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<AzureDevOpsSettings>(configuration.GetSection("AzureDevOps"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureDevOpsSettings = serviceProvider.GetRequiredService<IOptions<AzureDevOpsSettings>>().Value;

        // Assert
        azureDevOpsSettings.WorkItemIdPatterns.Should().HaveCountGreaterThan(0);
        foreach (var pattern in azureDevOpsSettings.WorkItemIdPatterns)
        {
            pattern.Name.Should().NotBeNullOrEmpty();
            pattern.Regex.Should().NotBeNullOrEmpty();
            pattern.CaptureGroup.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// 測試當 User Secrets 不存在時,應能正常處理 (optional: true)
    /// </summary>
    [Fact]
    public void Should_Handle_Missing_Secure_Configuration_Gracefully()
    {
        // Arrange - 僅載入 appsettings.json,不載入 User Secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(GetTestConfigurationPath())
            .AddJsonFile("appsettings.test.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<GitLabSettings>(configuration.GetSection("GitLab"));

        // Act & Assert - 應能正常建立 ServiceProvider,即使缺少 secure.json
        var serviceProvider = services.BuildServiceProvider();
        var gitLabSettings = serviceProvider.GetService<IOptions<GitLabSettings>>()?.Value;

        gitLabSettings.Should().NotBeNull();
    }

    /// <summary>
    /// 測試環境變數是否能覆蓋組態檔設定
    /// </summary>
    [Fact]
    public void Should_Override_Settings_With_Environment_Variables()
    {
        // Arrange
        const string testToken = "test-token-from-env";
        Environment.SetEnvironmentVariable("GitLab__PersonalAccessToken", testToken);

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(GetTestConfigurationPath())
                .AddJsonFile("appsettings.test.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<GitLabSettings>(configuration.GetSection("GitLab"));
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var gitLabSettings = serviceProvider.GetRequiredService<IOptions<GitLabSettings>>().Value;

            // Assert
            gitLabSettings.PersonalAccessToken.Should().Be(testToken);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("GitLab__PersonalAccessToken", null);
        }
    }

    /// <summary>
    /// 建立測試用的 Configuration 物件
    /// </summary>
    private static IConfiguration BuildTestConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(GetTestConfigurationPath())
            .AddJsonFile("appsettings.test.json", optional: false)
            .AddJsonFile("appsettings.test.secure.json", optional: true)
            .Build();
    }

    /// <summary>
    /// 取得測試組態檔路徑
    /// </summary>
    private static string GetTestConfigurationPath()
    {
        // 尋找 appsettings.test.json 的路徑
        var currentDirectory = Directory.GetCurrentDirectory();
        var testConfigPath = Path.Combine(currentDirectory, "TestData");

        if (!Directory.Exists(testConfigPath))
        {
            Directory.CreateDirectory(testConfigPath);
        }

        return testConfigPath;
    }
}
