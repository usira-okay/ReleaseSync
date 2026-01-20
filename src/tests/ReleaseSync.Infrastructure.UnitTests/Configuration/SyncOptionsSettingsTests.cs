using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.UnitTests.Configuration;

/// <summary>
/// SyncOptionsSettings 單元測試
/// </summary>
public class SyncOptionsSettingsTests
{
    /// <summary>
    /// 測試預設值 - DefaultFetchMode 應為 DateRange
    /// </summary>
    [Fact]
    public void DefaultFetchMode_ShouldBeDataRange()
    {
        // Arrange & Act
        var settings = new SyncOptionsSettings();

        // Assert
        Assert.Equal(FetchMode.DateRange, settings.DefaultFetchMode);
    }

    /// <summary>
    /// 測試預設值 - ReleaseBranch 應為 null
    /// </summary>
    [Fact]
    public void ReleaseBranch_Default_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new SyncOptionsSettings();

        // Assert
        Assert.Null(settings.ReleaseBranch);
    }

    /// <summary>
    /// 測試預設值 - StartDate 應為 null
    /// </summary>
    [Fact]
    public void StartDate_Default_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new SyncOptionsSettings();

        // Assert
        Assert.Null(settings.StartDate);
    }

    /// <summary>
    /// 測試預設值 - EndDate 應為 null
    /// </summary>
    [Fact]
    public void EndDate_Default_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new SyncOptionsSettings();

        // Assert
        Assert.Null(settings.EndDate);
    }

    /// <summary>
    /// 測試設定所有屬性
    /// </summary>
    [Fact]
    public void AllProperties_WhenSet_ShouldRetainValues()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 13);
        var endDate = new DateTime(2026, 1, 20);

        // Act
        var settings = new SyncOptionsSettings
        {
            ReleaseBranch = "release/20260120",
            StartDate = startDate,
            EndDate = endDate,
            DefaultFetchMode = FetchMode.ReleaseBranch
        };

        // Assert
        Assert.Equal("release/20260120", settings.ReleaseBranch);
        Assert.Equal(startDate, settings.StartDate);
        Assert.Equal(endDate, settings.EndDate);
        Assert.Equal(FetchMode.ReleaseBranch, settings.DefaultFetchMode);
    }
}
