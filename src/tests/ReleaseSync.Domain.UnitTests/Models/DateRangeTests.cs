using FluentAssertions;
using ReleaseSync.Domain.Models;

namespace ReleaseSync.Domain.UnitTests.Models;

/// <summary>
/// DateRange 值物件單元測試
/// </summary>
public class DateRangeTests
{
    [Fact]
    public void Constructor_WithValidDates_ShouldCreateInstance()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var dateRange = new DateRange(startDate, endDate);

        // Assert
        dateRange.StartDate.Should().Be(startDate);
        dateRange.EndDate.Should().Be(endDate);
    }

    [Fact]
    public void Validate_WhenStartDateAfterEndDate_ShouldThrowException()
    {
        // Arrange
        var startDate = new DateTime(2025, 2, 1);
        var endDate = new DateTime(2025, 1, 1);
        var dateRange = new DateRange(startDate, endDate);

        // Act
        var act = () => dateRange.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*起始日期*不能晚於結束日期*");
    }

    [Fact]
    public void Validate_WhenStartDateEqualsEndDate_ShouldNotThrow()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15);
        var dateRange = new DateRange(date, date);

        // Act
        var act = () => dateRange.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenStartDateBeforeEndDate_ShouldNotThrow()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var dateRange = new DateRange(startDate, endDate);

        // Act
        var act = () => dateRange.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("2025-01-15", true)]
    [InlineData("2025-01-01", true)]
    [InlineData("2025-01-31", true)]
    [InlineData("2024-12-31", false)]
    [InlineData("2025-02-01", false)]
    public void Contains_ShouldReturnCorrectResult(string dateString, bool expected)
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var dateRange = new DateRange(startDate, endDate);
        var date = DateTime.Parse(dateString);

        // Act
        var result = dateRange.Contains(date);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void LastDays_ShouldCreateCorrectDateRange()
    {
        // Arrange
        var days = 7;

        // Act
        var dateRange = DateRange.LastDays(days);

        // Assert
        dateRange.StartDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(-days), TimeSpan.FromSeconds(1));
        dateRange.EndDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dateRange.StartDate.Should().BeBefore(dateRange.EndDate);
    }

    [Fact]
    public void LastDays_WithZeroDays_ShouldCreateSameDayRange()
    {
        // Act
        var dateRange = DateRange.LastDays(0);

        // Assert
        dateRange.StartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dateRange.EndDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void LastDays_WithNegativeDays_ShouldCreateFutureRange()
    {
        // Arrange
        var days = -7;

        // Act
        var dateRange = DateRange.LastDays(days);

        // Assert
        dateRange.StartDate.Should().BeAfter(DateTime.UtcNow);
        dateRange.EndDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordType_ShouldSupportValueEquality()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var dateRange1 = new DateRange(startDate, endDate);
        var dateRange2 = new DateRange(startDate, endDate);
        var dateRange3 = new DateRange(startDate, endDate.AddDays(1));

        // Act & Assert
        dateRange1.Should().Be(dateRange2);
        dateRange1.Should().NotBe(dateRange3);
    }

    [Fact]
    public void RecordType_ShouldSupportWithExpression()
    {
        // Arrange
        var original = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

        // Act
        var modified = original with { EndDate = new DateTime(2025, 2, 28) };

        // Assert
        modified.StartDate.Should().Be(original.StartDate);
        modified.EndDate.Should().Be(new DateTime(2025, 2, 28));
        modified.Should().NotBe(original);
    }
}
