using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.UnitTests.Configuration;

/// <summary>
/// SyncOptionsSettings 單元測試
/// </summary>
public class SyncOptionsSettingsTests
{
    /// <summary>
    /// 測試應正確從組態檔綁定所有必要屬性
    /// </summary>
    [Fact]
    public void SyncOptionsSettings_Should_Bind_All_Properties_From_Configuration()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["SyncOptions:StartDate"] = "2025-01-01",
            ["SyncOptions:EndDate"] = "2025-01-31",
            ["SyncOptions:EnableGitLab"] = "true",
            ["SyncOptions:EnableBitBucket"] = "false",
            ["SyncOptions:EnableAzureDevOps"] = "true",
            ["SyncOptions:EnableExport"] = "true",
            ["SyncOptions:OutputFile"] = "output.json",
            ["SyncOptions:Force"] = "false",
            ["SyncOptions:Verbose"] = "true",
            ["SyncOptions:EnableGoogleSheet"] = "true",
            ["SyncOptions:GoogleSheetId"] = "1A2B3C4D5E6F",
            ["SyncOptions:GoogleSheetName"] = "Release Log"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        var settings = new SyncOptionsSettings();
        configuration.GetSection("SyncOptions").Bind(settings);

        // Assert
        settings.StartDate.Should().Be(new DateTime(2025, 1, 1));
        settings.EndDate.Should().Be(new DateTime(2025, 1, 31));
        settings.EnableGitLab.Should().BeTrue();
        settings.EnableBitBucket.Should().BeFalse();
        settings.EnableAzureDevOps.Should().BeTrue();
        settings.EnableExport.Should().BeTrue();
        settings.OutputFile.Should().Be("output.json");
        settings.Force.Should().BeFalse();
        settings.Verbose.Should().BeTrue();
        settings.EnableGoogleSheet.Should().BeTrue();
        settings.GoogleSheetId.Should().Be("1A2B3C4D5E6F");
        settings.GoogleSheetName.Should().Be("Release Log");
    }

    /// <summary>
    /// 測試當未提供可選參數時,應使用預設值
    /// </summary>
    [Fact]
    public void SyncOptionsSettings_Should_Use_Default_Values_When_Not_Provided()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["SyncOptions:StartDate"] = "2025-01-01",
            ["SyncOptions:EndDate"] = "2025-01-31"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        var settings = new SyncOptionsSettings();
        configuration.GetSection("SyncOptions").Bind(settings);

        // Assert
        settings.StartDate.Should().Be(new DateTime(2025, 1, 1));
        settings.EndDate.Should().Be(new DateTime(2025, 1, 31));
        settings.EnableGitLab.Should().BeFalse(); // 預設值
        settings.EnableBitBucket.Should().BeFalse();
        settings.EnableAzureDevOps.Should().BeFalse();
        settings.EnableExport.Should().BeFalse();
        settings.OutputFile.Should().BeNull();
        settings.Force.Should().BeFalse();
        settings.Verbose.Should().BeFalse();
        settings.EnableGoogleSheet.Should().BeFalse();
        settings.GoogleSheetId.Should().BeNull();
        settings.GoogleSheetName.Should().BeNull();
    }

    /// <summary>
    /// 測試應正確處理日期時間格式
    /// </summary>
    [Fact]
    public void SyncOptionsSettings_Should_Parse_DateTime_Correctly()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["SyncOptions:StartDate"] = "2025-06-15T10:30:00",
            ["SyncOptions:EndDate"] = "2025-12-31T23:59:59"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        var settings = new SyncOptionsSettings();
        configuration.GetSection("SyncOptions").Bind(settings);

        // Assert
        settings.StartDate.Should().Be(new DateTime(2025, 6, 15, 10, 30, 0));
        settings.EndDate.Should().Be(new DateTime(2025, 12, 31, 23, 59, 59));
    }
}
