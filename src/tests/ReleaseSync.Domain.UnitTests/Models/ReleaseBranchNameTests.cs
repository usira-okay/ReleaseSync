namespace ReleaseSync.Domain.UnitTests.Models;

using ReleaseSync.Domain.Models;
using Xunit;

/// <summary>
/// ReleaseBranchName value object 測試
/// </summary>
public class ReleaseBranchNameTests
{
    /// <summary>
    /// 建構子應接受有效的 release branch 名稱
    /// </summary>
    [Theory]
    [InlineData("release/20260119")]
    [InlineData("release/20251231")]
    [InlineData("release/20260101")]
    public void Constructor_WithValidReleaseBranch_ShouldCreateInstance(string branchName)
    {
        // Arrange & Act
        var releaseBranch = new ReleaseBranchName(branchName);

        // Assert
        Assert.NotNull(releaseBranch);
        Assert.Equal(branchName, releaseBranch.Value);
    }

    /// <summary>
    /// 建構子應拒絕 null 值
    /// </summary>
    [Fact]
    public void Constructor_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReleaseBranchName(null!));
    }

    /// <summary>
    /// 建構子應拒絕空白字串
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespace_ShouldThrowArgumentException(string branchName)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ReleaseBranchName(branchName));
        Assert.Contains("release branch 名稱不能為空白", exception.Message);
    }

    /// <summary>
    /// 建構子應拒絕不符合 release/yyyyMMdd 格式的分支名稱
    /// </summary>
    [Theory]
    [InlineData("main")]
    [InlineData("develop")]
    [InlineData("feature/new-feature")]
    [InlineData("release/2026-01-19")] // 錯誤格式（包含破折號）
    [InlineData("release/20261340")]   // 無效月份
    [InlineData("release/20260132")]   // 無效日期
    [InlineData("release/abc")]
    [InlineData("hotfix/20260119")]
    public void Constructor_WithInvalidFormat_ShouldThrowArgumentException(string branchName)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ReleaseBranchName(branchName));
        Assert.Contains("release branch", exception.Message);
    }

    /// <summary>
    /// Validate 方法應驗證格式
    /// </summary>
    [Fact]
    public void Validate_WithValidFormat_ShouldNotThrow()
    {
        // Arrange
        var releaseBranch = new ReleaseBranchName("release/20260119");

        // Act & Assert
        releaseBranch.Validate(); // 不應拋出例外
    }

    /// <summary>
    /// GetDate 方法應正確解析日期
    /// </summary>
    [Theory]
    [InlineData("release/20260119", 2026, 1, 19)]
    [InlineData("release/20251231", 2025, 12, 31)]
    [InlineData("release/20260601", 2026, 6, 1)]
    public void GetDate_ShouldReturnCorrectDate(string branchName, int year, int month, int day)
    {
        // Arrange
        var releaseBranch = new ReleaseBranchName(branchName);

        // Act
        var date = releaseBranch.GetDate();

        // Assert
        Assert.Equal(year, date.Year);
        Assert.Equal(month, date.Month);
        Assert.Equal(day, date.Day);
    }

    /// <summary>
    /// IsNewerThan 方法應正確比較版本
    /// </summary>
    [Fact]
    public void IsNewerThan_WithOlderBranch_ShouldReturnTrue()
    {
        // Arrange
        var newerBranch = new ReleaseBranchName("release/20260120");
        var olderBranch = new ReleaseBranchName("release/20260113");

        // Act
        var result = newerBranch.IsNewerThan(olderBranch);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// IsNewerThan 方法應在版本較舊時返回 false
    /// </summary>
    [Fact]
    public void IsNewerThan_WithNewerBranch_ShouldReturnFalse()
    {
        // Arrange
        var olderBranch = new ReleaseBranchName("release/20260113");
        var newerBranch = new ReleaseBranchName("release/20260120");

        // Act
        var result = olderBranch.IsNewerThan(newerBranch);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// IsNewerThan 方法應在版本相同時返回 false
    /// </summary>
    [Fact]
    public void IsNewerThan_WithSameBranch_ShouldReturnFalse()
    {
        // Arrange
        var branch1 = new ReleaseBranchName("release/20260120");
        var branch2 = new ReleaseBranchName("release/20260120");

        // Act
        var result = branch1.IsNewerThan(branch2);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Record 類型應支援值相等性比較
    /// </summary>
    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var branch1 = new ReleaseBranchName("release/20260120");
        var branch2 = new ReleaseBranchName("release/20260120");

        // Act & Assert
        Assert.Equal(branch1, branch2);
        Assert.True(branch1 == branch2);
    }

    /// <summary>
    /// Record 類型應在值不同時不相等
    /// </summary>
    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var branch1 = new ReleaseBranchName("release/20260120");
        var branch2 = new ReleaseBranchName("release/20260113");

        // Act & Assert
        Assert.NotEqual(branch1, branch2);
        Assert.True(branch1 != branch2);
    }

    /// <summary>
    /// ToString 方法應返回分支名稱
    /// </summary>
    [Fact]
    public void ToString_ShouldReturnBranchName()
    {
        // Arrange
        var branchName = "release/20260120";
        var releaseBranch = new ReleaseBranchName(branchName);

        // Act
        var result = releaseBranch.ToString();

        // Assert
        Assert.Equal(branchName, result);
    }
}
