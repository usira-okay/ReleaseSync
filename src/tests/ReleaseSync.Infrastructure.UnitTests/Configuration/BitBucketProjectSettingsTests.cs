namespace ReleaseSync.Infrastructure.UnitTests.Configuration;

using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Configuration;
using Xunit;

/// <summary>
/// BitBucketProjectSettings 配置測試
/// </summary>
public class BitBucketProjectSettingsTests
{
    /// <summary>
    /// TargetBranch 應為單一字串（非陣列）
    /// </summary>
    [Fact]
    public void TargetBranch_ShouldBeSingleString()
    {
        // Arrange & Act
        var settings = new BitBucketProjectSettings
        {
            WorkspaceAndRepo = "store/webstore.webmall",
            TargetBranch = "develop"
        };

        // Assert
        Assert.NotNull(settings.TargetBranch);
        Assert.Equal("develop", settings.TargetBranch);
    }

    /// <summary>
    /// FetchMode 屬性應接受 DateRange 或 ReleaseBranch
    /// </summary>
    [Theory]
    [InlineData(FetchMode.DateRange)]
    [InlineData(FetchMode.ReleaseBranch)]
    public void FetchMode_ShouldAcceptValidValues(FetchMode mode)
    {
        // Arrange & Act
        var settings = new BitBucketProjectSettings
        {
            WorkspaceAndRepo = "store/webstore.webmall",
            TargetBranch = "develop",
            FetchMode = mode
        };

        // Assert
        Assert.Equal(mode, settings.FetchMode);
    }

    /// <summary>
    /// ReleaseBranch 應為可選屬性（用於 Repository 層級覆寫）
    /// </summary>
    [Fact]
    public void ReleaseBranch_ShouldBeOptional()
    {
        // Arrange & Act
        var settings = new BitBucketProjectSettings
        {
            WorkspaceAndRepo = "store/webstore.webmall",
            TargetBranch = "develop",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120"
        };

        // Assert
        Assert.Equal("release/20260120", settings.ReleaseBranch);
    }

    /// <summary>
    /// 完整的 ReleaseBranch 模式配置
    /// </summary>
    [Fact]
    public void ReleaseBranchMode_ShouldWorkWithAllProperties()
    {
        // Arrange & Act
        var settings = new BitBucketProjectSettings
        {
            WorkspaceAndRepo = "store/webstore.webmall",
            TargetBranch = "develop",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120",
            StartDate = null,
            EndDate = null
        };

        // Assert
        Assert.Equal("store/webstore.webmall", settings.WorkspaceAndRepo);
        Assert.Equal("develop", settings.TargetBranch);
        Assert.Equal(FetchMode.ReleaseBranch, settings.FetchMode);
        Assert.Equal("release/20260120", settings.ReleaseBranch);
        Assert.Null(settings.StartDate);
        Assert.Null(settings.EndDate);
    }

    /// <summary>
    /// StartDate 和 EndDate 應為可選屬性
    /// </summary>
    [Fact]
    public void StartAndEndDates_ShouldBeOptional()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 1, 31);

        // Act
        var settings = new BitBucketProjectSettings
        {
            WorkspaceAndRepo = "store/webstore.webmall",
            TargetBranch = "develop",
            FetchMode = FetchMode.DateRange,
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        Assert.Equal(startDate, settings.StartDate);
        Assert.Equal(endDate, settings.EndDate);
    }
}
