namespace ReleaseSync.Infrastructure.UnitTests.Configuration;

using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Configuration;
using Xunit;

/// <summary>
/// GitLabProjectSettings 配置測試
/// </summary>
public class GitLabProjectSettingsTests
{
    /// <summary>
    /// TargetBranch 應為單一字串（非陣列）
    /// </summary>
    [Fact]
    public void TargetBranch_ShouldBeSingleString()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master"
        };

        // Assert
        Assert.NotNull(settings.TargetBranch);
        Assert.Equal("master", settings.TargetBranch);
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
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
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
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120"
        };

        // Assert
        Assert.Equal("release/20260120", settings.ReleaseBranch);
    }

    /// <summary>
    /// ReleaseBranch 可為 null（使用全域配置）
    /// </summary>
    [Fact]
    public void ReleaseBranch_CanBeNull()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = null
        };

        // Assert
        Assert.Null(settings.ReleaseBranch);
    }

    /// <summary>
    /// StartDate 應為可選屬性（用於 Repository 層級覆寫）
    /// </summary>
    [Fact]
    public void StartDate_ShouldBeOptional()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);

        // Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.DateRange,
            StartDate = startDate
        };

        // Assert
        Assert.Equal(startDate, settings.StartDate);
    }

    /// <summary>
    /// EndDate 應為可選屬性（用於 Repository 層級覆寫）
    /// </summary>
    [Fact]
    public void EndDate_ShouldBeOptional()
    {
        // Arrange
        var endDate = new DateTime(2026, 1, 31);

        // Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.DateRange,
            EndDate = endDate
        };

        // Assert
        Assert.Equal(endDate, settings.EndDate);
    }

    /// <summary>
    /// 完整配置應能正確設定所有屬性
    /// </summary>
    [Fact]
    public void FullConfiguration_ShouldSetAllProperties()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120",
            StartDate = new DateTime(2026, 1, 10),
            EndDate = new DateTime(2026, 1, 20)
        };

        // Assert
        Assert.Equal("payment/payment.adminapi", settings.ProjectPath);
        Assert.Equal("master", settings.TargetBranch);
        Assert.Equal(FetchMode.ReleaseBranch, settings.FetchMode);
        Assert.Equal("release/20260120", settings.ReleaseBranch);
        Assert.Equal(new DateTime(2026, 1, 10), settings.StartDate);
        Assert.Equal(new DateTime(2026, 1, 20), settings.EndDate);
    }

    /// <summary>
    /// 使用 DateRange 模式的完整配置範例
    /// </summary>
    [Fact]
    public void DateRangeMode_ShouldWorkWithStartAndEndDates()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.management",
            TargetBranch = "master",
            FetchMode = FetchMode.DateRange,
            ReleaseBranch = null,
            StartDate = new DateTime(2026, 1, 10),
            EndDate = new DateTime(2026, 1, 17)
        };

        // Assert
        Assert.Equal(FetchMode.DateRange, settings.FetchMode);
        Assert.Null(settings.ReleaseBranch);
        Assert.NotNull(settings.StartDate);
        Assert.NotNull(settings.EndDate);
    }

    /// <summary>
    /// 使用 ReleaseBranch 模式的完整配置範例
    /// </summary>
    [Fact]
    public void ReleaseBranchMode_ShouldWorkWithReleaseBranch()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260118",
            StartDate = null,
            EndDate = null
        };

        // Assert
        Assert.Equal(FetchMode.ReleaseBranch, settings.FetchMode);
        Assert.Equal("release/20260118", settings.ReleaseBranch);
        Assert.Null(settings.StartDate);
        Assert.Null(settings.EndDate);
    }
}
