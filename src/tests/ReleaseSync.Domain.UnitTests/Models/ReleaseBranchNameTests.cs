using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.UnitTests.Models;

/// <summary>
/// ReleaseBranchName 值物件單元測試
/// </summary>
public class ReleaseBranchNameTests
{
    #region Parse 方法測試

    /// <summary>
    /// 測試 Parse 有效的 release branch 名稱
    /// </summary>
    [Theory]
    [InlineData("release/20260120")]
    [InlineData("release/20250101")]
    [InlineData("release/20231231")]
    public void Parse_ValidReleaseBranchName_ShouldCreateInstance(string branchName)
    {
        // Arrange & Act
        var result = ReleaseBranchName.Parse(branchName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(branchName, result.Value);
    }

    /// <summary>
    /// 測試 Parse 有效名稱後日期解析正確
    /// </summary>
    [Fact]
    public void Parse_ValidReleaseBranchName_ShouldExtractCorrectDate()
    {
        // Arrange
        var branchName = "release/20260120";

        // Act
        var result = ReleaseBranchName.Parse(branchName);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 20), result.Date);
    }

    /// <summary>
    /// 測試 Parse 無效格式 - 缺少 release/ 前綴
    /// </summary>
    [Theory]
    [InlineData("20260120")]
    [InlineData("branch/20260120")]
    [InlineData("Release/20260120")]  // 大小寫敏感
    public void Parse_InvalidPrefix_ShouldThrowFormatException(string branchName)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<FormatException>(() => ReleaseBranchName.Parse(branchName));
        Assert.Contains("release/yyyyMMdd", ex.Message);
    }

    /// <summary>
    /// 測試 Parse 無效格式 - 日期格式錯誤
    /// </summary>
    [Theory]
    [InlineData("release/2026012")]    // 日期太短
    [InlineData("release/202601200")]  // 日期太長
    [InlineData("release/2026-01-20")] // 包含分隔符
    [InlineData("release/abcdefgh")]   // 非數字
    public void Parse_InvalidDateFormat_ShouldThrowFormatException(string branchName)
    {
        // Arrange & Act & Assert
        Assert.Throws<FormatException>(() => ReleaseBranchName.Parse(branchName));
    }

    /// <summary>
    /// 測試 Parse 無效日期 - 不存在的日期
    /// </summary>
    [Theory]
    [InlineData("release/20260132")]  // 1月沒有32日
    [InlineData("release/20260229")]  // 2026年不是閏年，2月沒有29日
    [InlineData("release/20261301")]  // 沒有13月
    public void Parse_InvalidDate_ShouldThrowFormatException(string branchName)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<FormatException>(() => ReleaseBranchName.Parse(branchName));
        Assert.Contains("not a valid date", ex.Message);
    }

    /// <summary>
    /// 測試 Parse null 或空字串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrEmpty_ShouldThrowArgumentException(string? branchName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => ReleaseBranchName.Parse(branchName!));
    }

    #endregion

    #region TryParse 方法測試

    /// <summary>
    /// 測試 TryParse 有效名稱應回傳 true
    /// </summary>
    [Theory]
    [InlineData("release/20260120")]
    [InlineData("release/20250101")]
    public void TryParse_ValidName_ShouldReturnTrueAndOutputInstance(string branchName)
    {
        // Arrange & Act
        var success = ReleaseBranchName.TryParse(branchName, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(branchName, result.Value);
    }

    /// <summary>
    /// 測試 TryParse 無效名稱應回傳 false
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("release/invalid")]
    [InlineData("release/20260132")]
    [InlineData(null)]
    [InlineData("")]
    public void TryParse_InvalidName_ShouldReturnFalseAndNullOutput(string? branchName)
    {
        // Arrange & Act
        var success = ReleaseBranchName.TryParse(branchName, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    #endregion

    #region FromDate 方法測試

    /// <summary>
    /// 測試 FromDate 建立正確的 branch 名稱
    /// </summary>
    [Fact]
    public void FromDate_ShouldCreateCorrectBranchName()
    {
        // Arrange
        var date = new DateOnly(2026, 1, 20);

        // Act
        var result = ReleaseBranchName.FromDate(date);

        // Assert
        Assert.Equal("release/20260120", result.Value);
        Assert.Equal(date, result.Date);
    }

    /// <summary>
    /// 測試 FromDate 單位數月份和日期補零
    /// </summary>
    [Theory]
    [InlineData(2026, 1, 5, "release/20260105")]
    [InlineData(2026, 12, 1, "release/20261201")]
    [InlineData(2025, 8, 9, "release/20250809")]
    public void FromDate_ShouldPadZeros(int year, int month, int day, string expectedValue)
    {
        // Arrange
        var date = new DateOnly(year, month, day);

        // Act
        var result = ReleaseBranchName.FromDate(date);

        // Assert
        Assert.Equal(expectedValue, result.Value);
    }

    #endregion

    #region ShortName 屬性測試

    /// <summary>
    /// 測試 ShortName 移除 refs/heads/ 前綴
    /// </summary>
    [Fact]
    public void ShortName_WithoutPrefix_ShouldReturnSameValue()
    {
        // Arrange
        var branch = ReleaseBranchName.Parse("release/20260120");

        // Act & Assert
        Assert.Equal("release/20260120", branch.ShortName);
    }

    #endregion

    #region 比較運算測試

    /// <summary>
    /// 測試 CompareTo - 較新日期應大於較舊日期
    /// </summary>
    [Fact]
    public void CompareTo_NewerDate_ShouldBeGreaterThanOlderDate()
    {
        // Arrange
        var older = ReleaseBranchName.Parse("release/20260113");
        var newer = ReleaseBranchName.Parse("release/20260120");

        // Act
        var result = newer.CompareTo(older);

        // Assert
        Assert.True(result > 0);
    }

    /// <summary>
    /// 測試 CompareTo - 相同日期應相等
    /// </summary>
    [Fact]
    public void CompareTo_SameDate_ShouldBeEqual()
    {
        // Arrange
        var branch1 = ReleaseBranchName.Parse("release/20260120");
        var branch2 = ReleaseBranchName.Parse("release/20260120");

        // Act
        var result = branch1.CompareTo(branch2);

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// 測試 CompareTo - 較舊日期應小於較新日期
    /// </summary>
    [Fact]
    public void CompareTo_OlderDate_ShouldBeLessThanNewerDate()
    {
        // Arrange
        var older = ReleaseBranchName.Parse("release/20260113");
        var newer = ReleaseBranchName.Parse("release/20260120");

        // Act
        var result = older.CompareTo(newer);

        // Assert
        Assert.True(result < 0);
    }

    /// <summary>
    /// 測試 CompareTo null 應大於 null
    /// </summary>
    [Fact]
    public void CompareTo_Null_ShouldReturnPositive()
    {
        // Arrange
        var branch = ReleaseBranchName.Parse("release/20260120");

        // Act
        var result = branch.CompareTo(null);

        // Assert
        Assert.True(result > 0);
    }

    /// <summary>
    /// 測試小於運算子
    /// </summary>
    [Fact]
    public void LessThanOperator_OlderVsNewer_ShouldReturnTrue()
    {
        // Arrange
        var older = ReleaseBranchName.Parse("release/20260113");
        var newer = ReleaseBranchName.Parse("release/20260120");

        // Act & Assert
        Assert.True(older < newer);
        Assert.False(newer < older);
    }

    /// <summary>
    /// 測試大於運算子
    /// </summary>
    [Fact]
    public void GreaterThanOperator_NewerVsOlder_ShouldReturnTrue()
    {
        // Arrange
        var older = ReleaseBranchName.Parse("release/20260113");
        var newer = ReleaseBranchName.Parse("release/20260120");

        // Act & Assert
        Assert.True(newer > older);
        Assert.False(older > newer);
    }

    /// <summary>
    /// 測試小於等於運算子
    /// </summary>
    [Theory]
    [InlineData("release/20260113", "release/20260120", true)]  // older <= newer
    [InlineData("release/20260120", "release/20260120", true)]  // same
    [InlineData("release/20260120", "release/20260113", false)] // newer <= older
    public void LessThanOrEqualOperator_ShouldReturnCorrectResult(
        string left, string right, bool expected)
    {
        // Arrange
        var leftBranch = ReleaseBranchName.Parse(left);
        var rightBranch = ReleaseBranchName.Parse(right);

        // Act & Assert
        Assert.Equal(expected, leftBranch <= rightBranch);
    }

    /// <summary>
    /// 測試大於等於運算子
    /// </summary>
    [Theory]
    [InlineData("release/20260120", "release/20260113", true)]  // newer >= older
    [InlineData("release/20260120", "release/20260120", true)]  // same
    [InlineData("release/20260113", "release/20260120", false)] // older >= newer
    public void GreaterThanOrEqualOperator_ShouldReturnCorrectResult(
        string left, string right, bool expected)
    {
        // Arrange
        var leftBranch = ReleaseBranchName.Parse(left);
        var rightBranch = ReleaseBranchName.Parse(right);

        // Act & Assert
        Assert.Equal(expected, leftBranch >= rightBranch);
    }

    #endregion

    #region 隱式轉換測試

    /// <summary>
    /// 測試隱式轉換為字串
    /// </summary>
    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        // Arrange
        var branch = ReleaseBranchName.Parse("release/20260120");

        // Act
        string result = branch;

        // Assert
        Assert.Equal("release/20260120", result);
    }

    #endregion

    #region 相等性測試

    /// <summary>
    /// 測試相同值的 ReleaseBranchName 應相等
    /// </summary>
    [Fact]
    public void Equals_SameValue_ShouldBeEqual()
    {
        // Arrange
        var branch1 = ReleaseBranchName.Parse("release/20260120");
        var branch2 = ReleaseBranchName.Parse("release/20260120");

        // Act & Assert
        Assert.Equal(branch1, branch2);
        Assert.True(branch1 == branch2);
    }

    /// <summary>
    /// 測試不同值的 ReleaseBranchName 應不相等
    /// </summary>
    [Fact]
    public void Equals_DifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var branch1 = ReleaseBranchName.Parse("release/20260120");
        var branch2 = ReleaseBranchName.Parse("release/20260113");

        // Act & Assert
        Assert.NotEqual(branch1, branch2);
        Assert.True(branch1 != branch2);
    }

    /// <summary>
    /// 測試 GetHashCode 相同值應相同
    /// </summary>
    [Fact]
    public void GetHashCode_SameValue_ShouldBeEqual()
    {
        // Arrange
        var branch1 = ReleaseBranchName.Parse("release/20260120");
        var branch2 = ReleaseBranchName.Parse("release/20260120");

        // Act & Assert
        Assert.Equal(branch1.GetHashCode(), branch2.GetHashCode());
    }

    #endregion

    #region 排序測試

    /// <summary>
    /// 測試排序 - 應按日期升序排列
    /// </summary>
    [Fact]
    public void Sort_ShouldOrderByDateAscending()
    {
        // Arrange
        var branches = new[]
        {
            ReleaseBranchName.Parse("release/20260120"),
            ReleaseBranchName.Parse("release/20260106"),
            ReleaseBranchName.Parse("release/20260113"),
            ReleaseBranchName.Parse("release/20251230")
        };

        // Act
        var sorted = branches.Order().ToList();

        // Assert
        Assert.Equal("release/20251230", sorted[0].Value);
        Assert.Equal("release/20260106", sorted[1].Value);
        Assert.Equal("release/20260113", sorted[2].Value);
        Assert.Equal("release/20260120", sorted[3].Value);
    }

    /// <summary>
    /// 測試排序 - 應按日期降序排列
    /// </summary>
    [Fact]
    public void Sort_ShouldOrderByDateDescending()
    {
        // Arrange
        var branches = new[]
        {
            ReleaseBranchName.Parse("release/20260106"),
            ReleaseBranchName.Parse("release/20260120"),
            ReleaseBranchName.Parse("release/20260113")
        };

        // Act
        var sorted = branches.OrderDescending().ToList();

        // Assert
        Assert.Equal("release/20260120", sorted[0].Value);
        Assert.Equal("release/20260113", sorted[1].Value);
        Assert.Equal("release/20260106", sorted[2].Value);
    }

    #endregion
}
