using Microsoft.Extensions.Configuration;
using FluentAssertions;
using ReleaseSync.Console.Configuration;

namespace ReleaseSync.Integration.Tests.Configuration;

/// <summary>
/// SyncOptions 組態整合測試
/// </summary>
public class SyncOptionsIntegrationTests
{
    /// <summary>
    /// 測試從 appsettings.test.json 載入 SyncOptions 組態
    /// </summary>
    [Fact]
    public void Should_Load_SyncOptions_From_AppSettings()
    {
        // Arrange
        var configuration = BuildTestConfiguration();

        // Act
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

        // Assert
        syncOptions.Should().NotBeNull();
        syncOptions!.StartDate.Should().NotBe(default);
        syncOptions.EndDate.Should().NotBe(default);
    }

    /// <summary>
    /// 測試載入的 SyncOptions 應通過驗證
    /// </summary>
    [Fact]
    public void Loaded_SyncOptions_Should_Pass_Validation()
    {
        // Arrange
        var configuration = BuildTestConfiguration();
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

        // Act
        var act = () => syncOptions!.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 SyncOptions 所有布林屬性應正確載入
    /// </summary>
    [Fact]
    public void Should_Load_All_Boolean_Properties_Correctly()
    {
        // Arrange
        var configuration = BuildTestConfiguration();

        // Act
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

        // Assert
        syncOptions.Should().NotBeNull();
        syncOptions!.EnableGitLab.Should().NotBeNull();
        syncOptions.EnableBitBucket.Should().NotBeNull();
        syncOptions.EnableAzureDevOps.Should().NotBeNull();
        syncOptions.EnableExport.Should().NotBeNull();
        syncOptions.Force.Should().NotBeNull();
        syncOptions.Verbose.Should().NotBeNull();
        syncOptions.EnableGoogleSheet.Should().NotBeNull();
    }

    /// <summary>
    /// 測試 SyncOptions 日期屬性應正確載入為 DateTime 類型
    /// </summary>
    [Fact]
    public void Should_Load_Date_Properties_As_DateTime()
    {
        // Arrange
        var configuration = BuildTestConfiguration();

        // Act
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

        // Assert
        syncOptions.Should().NotBeNull();
        syncOptions!.StartDate.Should().BeAfter(DateTime.MinValue);
        syncOptions.EndDate.Should().BeAfter(DateTime.MinValue);
        syncOptions.StartDate.Should().BeBefore(syncOptions.EndDate);
    }

    /// <summary>
    /// 測試 SyncOptions 輸出檔案路徑應正確載入
    /// </summary>
    [Fact]
    public void Should_Load_OutputFile_Property()
    {
        // Arrange
        var configuration = BuildTestConfiguration();

        // Act
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

        // Assert
        syncOptions.Should().NotBeNull();
        // OutputFile 可能為 null 或有值,取決於 appsettings.test.json
        if (syncOptions!.EnableExport)
        {
            syncOptions.OutputFile.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>
    /// 測試環境變數應能覆蓋 SyncOptions 設定
    /// </summary>
    [Fact]
    public void Should_Override_SyncOptions_With_Environment_Variables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SyncOptions__Verbose", "true");
        Environment.SetEnvironmentVariable("SyncOptions__Force", "true");

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(GetTestConfigurationPath())
                .AddJsonFile("appsettings.test.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            // Act
            var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

            // Assert
            syncOptions.Should().NotBeNull();
            syncOptions!.Verbose.Should().BeTrue();
            syncOptions.Force.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("SyncOptions__Verbose", null);
            Environment.SetEnvironmentVariable("SyncOptions__Force", null);
        }
    }

    /// <summary>
    /// 測試 ToCommandOptions 轉換後的物件應包含所有屬性
    /// </summary>
    [Fact]
    public void ToCommandOptions_Should_Preserve_All_Properties()
    {
        // Arrange
        var configuration = BuildTestConfiguration();
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

        // Act
        var commandOptions = syncOptions!.ToCommandOptions();

        // Assert
        commandOptions.Should().NotBeNull();
        commandOptions.StartDate.Should().Be(syncOptions.StartDate);
        commandOptions.EndDate.Should().Be(syncOptions.EndDate);
        commandOptions.EnableGitLab.Should().Be(syncOptions.EnableGitLab);
        commandOptions.EnableBitBucket.Should().Be(syncOptions.EnableBitBucket);
        commandOptions.EnableAzureDevOps.Should().Be(syncOptions.EnableAzureDevOps);
        commandOptions.EnableExport.Should().Be(syncOptions.EnableExport);
        commandOptions.OutputFile.Should().Be(syncOptions.OutputFile);
        commandOptions.Force.Should().Be(syncOptions.Force);
        commandOptions.Verbose.Should().Be(syncOptions.Verbose);
        commandOptions.EnableGoogleSheet.Should().Be(syncOptions.EnableGoogleSheet);
        commandOptions.GoogleSheetId.Should().Be(syncOptions.GoogleSheetId);
        commandOptions.GoogleSheetName.Should().Be(syncOptions.GoogleSheetName);
    }

    /// <summary>
    /// 測試完整的組態載入與驗證流程
    /// </summary>
    [Fact]
    public void Complete_Configuration_Loading_And_Validation_Workflow_Should_Succeed()
    {
        // Arrange
        var configuration = BuildTestConfiguration();

        // Act
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

        // Assert - 載入成功
        syncOptions.Should().NotBeNull();

        // Act - 驗證
        var validateAction = () => syncOptions!.Validate();
        validateAction.Should().NotThrow();

        // Act - 轉換
        var commandOptions = syncOptions!.ToCommandOptions();

        // Assert - 轉換成功
        commandOptions.Should().NotBeNull();
        commandOptions.StartDate.Should().Be(syncOptions.StartDate);
        commandOptions.EndDate.Should().Be(syncOptions.EndDate);
    }

    /// <summary>
    /// 測試當組態缺少 required 屬性時應正確處理
    /// </summary>
    [Fact]
    public void Should_Handle_Missing_Required_Properties()
    {
        // Arrange - 建立不完整的組態
        var incompleteJson = """
        {
          "SyncOptions": {
            "EnableGitLab": true
          }
        }
        """;

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, incompleteJson);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(tempFile, optional: false)
                .Build();

            // Act
            var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();

            // Assert - 應該無法建立物件或拋出例外
            // 由於 required 關鍵字,物件建立會失敗
            syncOptions.Should().BeNull();
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    #region Helper Methods

    /// <summary>
    /// 建立測試用的 Configuration 物件
    /// </summary>
    private static IConfiguration BuildTestConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(GetTestConfigurationPath())
            .AddJsonFile("appsettings.test.json", optional: false)
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

    #endregion
}
