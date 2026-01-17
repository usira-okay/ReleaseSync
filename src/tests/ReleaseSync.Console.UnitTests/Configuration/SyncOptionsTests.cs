using FluentAssertions;
using ReleaseSync.Console.Configuration;

namespace ReleaseSync.Console.UnitTests.Configuration;

/// <summary>
/// SyncOptions 組態類別單元測試
/// </summary>
public class SyncOptionsTests
{
    #region 基本屬性測試

    /// <summary>
    /// 測試建立有效的 SyncOptions 實例
    /// </summary>
    [Fact]
    public void Constructor_WithValidProperties_ShouldCreateInstance()
    {
        // Arrange & Act
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = true,
            EnableExport = true,
            OutputFile = "output.json",
            Force = false,
            Verbose = false,
            EnableGoogleSheet = false,
            GoogleSheetId = null,
            GoogleSheetName = null
        };

        // Assert
        options.StartDate.Should().Be(new DateTime(2025, 1, 1));
        options.EndDate.Should().Be(new DateTime(2025, 1, 31));
        options.EnableGitLab.Should().BeTrue();
        options.EnableBitBucket.Should().BeFalse();
        options.EnableAzureDevOps.Should().BeTrue();
        options.EnableExport.Should().BeTrue();
        options.OutputFile.Should().Be("output.json");
        options.Force.Should().BeFalse();
        options.Verbose.Should().BeFalse();
        options.EnableGoogleSheet.Should().BeFalse();
        options.GoogleSheetId.Should().BeNull();
        options.GoogleSheetName.Should().BeNull();
    }

    /// <summary>
    /// 測試 OutputFile 可為 null
    /// </summary>
    [Fact]
    public void OutputFile_CanBeNull()
    {
        // Arrange & Act
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            OutputFile = null
        };

        // Assert
        options.OutputFile.Should().BeNull();
    }

    /// <summary>
    /// 測試 Google Sheet 參數可為 null
    /// </summary>
    [Fact]
    public void GoogleSheetParameters_CanBeNull()
    {
        // Arrange & Act
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            GoogleSheetId = null,
            GoogleSheetName = null
        };

        // Assert
        options.GoogleSheetId.Should().BeNull();
        options.GoogleSheetName.Should().BeNull();
    }

    #endregion

    #region 驗證邏輯測試

    /// <summary>
    /// 測試 Validate 方法 - 有效組態應該通過驗證
    /// </summary>
    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 Validate 方法 - 起始日期晚於結束日期應該拋出例外
    /// </summary>
    [Fact]
    public void Validate_WhenStartDateAfterEndDate_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 2, 1),
            EndDate = new DateTime(2025, 1, 1),
            EnableGitLab = true
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*起始日期*不能晚於結束日期*");
    }

    /// <summary>
    /// 測試 Validate 方法 - 起始日期等於結束日期應該通過驗證
    /// </summary>
    [Fact]
    public void Validate_WhenStartDateEqualsEndDate_ShouldNotThrow()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 15),
            EndDate = new DateTime(2025, 1, 15),
            EnableGitLab = true
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 Validate 方法 - 未啟用任何平台應該拋出例外
    /// </summary>
    [Fact]
    public void Validate_WhenNoPlatformEnabled_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = false,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*至少必須啟用一個平台*");
    }

    /// <summary>
    /// 測試 Validate 方法 - 啟用 GitLab 時應該通過驗證
    /// </summary>
    [Fact]
    public void Validate_WhenOnlyGitLabEnabled_ShouldNotThrow()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 Validate 方法 - 啟用 BitBucket 時應該通過驗證
    /// </summary>
    [Fact]
    public void Validate_WhenOnlyBitBucketEnabled_ShouldNotThrow()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = false,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 Validate 方法 - 啟用 Azure DevOps 時應該通過驗證
    /// </summary>
    [Fact]
    public void Validate_WhenOnlyAzureDevOpsEnabled_ShouldNotThrow()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = false,
            EnableBitBucket = false,
            EnableAzureDevOps = true
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 Validate 方法 - 啟用匯出但未指定輸出檔案應該拋出例外
    /// </summary>
    [Fact]
    public void Validate_WhenEnableExportWithoutOutputFile_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            EnableExport = true,
            OutputFile = null
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*啟用匯出功能時必須指定輸出檔案*");
    }

    /// <summary>
    /// 測試 Validate 方法 - 啟用匯出且指定輸出檔案應該通過驗證
    /// </summary>
    [Fact]
    public void Validate_WhenEnableExportWithOutputFile_ShouldNotThrow()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            EnableExport = true,
            OutputFile = "output.json"
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 Validate 方法 - 未啟用匯出時輸出檔案可為 null
    /// </summary>
    [Fact]
    public void Validate_WhenExportDisabledAndOutputFileNull_ShouldNotThrow()
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            EnableExport = false,
            OutputFile = null
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 測試 Validate 方法 - 輸出檔案路徑無效應該拋出例外
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenOutputFilePathIsInvalid_ShouldThrowArgumentException(string invalidPath)
    {
        // Arrange
        var options = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            EnableExport = true,
            OutputFile = invalidPath
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*輸出檔案路徑不能為空白*");
    }

    #endregion

    #region ToCommandOptions 測試

    /// <summary>
    /// 測試 ToCommandOptions 方法 - 應該正確對映所有屬性
    /// </summary>
    [Fact]
    public void ToCommandOptions_ShouldMapAllProperties()
    {
        // Arrange
        var syncOptions = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = true,
            EnableExport = true,
            OutputFile = "output.json",
            Force = true,
            Verbose = true,
            EnableGoogleSheet = true,
            GoogleSheetId = "test-sheet-id",
            GoogleSheetName = "Test Sheet"
        };

        // Act
        var commandOptions = syncOptions.ToCommandOptions();

        // Assert
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
    /// 測試 ToCommandOptions 方法 - Nullable 屬性應該正確對映
    /// </summary>
    [Fact]
    public void ToCommandOptions_WithNullableProperties_ShouldMapCorrectly()
    {
        // Arrange
        var syncOptions = new SyncOptions
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            EnableGitLab = true,
            OutputFile = null,
            GoogleSheetId = null,
            GoogleSheetName = null
        };

        // Act
        var commandOptions = syncOptions.ToCommandOptions();

        // Assert
        commandOptions.OutputFile.Should().BeNull();
        commandOptions.GoogleSheetId.Should().BeNull();
        commandOptions.GoogleSheetName.Should().BeNull();
    }

    #endregion
}
