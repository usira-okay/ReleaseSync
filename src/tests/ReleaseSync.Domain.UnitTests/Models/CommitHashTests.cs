namespace ReleaseSync.Domain.UnitTests.Models;

using ReleaseSync.Domain.Models;
using Xunit;

/// <summary>
/// CommitHash value object 測試
/// </summary>
public class CommitHashTests
{
    /// <summary>
    /// 建構子應接受有效的 commit hash（完整格式）
    /// </summary>
    [Theory]
    [InlineData("a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0")]
    [InlineData("1234567890abcdef1234567890abcdef12345678")]
    [InlineData("ABCDEF1234567890abcdef1234567890ABCDEF12")]
    public void Constructor_WithValidFullHash_ShouldCreateInstance(string hash)
    {
        // Arrange & Act
        var commitHash = new CommitHash(hash);

        // Assert
        Assert.NotNull(commitHash);
        Assert.Equal(hash.ToLowerInvariant(), commitHash.Value);
    }

    /// <summary>
    /// 建構子應接受有效的 commit hash（短格式，7 字元）
    /// </summary>
    [Theory]
    [InlineData("a1b2c3d")]
    [InlineData("1234567")]
    [InlineData("ABCDEF1")]
    public void Constructor_WithValidShortHash_ShouldCreateInstance(string hash)
    {
        // Arrange & Act
        var commitHash = new CommitHash(hash);

        // Assert
        Assert.NotNull(commitHash);
        Assert.Equal(hash.ToLowerInvariant(), commitHash.Value);
    }

    /// <summary>
    /// 建構子應拒絕 null 值
    /// </summary>
    [Fact]
    public void Constructor_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CommitHash(null!));
    }

    /// <summary>
    /// 建構子應拒絕空白字串
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespace_ShouldThrowArgumentException(string hash)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new CommitHash(hash));
        Assert.Contains("Commit hash 不能為空白", exception.Message);
    }

    /// <summary>
    /// 建構子應拒絕無效長度的 hash（不是 7 或 40 字元）
    /// </summary>
    [Theory]
    [InlineData("a1b")]           // 3 字元
    [InlineData("a1b2c")]         // 5 字元
    [InlineData("a1b2c3d4")]      // 8 字元
    [InlineData("a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0123")] // 43 字元
    public void Constructor_WithInvalidLength_ShouldThrowArgumentException(string hash)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new CommitHash(hash));
        Assert.Contains("Commit hash", exception.Message);
    }

    /// <summary>
    /// 建構子應拒絕包含非十六進位字元的 hash
    /// </summary>
    [Theory]
    [InlineData("g123456")]                                      // 包含 'g'
    [InlineData("xyz1234")]                                      // 包含 'xyz'
    [InlineData("1234567890abcdef1234567890abcdef1234567g")]    // 完整長度但包含非十六進位字元
    public void Constructor_WithNonHexCharacters_ShouldThrowArgumentException(string hash)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new CommitHash(hash));
        Assert.Contains("Commit hash", exception.Message);
    }

    /// <summary>
    /// Validate 方法應驗證格式
    /// </summary>
    [Fact]
    public void Validate_WithValidHash_ShouldNotThrow()
    {
        // Arrange
        var commitHash = new CommitHash("a1b2c3d");

        // Act & Assert
        commitHash.Validate(); // 不應拋出例外
    }

    /// <summary>
    /// ShortHash 屬性應返回前 7 個字元
    /// </summary>
    [Fact]
    public void ShortHash_WithFullHash_ShouldReturnFirst7Characters()
    {
        // Arrange
        var fullHash = "1234567890abcdef1234567890abcdef12345678";
        var commitHash = new CommitHash(fullHash);

        // Act
        var shortHash = commitHash.ShortHash;

        // Assert
        Assert.Equal("1234567", shortHash);
    }

    /// <summary>
    /// ShortHash 屬性應在短 hash 時返回原值
    /// </summary>
    [Fact]
    public void ShortHash_WithShortHash_ShouldReturnOriginalValue()
    {
        // Arrange
        var shortHash = "a1b2c3d";
        var commitHash = new CommitHash(shortHash);

        // Act
        var result = commitHash.ShortHash;

        // Assert
        Assert.Equal(shortHash, result);
    }

    /// <summary>
    /// Record 類型應支援值相等性比較
    /// </summary>
    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var hash1 = new CommitHash("a1b2c3d");
        var hash2 = new CommitHash("A1B2C3D"); // 大小寫不同

        // Act & Assert
        Assert.Equal(hash1, hash2); // 應該相等（忽略大小寫）
        Assert.True(hash1 == hash2);
    }

    /// <summary>
    /// Record 類型應在值不同時不相等
    /// </summary>
    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var hash1 = new CommitHash("a1b2c3d");
        var hash2 = new CommitHash("1234567");

        // Act & Assert
        Assert.NotEqual(hash1, hash2);
        Assert.True(hash1 != hash2);
    }

    /// <summary>
    /// ToString 方法應返回 hash 值
    /// </summary>
    [Fact]
    public void ToString_ShouldReturnHashValue()
    {
        // Arrange
        var hash = "a1b2c3d";
        var commitHash = new CommitHash(hash);

        // Act
        var result = commitHash.ToString();

        // Assert
        Assert.Equal(hash, result);
    }

    /// <summary>
    /// 建構子應將 hash 轉換為小寫
    /// </summary>
    [Fact]
    public void Constructor_ShouldConvertToLowerCase()
    {
        // Arrange & Act
        var commitHash = new CommitHash("ABCDEF1");

        // Assert
        Assert.Equal("abcdef1", commitHash.Value);
    }
}
