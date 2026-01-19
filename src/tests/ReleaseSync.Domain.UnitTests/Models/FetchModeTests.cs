namespace ReleaseSync.Domain.UnitTests.Models;

using ReleaseSync.Domain.Models;
using Xunit;

/// <summary>
/// FetchMode enum 測試
/// </summary>
public class FetchModeTests
{
    /// <summary>
    /// 驗證 FetchMode 包含 DateRange 選項
    /// </summary>
    [Fact]
    public void FetchMode_ShouldHaveDateRangeOption()
    {
        // Arrange & Act
        var fetchMode = FetchMode.DateRange;

        // Assert
        Assert.Equal(FetchMode.DateRange, fetchMode);
    }

    /// <summary>
    /// 驗證 FetchMode 包含 ReleaseBranch 選項
    /// </summary>
    [Fact]
    public void FetchMode_ShouldHaveReleaseBranchOption()
    {
        // Arrange & Act
        var fetchMode = FetchMode.ReleaseBranch;

        // Assert
        Assert.Equal(FetchMode.ReleaseBranch, fetchMode);
    }

    /// <summary>
    /// 驗證 FetchMode 的值為整數
    /// </summary>
    [Fact]
    public void FetchMode_ShouldBeIntegerEnum()
    {
        // Arrange & Act
        int dateRangeValue = (int)FetchMode.DateRange;
        int releaseBranchValue = (int)FetchMode.ReleaseBranch;

        // Assert
        Assert.True(dateRangeValue >= 0);
        Assert.True(releaseBranchValue >= 0);
        Assert.NotEqual(dateRangeValue, releaseBranchValue);
    }

    /// <summary>
    /// 驗證 FetchMode 可以從字串解析
    /// </summary>
    [Theory]
    [InlineData("DateRange", FetchMode.DateRange)]
    [InlineData("ReleaseBranch", FetchMode.ReleaseBranch)]
    public void FetchMode_ShouldParseFromString(string input, FetchMode expected)
    {
        // Arrange & Act
        var result = Enum.Parse<FetchMode>(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 驗證 FetchMode 可以轉換為字串
    /// </summary>
    [Theory]
    [InlineData(FetchMode.DateRange, "DateRange")]
    [InlineData(FetchMode.ReleaseBranch, "ReleaseBranch")]
    public void FetchMode_ShouldConvertToString(FetchMode mode, string expected)
    {
        // Arrange & Act
        var result = mode.ToString();

        // Assert
        Assert.Equal(expected, result);
    }
}
