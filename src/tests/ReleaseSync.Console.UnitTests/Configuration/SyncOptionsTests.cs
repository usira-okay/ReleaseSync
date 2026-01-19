namespace ReleaseSync.Console.UnitTests.Configuration;

using ReleaseSync.Console.Configuration;
using Xunit;

/// <summary>
/// SyncOptions 配置測試
/// </summary>
public class SyncOptionsTests
{
    /// <summary>
    /// ReleaseBranch 屬性應為可選（用於全域 release branch 設定）
    /// </summary>
    [Fact]
    public void ReleaseBranch_ShouldBeOptional()
    {
        // Arrange & Act
        var options = new SyncOptions
        {
            StartDate = new DateTime(2026, 1, 13),
            EndDate = new DateTime(2026, 1, 20),
            ReleaseBranch = "release/20260120",
            EnabledPlatforms = new EnabledPlatformsSettings(),
            Export = new ExportSettings(),
            GoogleSheet = new GoogleSheetSyncSettings(),
            Verbose = false
        };

        // Assert
        Assert.Equal("release/20260120", options.ReleaseBranch);
    }

    /// <summary>
    /// ReleaseBranch 可以為 null（使用時間範圍模式）
    /// </summary>
    [Fact]
    public void ReleaseBranch_CanBeNull()
    {
        // Arrange & Act
        var options = new SyncOptions
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            ReleaseBranch = null,
            EnabledPlatforms = new EnabledPlatformsSettings(),
            Export = new ExportSettings(),
            GoogleSheet = new GoogleSheetSyncSettings(),
            Verbose = false
        };

        // Assert
        Assert.Null(options.ReleaseBranch);
    }

    /// <summary>
    /// 完整配置應包含 ReleaseBranch、StartDate、EndDate
    /// </summary>
    [Fact]
    public void FullConfiguration_ShouldIncludeAllTimeRangeProperties()
    {
        // Arrange & Act
        var options = new SyncOptions
        {
            StartDate = new DateTime(2026, 1, 13),
            EndDate = new DateTime(2026, 1, 20),
            ReleaseBranch = "release/20260120",
            EnabledPlatforms = new EnabledPlatformsSettings
            {
                GitLab = true,
                BitBucket = true,
                AzureDevOps = false
            },
            Export = new ExportSettings
            {
                Enabled = true,
                OutputFile = "output.json"
            },
            GoogleSheet = new GoogleSheetSyncSettings
            {
                Enabled = false
            },
            Verbose = true
        };

        // Assert
        Assert.Equal(new DateTime(2026, 1, 13), options.StartDate);
        Assert.Equal(new DateTime(2026, 1, 20), options.EndDate);
        Assert.Equal("release/20260120", options.ReleaseBranch);
        Assert.True(options.EnabledPlatforms.GitLab);
        Assert.True(options.EnabledPlatforms.BitBucket);
        Assert.True(options.Verbose);
    }

    /// <summary>
    /// 驗證 SyncOptions 保留原有的所有屬性
    /// </summary>
    [Fact]
    public void SyncOptions_ShouldRetainAllExistingProperties()
    {
        // Arrange & Act
        var options = new SyncOptions
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            ReleaseBranch = null,
            EnabledPlatforms = new EnabledPlatformsSettings
            {
                GitLab = false,
                BitBucket = false,
                AzureDevOps = false
            },
            Export = new ExportSettings
            {
                Enabled = false,
                OutputFile = "output.json",
                Force = false
            },
            GoogleSheet = new GoogleSheetSyncSettings
            {
                Enabled = false
            },
            Verbose = false
        };

        // Assert - 驗證所有原有屬性仍然存在
        Assert.NotNull(options.StartDate);
        Assert.NotNull(options.EndDate);
        Assert.NotNull(options.EnabledPlatforms);
        Assert.NotNull(options.Export);
        Assert.NotNull(options.GoogleSheet);
        Assert.False(options.Verbose);
    }
}
