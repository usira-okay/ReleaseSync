// <copyright file="SheetBlockReorderOperationTests.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Application.UnitTests.Models;

using FluentAssertions;
using ReleaseSync.Application.Models;
using Xunit;

/// <summary>
/// SheetBlockReorderOperation 單元測試。
/// </summary>
public class SheetBlockReorderOperationTests
{
    #region IsValid 測試

    [Fact(DisplayName = "IsValid: 有效的操作應返回 true")]
    public void IsValid_WithValidOperation_ShouldReturnTrue()
    {
        // Arrange
        var operation = new SheetBlockReorderOperation
        {
            StartRowNumber = 3,
            EndRowNumber = 6,
            RepositoryName = "TestRepo",
            SortedOriginalRowNumbers = new List<int> { 5, 3, 4, 6 },
        };

        // Act
        var result = operation.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsValid: StartRowNumber <= 1 應返回 false")]
    public void IsValid_StartRowNumberLessThanOrEqualTo1_ShouldReturnFalse()
    {
        // Arrange
        var operation = new SheetBlockReorderOperation
        {
            StartRowNumber = 1, // 標題列
            EndRowNumber = 3,
            RepositoryName = "TestRepo",
            SortedOriginalRowNumbers = new List<int> { 1, 2, 3 },
        };

        // Act
        var result = operation.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsValid: EndRowNumber < StartRowNumber 應返回 false")]
    public void IsValid_EndRowNumberLessThanStartRowNumber_ShouldReturnFalse()
    {
        // Arrange
        var operation = new SheetBlockReorderOperation
        {
            StartRowNumber = 5,
            EndRowNumber = 3,
            RepositoryName = "TestRepo",
            SortedOriginalRowNumbers = new List<int>(),
        };

        // Act
        var result = operation.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsValid: SortedOriginalRowNumbers 數量不符應返回 false")]
    public void IsValid_SortedOriginalRowNumbersCountMismatch_ShouldReturnFalse()
    {
        // Arrange
        var operation = new SheetBlockReorderOperation
        {
            StartRowNumber = 3,
            EndRowNumber = 6, // 期望 4 個 rows
            RepositoryName = "TestRepo",
            SortedOriginalRowNumbers = new List<int> { 3, 4 }, // 只有 2 個，不符合 4 個
        };

        // Act
        var result = operation.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RowCount 測試

    [Theory(DisplayName = "RowCount: 應正確計算區塊包含的 Row 數量")]
    [InlineData(2, 2, 1)]
    [InlineData(2, 5, 4)]
    [InlineData(10, 20, 11)]
    public void RowCount_ShouldCalculateCorrectly(int startRow, int endRow, int expectedCount)
    {
        // Arrange
        var operation = new SheetBlockReorderOperation
        {
            StartRowNumber = startRow,
            EndRowNumber = endRow,
        };

        // Act
        var result = operation.RowCount;

        // Assert
        result.Should().Be(expectedCount);
    }

    #endregion

    #region 預設值測試

    [Fact(DisplayName = "預設值: RepositoryName 應為空字串")]
    public void Default_RepositoryName_ShouldBeEmptyString()
    {
        // Arrange & Act
        var operation = new SheetBlockReorderOperation();

        // Assert
        operation.RepositoryName.Should().BeEmpty();
    }

    [Fact(DisplayName = "預設值: SortedOriginalRowNumbers 應為空陣列")]
    public void Default_SortedOriginalRowNumbers_ShouldBeEmptyArray()
    {
        // Arrange & Act
        var operation = new SheetBlockReorderOperation();

        // Assert
        operation.SortedOriginalRowNumbers.Should().BeEmpty();
    }

    #endregion
}
