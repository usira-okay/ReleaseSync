using FluentAssertions;
using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.UnitTests.Models;

/// <summary>
/// WorkItemId 值物件單元測試
/// </summary>
public class WorkItemIdTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(12345)]
    [InlineData(999999)]
    public void Constructor_WithPositiveInteger_ShouldCreateInstance(int value)
    {
        // Act
        var workItemId = new WorkItemId(value);

        // Assert
        workItemId.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithZeroOrNegative_ShouldThrowException(int value)
    {
        // Arrange
        var workItemId = new WorkItemId(value);

        // Act
        var act = () => workItemId.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Work Item ID 必須為正整數*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(12345)]
    public void Validate_WithPositiveInteger_ShouldNotThrow(int value)
    {
        // Arrange
        var workItemId = new WorkItemId(value);

        // Act
        var act = () => workItemId.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("1", 1)]
    [InlineData("999999", 999999)]
    public void Parse_WithValidString_ShouldReturnWorkItemId(string value, int expectedId)
    {
        // Act
        var workItemId = WorkItemId.Parse(value);

        // Assert
        workItemId.Value.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12.34")]
    [InlineData("12abc")]
    [InlineData("  ")]
    public void Parse_WithInvalidString_ShouldThrowFormatException(string value)
    {
        // Act
        var act = () => WorkItemId.Parse(value);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage($"*無法將 '{value}' 解析為 Work Item ID*");
    }

    [Theory]
    [InlineData("123", true, 123)]
    [InlineData("1", true, 1)]
    [InlineData("999999", true, 999999)]
    public void TryParse_WithValidString_ShouldReturnTrueAndWorkItemId(string value, bool expectedResult, int expectedId)
    {
        // Act
        var result = WorkItemId.TryParse(value, out var workItemId);

        // Assert
        result.Should().Be(expectedResult);
        workItemId.Should().NotBeNull();
        workItemId!.Value.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12.34")]
    [InlineData("0")]
    [InlineData("-1")]
    public void TryParse_WithInvalidString_ShouldReturnFalseAndNull(string value)
    {
        // Act
        var result = WorkItemId.TryParse(value, out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }

    [Fact]
    public void ToString_ShouldReturnValueAsString()
    {
        // Arrange
        var workItemId = new WorkItemId(12345);

        // Act
        var result = workItemId.ToString();

        // Assert
        result.Should().Be("12345");
    }

    [Fact]
    public void RecordType_ShouldSupportValueEquality()
    {
        // Arrange
        var workItemId1 = new WorkItemId(123);
        var workItemId2 = new WorkItemId(123);
        var workItemId3 = new WorkItemId(456);

        // Act & Assert
        workItemId1.Should().Be(workItemId2);
        workItemId1.Should().NotBe(workItemId3);
    }

    [Fact]
    public void RecordType_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new WorkItemId(123);

        // Act
        var modified = original with { Value = 456 };

        // Assert
        modified.Value.Should().Be(456);
        modified.Should().NotBe(original);
    }

    [Fact]
    public void TryParse_WithZeroValue_ShouldReturnFalse()
    {
        // Act
        var result = WorkItemId.TryParse("0", out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }
}
