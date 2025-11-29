using FluentAssertions;
using ReleaseSync.Console.Handlers;

namespace ReleaseSync.Console.UnitTests.Handlers;

/// <summary>
/// SyncCommandOptions 單元測試
/// </summary>
public class SyncCommandOptionsTests
{
    /// <summary>
    /// 測試 Google Sheet ID 參數應正確接收並儲存
    /// </summary>
    [Fact]
    public void GoogleSheetId_Should_Be_Set_Correctly()
    {
        // Arrange & Act
        var options = new SyncCommandOptions
        {
            StartDate = DateTime.Parse("2025-01-01"),
            EndDate = DateTime.Parse("2025-01-31"),
            GoogleSheetId = "1A2B3C4D5E6F7G8H9I0J"
        };

        // Assert
        options.GoogleSheetId.Should().Be("1A2B3C4D5E6F7G8H9I0J");
    }

    /// <summary>
    /// 測試 Google Sheet Name 參數應正確接收並儲存
    /// </summary>
    [Fact]
    public void GoogleSheetName_Should_Be_Set_Correctly()
    {
        // Arrange & Act
        var options = new SyncCommandOptions
        {
            StartDate = DateTime.Parse("2025-01-01"),
            EndDate = DateTime.Parse("2025-01-31"),
            GoogleSheetName = "Release Log"
        };

        // Assert
        options.GoogleSheetName.Should().Be("Release Log");
    }

    /// <summary>
    /// 測試當未提供 Google Sheet 參數時,應為 null
    /// </summary>
    [Fact]
    public void GoogleSheetId_And_GoogleSheetName_Should_Be_Null_When_Not_Provided()
    {
        // Arrange & Act
        var options = new SyncCommandOptions
        {
            StartDate = DateTime.Parse("2025-01-01"),
            EndDate = DateTime.Parse("2025-01-31")
        };

        // Assert
        options.GoogleSheetId.Should().BeNull();
        options.GoogleSheetName.Should().BeNull();
    }

    /// <summary>
    /// 測試 Google Sheet 參數可以同時設定
    /// </summary>
    [Fact]
    public void GoogleSheetId_And_GoogleSheetName_Can_Be_Set_Together()
    {
        // Arrange & Act
        var options = new SyncCommandOptions
        {
            StartDate = DateTime.Parse("2025-01-01"),
            EndDate = DateTime.Parse("2025-01-31"),
            EnableGoogleSheet = true,
            GoogleSheetId = "1A2B3C4D5E6F7G8H9I0J",
            GoogleSheetName = "Release Log"
        };

        // Assert
        options.EnableGoogleSheet.Should().BeTrue();
        options.GoogleSheetId.Should().Be("1A2B3C4D5E6F7G8H9I0J");
        options.GoogleSheetName.Should().Be("Release Log");
    }

    /// <summary>
    /// 測試 ShouldSyncToGoogleSheet 計算屬性 - 啟用 Google Sheet 且有啟用平台時應為 true
    /// </summary>
    [Fact]
    public void ShouldSyncToGoogleSheet_Should_Be_True_When_Enabled_And_Platform_Active()
    {
        // Arrange & Act
        var options = new SyncCommandOptions
        {
            StartDate = DateTime.Parse("2025-01-01"),
            EndDate = DateTime.Parse("2025-01-31"),
            EnableGoogleSheet = true,
            EnableGitLab = true
        };

        // Assert
        options.ShouldSyncToGoogleSheet.Should().BeTrue();
    }

    /// <summary>
    /// 測試 ShouldSyncToGoogleSheet 計算屬性 - 啟用 Google Sheet 且有輸出檔案時應為 true
    /// </summary>
    [Fact]
    public void ShouldSyncToGoogleSheet_Should_Be_True_When_Enabled_And_OutputFile_Provided()
    {
        // Arrange & Act
        var options = new SyncCommandOptions
        {
            StartDate = DateTime.Parse("2025-01-01"),
            EndDate = DateTime.Parse("2025-01-31"),
            EnableGoogleSheet = true,
            OutputFile = "output.json"
        };

        // Assert
        options.ShouldSyncToGoogleSheet.Should().BeTrue();
    }

    /// <summary>
    /// 測試 ShouldSyncToGoogleSheet 計算屬性 - 未啟用 Google Sheet 時應為 false
    /// </summary>
    [Fact]
    public void ShouldSyncToGoogleSheet_Should_Be_False_When_Not_Enabled()
    {
        // Arrange & Act
        var options = new SyncCommandOptions
        {
            StartDate = DateTime.Parse("2025-01-01"),
            EndDate = DateTime.Parse("2025-01-31"),
            EnableGoogleSheet = false,
            EnableGitLab = true
        };

        // Assert
        options.ShouldSyncToGoogleSheet.Should().BeFalse();
    }
}
