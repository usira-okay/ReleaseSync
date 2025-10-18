using FluentAssertions;
using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.UnitTests.Models;

/// <summary>
/// BranchName 值物件單元測試
/// </summary>
public class BranchNameTests
{
    [Theory]
    [InlineData("feature/add-authentication")]
    [InlineData("main")]
    [InlineData("develop")]
    [InlineData("bugfix/fix-login-issue")]
    [InlineData("release/v1.0.0")]
    public void Constructor_WithValidBranchName_ShouldCreateInstance(string branchName)
    {
        // Act
        var result = new BranchName(branchName);

        // Assert
        result.Value.Should().Be(branchName);
    }

    [Fact]
    public void Constructor_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new BranchName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("Value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespace_ShouldThrowException(string branchName)
    {
        // Arrange
        var branch = new BranchName(branchName);

        // Act
        var act = () => branch.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*分支名稱不能為空白*");
    }

    [Theory]
    [InlineData("feature..bug")]
    [InlineData("/feature/test")]
    [InlineData("feature/test/")]
    public void Validate_WithInvalidGitBranchName_ShouldThrowException(string branchName)
    {
        // Arrange
        var branch = new BranchName(branchName);

        // Act
        var act = () => branch.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*無效的分支名稱格式*");
    }

    [Theory]
    [InlineData("feature/add-auth")]
    [InlineData("main")]
    [InlineData("bugfix/issue-123")]
    public void Validate_WithValidBranchName_ShouldNotThrow(string branchName)
    {
        // Arrange
        var branch = new BranchName(branchName);

        // Act
        var act = () => branch.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("refs/heads/feature/add-auth", "feature/add-auth")]
    [InlineData("refs/heads/main", "main")]
    [InlineData("refs/heads/bugfix/issue-123", "bugfix/issue-123")]
    public void ShortName_WithRefsHeadsPrefix_ShouldReturnShortName(string fullName, string expectedShortName)
    {
        // Arrange
        var branch = new BranchName(fullName);

        // Act
        var shortName = branch.ShortName;

        // Assert
        shortName.Should().Be(expectedShortName);
    }

    [Theory]
    [InlineData("feature/add-auth")]
    [InlineData("main")]
    public void ShortName_WithoutRefsHeadsPrefix_ShouldReturnOriginalValue(string branchName)
    {
        // Arrange
        var branch = new BranchName(branchName);

        // Act
        var shortName = branch.ShortName;

        // Assert
        shortName.Should().Be(branchName);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var branchName = "feature/add-auth";
        var branch = new BranchName(branchName);

        // Act
        var result = branch.ToString();

        // Assert
        result.Should().Be(branchName);
    }

    [Fact]
    public void RecordType_ShouldSupportValueEquality()
    {
        // Arrange
        var branch1 = new BranchName("feature/add-auth");
        var branch2 = new BranchName("feature/add-auth");
        var branch3 = new BranchName("main");

        // Act & Assert
        branch1.Should().Be(branch2);
        branch1.Should().NotBe(branch3);
    }

    [Fact]
    public void RecordType_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new BranchName("feature/add-auth");

        // Act
        var modified = original with { Value = "main" };

        // Assert
        modified.Value.Should().Be("main");
        modified.Should().NotBe(original);
    }
}
