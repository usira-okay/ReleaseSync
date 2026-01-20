using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.UnitTests.Configuration;

/// <summary>
/// GitLabProjectSettings 單元測試
/// </summary>
public class GitLabProjectSettingsTests
{
    #region TargetBranch 單一值測試

    /// <summary>
    /// 測試 TargetBranch 屬性可正常設定與讀取
    /// </summary>
    [Fact]
    public void TargetBranch_WhenSet_ShouldRetainValue()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
        };

        // Assert
        Assert.Equal("master", settings.TargetBranch);
    }

    /// <summary>
    /// 測試 TargetBranch 為 required 屬性（必填）
    /// </summary>
    [Fact]
    public void TargetBranch_ShouldBeRequired()
    {
        // Arrange
        var propertyInfo = typeof(GitLabProjectSettings).GetProperty(nameof(GitLabProjectSettings.TargetBranch));

        // Assert
        Assert.NotNull(propertyInfo);
        // required 屬性會標記為 non-nullable
        var nullabilityContext = new System.Reflection.NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(propertyInfo);
        Assert.Equal(System.Reflection.NullabilityState.NotNull, nullabilityInfo.WriteState);
    }

    #endregion

    #region 向後相容 TargetBranches 測試

    /// <summary>
    /// 測試向後相容 - TargetBranches 屬性仍可使用
    /// </summary>
    [Fact]
    public void TargetBranches_ShouldStillExistForBackwardCompatibility()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            TargetBranches = new List<string> { "master", "develop" }
        };

        // Assert
        Assert.NotNull(settings.TargetBranches);
        Assert.Equal(2, settings.TargetBranches.Count);
    }

    /// <summary>
    /// 測試 TargetBranches 預設為空清單
    /// </summary>
    [Fact]
    public void TargetBranches_Default_ShouldBeEmptyList()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
        };

        // Assert
        Assert.NotNull(settings.TargetBranches);
        Assert.Empty(settings.TargetBranches);
    }

    /// <summary>
    /// 測試 TargetBranches 屬性應標記為 Obsolete
    /// </summary>
    [Fact]
    public void TargetBranches_ShouldBeMarkedAsObsolete()
    {
        // Arrange
        var propertyInfo = typeof(GitLabProjectSettings).GetProperty(nameof(GitLabProjectSettings.TargetBranches));

        // Assert
        Assert.NotNull(propertyInfo);
        var obsoleteAttribute = propertyInfo.GetCustomAttributes(typeof(ObsoleteAttribute), false).FirstOrDefault();
        Assert.NotNull(obsoleteAttribute);
    }

    #endregion

    #region FetchMode 覆寫屬性測試

    /// <summary>
    /// 測試 FetchMode 覆寫屬性預設為 null
    /// </summary>
    [Fact]
    public void FetchMode_Default_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
        };

        // Assert
        Assert.Null(settings.FetchMode);
    }

    /// <summary>
    /// 測試 FetchMode 可設定為 ReleaseBranch
    /// </summary>
    [Fact]
    public void FetchMode_WhenSetToReleaseBranch_ShouldRetainValue()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch
        };

        // Assert
        Assert.Equal(FetchMode.ReleaseBranch, settings.FetchMode);
    }

    #endregion

    #region ReleaseBranch 覆寫屬性測試

    /// <summary>
    /// 測試 ReleaseBranch 覆寫屬性預設為 null
    /// </summary>
    [Fact]
    public void ReleaseBranch_Default_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
        };

        // Assert
        Assert.Null(settings.ReleaseBranch);
    }

    /// <summary>
    /// 測試 ReleaseBranch 可正常設定
    /// </summary>
    [Fact]
    public void ReleaseBranch_WhenSet_ShouldRetainValue()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            ReleaseBranch = "release/20260120"
        };

        // Assert
        Assert.Equal("release/20260120", settings.ReleaseBranch);
    }

    #endregion

    #region StartDate/EndDate 覆寫屬性測試

    /// <summary>
    /// 測試 StartDate 覆寫屬性預設為 null
    /// </summary>
    [Fact]
    public void StartDate_Default_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
        };

        // Assert
        Assert.Null(settings.StartDate);
    }

    /// <summary>
    /// 測試 EndDate 覆寫屬性預設為 null
    /// </summary>
    [Fact]
    public void EndDate_Default_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
        };

        // Assert
        Assert.Null(settings.EndDate);
    }

    /// <summary>
    /// 測試 StartDate 和 EndDate 可正常設定
    /// </summary>
    [Fact]
    public void StartDateAndEndDate_WhenSet_ShouldRetainValues()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 13);
        var endDate = new DateTime(2026, 1, 20);

        // Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        Assert.Equal(startDate, settings.StartDate);
        Assert.Equal(endDate, settings.EndDate);
    }

    #endregion

    #region 完整設定測試

    /// <summary>
    /// 測試所有新屬性可同時設定
    /// </summary>
    [Fact]
    public void AllNewProperties_CanBeSetTogether()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 13);
        var endDate = new DateTime(2026, 1, 20);

        // Act
        var settings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260118",
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        Assert.Equal("payment/payment.adminapi", settings.ProjectPath);
        Assert.Equal("master", settings.TargetBranch);
        Assert.Equal(FetchMode.ReleaseBranch, settings.FetchMode);
        Assert.Equal("release/20260118", settings.ReleaseBranch);
        Assert.Equal(startDate, settings.StartDate);
        Assert.Equal(endDate, settings.EndDate);
    }

    #endregion
}
