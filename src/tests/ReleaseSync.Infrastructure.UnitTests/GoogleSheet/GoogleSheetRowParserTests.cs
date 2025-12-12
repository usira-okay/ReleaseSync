// <copyright file="GoogleSheetRowParserTests.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using FluentAssertions;
using ReleaseSync.Application.Models;
using ReleaseSync.Infrastructure.GoogleSheet;
using Xunit;

namespace ReleaseSync.Infrastructure.UnitTests.GoogleSheet;

/// <summary>
/// GoogleSheetRowParser 單元測試 - MergedAt 欄位相關。
/// </summary>
public class GoogleSheetRowParserTests
{
    private readonly GoogleSheetRowParser _parser;
    private readonly GoogleSheetColumnMapping _columnMapping;

    /// <summary>
    /// 初始化測試類別。
    /// </summary>
    public GoogleSheetRowParserTests()
    {
        _parser = new GoogleSheetRowParser();
        _columnMapping = new GoogleSheetColumnMapping
        {
            RepositoryNameColumn = "A",
            FeatureColumn = "B",
            TeamColumn = "C",
            AuthorsColumn = "D",
            PullRequestUrlsColumn = "E",
            UniqueKeyColumn = "F",
            AutoSyncColumn = "H",
            MergedAtColumn = "G",
        };
    }

    /// <summary>
    /// 測試 ParseRow 可正確解析 MergedAt 欄位 (ISO 8601 格式)。
    /// </summary>
    [Fact]
    public void ParseRow_WithValidMergedAt_ShouldParseCorrectly()
    {
        // Arrange
        var rowValues = new List<object>
        {
            "repo-name",           // A: RepositoryName
            "VSTS123 - Feature",   // B: Feature
            "Team A",              // C: Team
            "John Doe",            // D: Authors
            "https://pr.url",      // E: PullRequestUrls
            "123repo-name",        // F: UniqueKey
            "2025-01-15 10:30:00", // G: MergedAt
            "TRUE",                // H: AutoSync
        };

        // Act
        var result = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert
        result.MergedAt.Should().NotBeNull();
        result.MergedAt!.Value.Year.Should().Be(2025);
        result.MergedAt!.Value.Month.Should().Be(1);
        result.MergedAt!.Value.Day.Should().Be(15);
    }

    /// <summary>
    /// 測試 ParseRow 當 MergedAt 欄位為空時返回 null。
    /// </summary>
    [Fact]
    public void ParseRow_WithEmptyMergedAt_ShouldReturnNull()
    {
        // Arrange
        var rowValues = new List<object>
        {
            "repo-name",           // A: RepositoryName
            "VSTS123 - Feature",   // B: Feature
            "Team A",              // C: Team
            "John Doe",            // D: Authors
            "https://pr.url",      // E: PullRequestUrls
            "123repo-name",        // F: UniqueKey
            "",                    // G: MergedAt (空)
            "TRUE",                // H: AutoSync
        };

        // Act
        var result = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert
        result.MergedAt.Should().BeNull();
    }

    /// <summary>
    /// 測試 ToRowValues 可正確輸出 MergedAt 欄位。
    /// 格式為 "yyyy-MM-dd (週) HH:mm"，例如 "2025-12-12 (五) 13:30"。
    /// </summary>
    [Fact]
    public void ToRowValues_WithMergedAt_ShouldOutputCorrectly()
    {
        // Arrange
        // 2025-01-15 是星期三
        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = new DateTime(2025, 1, 15, 10, 30, 0),
            IsAutoSync = true,
        };

        // Act
        var result = _parser.ToRowValues(rowData, _columnMapping);

        // Assert
        // G 欄是索引 6
        result[6].Should().Be("2025-01-15 (三) 10:30");
    }

    /// <summary>
    /// 測試 ToRowValues 當 MergedAt 為 null 時輸出空字串。
    /// </summary>
    [Fact]
    public void ToRowValues_WithNullMergedAt_ShouldOutputEmptyString()
    {
        // Arrange
        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = null,
            IsAutoSync = true,
        };

        // Act
        var result = _parser.ToRowValues(rowData, _columnMapping);

        // Assert
        // G 欄是索引 6
        result[6].Should().Be(string.Empty);
    }

    /// <summary>
    /// 測試 ParseRow 可解析多種日期格式。
    /// </summary>
    [Theory]
    [InlineData("2025-01-15")]
    [InlineData("2025/01/15")]
    [InlineData("2025-01-15T10:30:00")]
    [InlineData("2025-01-15 10:30:00")]
    public void ParseRow_WithVariousDateFormats_ShouldParseCorrectly(string dateString)
    {
        // Arrange
        var rowValues = new List<object>
        {
            "repo-name",           // A: RepositoryName
            "VSTS123 - Feature",   // B: Feature
            "Team A",              // C: Team
            "John Doe",            // D: Authors
            "https://pr.url",      // E: PullRequestUrls
            "123repo-name",        // F: UniqueKey
            dateString,            // G: MergedAt
            "TRUE",                // H: AutoSync
        };

        // Act
        var result = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert
        result.MergedAt.Should().NotBeNull();
        result.MergedAt!.Value.Year.Should().Be(2025);
        result.MergedAt!.Value.Month.Should().Be(1);
        result.MergedAt!.Value.Day.Should().Be(15);
    }
}
