using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.UnitTests.Models;

/// <summary>
/// FetchMode 列舉單元測試
/// </summary>
public class FetchModeTests
{
    /// <summary>
    /// 測試 DateRange 為預設值 (0)
    /// </summary>
    [Fact]
    public void DateRange_ShouldBeDefaultValue()
    {
        // Arrange & Act
        var defaultValue = default(FetchMode);

        // Assert
        Assert.Equal(FetchMode.DateRange, defaultValue);
    }

    /// <summary>
    /// 測試 DateRange 的數值為 0
    /// </summary>
    [Fact]
    public void DateRange_ShouldHaveValueZero()
    {
        // Arrange & Act
        var value = (int)FetchMode.DateRange;

        // Assert
        Assert.Equal(0, value);
    }

    /// <summary>
    /// 測試 ReleaseBranch 的數值為 1
    /// </summary>
    [Fact]
    public void ReleaseBranch_ShouldHaveValueOne()
    {
        // Arrange & Act
        var value = (int)FetchMode.ReleaseBranch;

        // Assert
        Assert.Equal(1, value);
    }

    /// <summary>
    /// 測試列舉只有兩個值
    /// </summary>
    [Fact]
    public void FetchMode_ShouldHaveExactlyTwoValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<FetchMode>();

        // Assert
        Assert.Equal(2, values.Length);
    }

    /// <summary>
    /// 測試字串轉換 - DateRange
    /// </summary>
    [Fact]
    public void DateRange_ToString_ShouldReturnCorrectString()
    {
        // Arrange & Act
        var result = FetchMode.DateRange.ToString();

        // Assert
        Assert.Equal("DateRange", result);
    }

    /// <summary>
    /// 測試字串轉換 - ReleaseBranch
    /// </summary>
    [Fact]
    public void ReleaseBranch_ToString_ShouldReturnCorrectString()
    {
        // Arrange & Act
        var result = FetchMode.ReleaseBranch.ToString();

        // Assert
        Assert.Equal("ReleaseBranch", result);
    }

    /// <summary>
    /// 測試從字串解析 - DateRange
    /// </summary>
    [Fact]
    public void Parse_DateRangeString_ShouldReturnDateRangeEnum()
    {
        // Arrange & Act
        var result = Enum.Parse<FetchMode>("DateRange");

        // Assert
        Assert.Equal(FetchMode.DateRange, result);
    }

    /// <summary>
    /// 測試從字串解析 - ReleaseBranch
    /// </summary>
    [Fact]
    public void Parse_ReleaseBranchString_ShouldReturnReleaseBranchEnum()
    {
        // Arrange & Act
        var result = Enum.Parse<FetchMode>("ReleaseBranch");

        // Assert
        Assert.Equal(FetchMode.ReleaseBranch, result);
    }

    /// <summary>
    /// 測試從數值轉換
    /// </summary>
    [Theory]
    [InlineData(0, FetchMode.DateRange)]
    [InlineData(1, FetchMode.ReleaseBranch)]
    public void CastFromInt_ShouldReturnCorrectEnum(int value, FetchMode expected)
    {
        // Arrange & Act
        var result = (FetchMode)value;

        // Assert
        Assert.Equal(expected, result);
    }
}
