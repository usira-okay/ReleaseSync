using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Configuration;

namespace ReleaseSync.Infrastructure.UnitTests.Configuration;

/// <summary>
/// EffectiveProjectConfig 配置覆寫解析單元測試
/// </summary>
public class EffectiveProjectConfigTests
{
    #region FetchMode 解析測試

    /// <summary>
    /// 測試專案層級 FetchMode 覆寫全域設定
    /// </summary>
    [Fact]
    public void Resolve_ProjectFetchModeOverridesGlobal()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings
        {
            DefaultFetchMode = FetchMode.DateRange
        };

        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal(FetchMode.ReleaseBranch, config.FetchMode);
    }

    /// <summary>
    /// 測試未設定專案層級 FetchMode 時使用全域設定
    /// </summary>
    [Fact]
    public void Resolve_UsesGlobalFetchMode_WhenProjectNotSet()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings
        {
            DefaultFetchMode = FetchMode.ReleaseBranch
        };

        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
            // FetchMode 未設定
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal(FetchMode.ReleaseBranch, config.FetchMode);
    }

    #endregion

    #region ReleaseBranch 解析測試

    /// <summary>
    /// 測試專案層級 ReleaseBranch 覆寫全域設定
    /// </summary>
    [Fact]
    public void Resolve_ProjectReleaseBranchOverridesGlobal()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings
        {
            ReleaseBranch = "release/20260113"
        };

        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            ReleaseBranch = "release/20260120"
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal("release/20260120", config.ReleaseBranch);
    }

    /// <summary>
    /// 測試未設定專案層級 ReleaseBranch 時使用全域設定
    /// </summary>
    [Fact]
    public void Resolve_UsesGlobalReleaseBranch_WhenProjectNotSet()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings
        {
            ReleaseBranch = "release/20260113"
        };

        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
            // ReleaseBranch 未設定
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal("release/20260113", config.ReleaseBranch);
    }

    #endregion

    #region StartDate/EndDate 解析測試

    /// <summary>
    /// 測試專案層級 StartDate 和 EndDate 覆寫全域設定
    /// </summary>
    [Fact]
    public void Resolve_ProjectDatesOverrideGlobal()
    {
        // Arrange
        var globalStartDate = new DateTime(2026, 1, 1);
        var globalEndDate = new DateTime(2026, 1, 31);
        var projectStartDate = new DateTime(2026, 1, 10);
        var projectEndDate = new DateTime(2026, 1, 17);

        var globalSettings = new SyncOptionsSettings
        {
            StartDate = globalStartDate,
            EndDate = globalEndDate
        };

        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            StartDate = projectStartDate,
            EndDate = projectEndDate
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal(projectStartDate, config.StartDate);
        Assert.Equal(projectEndDate, config.EndDate);
    }

    /// <summary>
    /// 測試未設定專案層級日期時使用全域設定
    /// </summary>
    [Fact]
    public void Resolve_UsesGlobalDates_WhenProjectNotSet()
    {
        // Arrange
        var globalStartDate = new DateTime(2026, 1, 1);
        var globalEndDate = new DateTime(2026, 1, 31);

        var globalSettings = new SyncOptionsSettings
        {
            StartDate = globalStartDate,
            EndDate = globalEndDate
        };

        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master"
            // StartDate 和 EndDate 未設定
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal(globalStartDate, config.StartDate);
        Assert.Equal(globalEndDate, config.EndDate);
    }

    #endregion

    #region TargetBranch 和 ProjectIdentifier 測試

    /// <summary>
    /// 測試 TargetBranch 正確設定
    /// </summary>
    [Fact]
    public void Resolve_SetsTargetBranchCorrectly()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings();
        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "develop"
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal("develop", config.TargetBranch);
    }

    /// <summary>
    /// 測試 ProjectIdentifier 正確設定
    /// </summary>
    [Fact]
    public void Resolve_SetsProjectIdentifierCorrectly()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings();
        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "payment/payment.adminapi",
            TargetBranch = "master"
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal("payment/payment.adminapi", config.ProjectIdentifier);
    }

    #endregion

    #region 混合覆寫測試

    /// <summary>
    /// 測試部分覆寫情境（專案設定部分屬性）
    /// </summary>
    [Fact]
    public void Resolve_PartialOverride_MixesGlobalAndProjectSettings()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings
        {
            DefaultFetchMode = FetchMode.DateRange,
            ReleaseBranch = "release/20260113",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };

        var projectSettings = new GitLabProjectSettings
        {
            ProjectPath = "mygroup/myproject",
            TargetBranch = "master",
            // 只覆寫 FetchMode 和 ReleaseBranch
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120"
            // StartDate 和 EndDate 使用全域設定
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.ProjectPath,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal(FetchMode.ReleaseBranch, config.FetchMode);
        Assert.Equal("release/20260120", config.ReleaseBranch);
        Assert.Equal(new DateTime(2026, 1, 1), config.StartDate); // 使用全域
        Assert.Equal(new DateTime(2026, 1, 31), config.EndDate);   // 使用全域
    }

    #endregion

    #region BitBucket 專案設定測試

    /// <summary>
    /// 測試 BitBucket 專案設定的解析
    /// </summary>
    [Fact]
    public void Resolve_WorksWithBitBucketProjectSettings()
    {
        // Arrange
        var globalSettings = new SyncOptionsSettings
        {
            DefaultFetchMode = FetchMode.DateRange,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };

        var projectSettings = new BitBucketProjectSettings
        {
            WorkspaceAndRepo = "store/webstore.webmall",
            TargetBranch = "develop",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260118"
        };

        // Act
        var config = EffectiveProjectConfig.Resolve(
            projectSettings,
            globalSettings,
            p => p.WorkspaceAndRepo,
            p => p.TargetBranch,
            p => p.FetchMode,
            p => p.ReleaseBranch,
            p => p.StartDate,
            p => p.EndDate);

        // Assert
        Assert.Equal("store/webstore.webmall", config.ProjectIdentifier);
        Assert.Equal("develop", config.TargetBranch);
        Assert.Equal(FetchMode.ReleaseBranch, config.FetchMode);
        Assert.Equal("release/20260118", config.ReleaseBranch);
    }

    #endregion
}
