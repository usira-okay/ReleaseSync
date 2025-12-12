// <copyright file="GoogleSheetColumnMappingTests.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using FluentAssertions;
using ReleaseSync.Application.Models;
using Xunit;

namespace ReleaseSync.Application.UnitTests.Models;

/// <summary>
/// GoogleSheetColumnMapping 單元測試。
/// </summary>
public class GoogleSheetColumnMappingTests
{
    /// <summary>
    /// 測試 MergedAtColumn 屬性預設值為 "G"。
    /// </summary>
    [Fact]
    public void MergedAtColumn_DefaultValue_ShouldBeG()
    {
        // Arrange & Act
        var mapping = new GoogleSheetColumnMapping();

        // Assert
        mapping.MergedAtColumn.Should().Be("G");
    }

    /// <summary>
    /// 測試 MergedAtColumn 屬性可自訂設定。
    /// </summary>
    [Fact]
    public void MergedAtColumn_CustomValue_ShouldBeSet()
    {
        // Arrange & Act
        var mapping = new GoogleSheetColumnMapping
        {
            MergedAtColumn = "H",
        };

        // Assert
        mapping.MergedAtColumn.Should().Be("H");
    }

    /// <summary>
    /// 測試 IsValid 方法包含 MergedAtColumn 驗證。
    /// </summary>
    [Fact]
    public void IsValid_WithValidMergedAtColumn_ShouldReturnTrue()
    {
        // Arrange
        var mapping = new GoogleSheetColumnMapping
        {
            MergedAtColumn = "G",
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 測試 IsValid 方法當 MergedAtColumn 為無效字母時返回 false。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("a")]
    [InlineData("AAA")]
    public void IsValid_WithInvalidMergedAtColumn_ShouldReturnFalse(string invalidColumn)
    {
        // Arrange
        var mapping = new GoogleSheetColumnMapping
        {
            MergedAtColumn = invalidColumn,
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 測試 IsValid 方法當 MergedAtColumn 與其他欄位重複時返回 false。
    /// </summary>
    [Fact]
    public void IsValid_WithDuplicateMergedAtColumn_ShouldReturnFalse()
    {
        // Arrange
        var mapping = new GoogleSheetColumnMapping
        {
            FeatureColumn = "B",
            MergedAtColumn = "B", // 與 FeatureColumn 重複
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        result.Should().BeFalse();
    }
}
