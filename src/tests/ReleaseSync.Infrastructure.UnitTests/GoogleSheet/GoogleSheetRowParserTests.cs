// <copyright file="GoogleSheetRowParserTests.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Options;
using ReleaseSync.Application.Configuration;
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
        // 預設使用 UTC+8 時區
        var settings = Options.Create(new GoogleSheetSettings { DisplayTimeZoneOffset = 8 });
        _parser = new GoogleSheetRowParser(settings);
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
    /// 測試 ParseRow 可正確解析 MergedAt 欄位，並將本地時間轉換為 UTC。
    /// Sheet 中儲存的是 UTC+8 本地時間 10:30，解析後應轉為 UTC 02:30。
    /// </summary>
    [Fact]
    public void ParseRow_WithValidMergedAt_ShouldConvertToUtc()
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
            "2025-01-15 10:30:00", // G: MergedAt (本地時間 UTC+8)
            "TRUE",                // H: AutoSync
        };

        // Act
        var result = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert
        // 本地時間 10:30 (UTC+8) 應轉換為 UTC 02:30
        result.MergedAt.Should().NotBeNull();
        result.MergedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.MergedAt!.Value.Year.Should().Be(2025);
        result.MergedAt!.Value.Month.Should().Be(1);
        result.MergedAt!.Value.Day.Should().Be(15);
        result.MergedAt!.Value.Hour.Should().Be(2);
        result.MergedAt!.Value.Minute.Should().Be(30);
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
    /// 測試 ToRowValues 可正確輸出 MergedAt 欄位，並轉換為 UTC+8 時區。
    /// 格式為 "yyyy-MM-dd (週) HH:mm"，例如 "2025-12-12 (五) 13:30"。
    /// </summary>
    [Fact]
    public void ToRowValues_WithMergedAt_ShouldOutputWithTimeZoneConversion()
    {
        // Arrange
        // UTC 時間 2025-01-15 02:30 轉換為 UTC+8 後為 2025-01-15 10:30 (星期三)
        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = new DateTime(2025, 1, 15, 2, 30, 0, DateTimeKind.Utc),
            IsAutoSync = true,
        };

        // Act
        var result = _parser.ToRowValues(rowData, _columnMapping);

        // Assert
        // G 欄是索引 6
        // UTC 02:30 + 8 小時 = 10:30
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
    /// 測試 ParseRow 可解析多種日期格式，並轉換為 UTC。
    /// 注意：日期時間會被視為本地時間 (UTC+8)，並轉換為 UTC。
    /// </summary>
    [Theory]
    [InlineData("2025-01-15 10:30:00", 2025, 1, 15, 2, 30)]  // 本地 10:30 → UTC 02:30
    [InlineData("2025-01-15T10:30:00", 2025, 1, 15, 2, 30)]  // 本地 10:30 → UTC 02:30
    [InlineData("2025-01-15 (三) 10:30", 2025, 1, 15, 2, 30)] // 本程式輸出格式
    public void ParseRow_WithVariousDateFormats_ShouldParseAndConvertToUtc(
        string dateString,
        int expectedYear,
        int expectedMonth,
        int expectedDay,
        int expectedHour,
        int expectedMinute)
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
            dateString,            // G: MergedAt (本地時間)
            "TRUE",                // H: AutoSync
        };

        // Act
        var result = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert
        result.MergedAt.Should().NotBeNull();
        result.MergedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.MergedAt!.Value.Year.Should().Be(expectedYear);
        result.MergedAt!.Value.Month.Should().Be(expectedMonth);
        result.MergedAt!.Value.Day.Should().Be(expectedDay);
        result.MergedAt!.Value.Hour.Should().Be(expectedHour);
        result.MergedAt!.Value.Minute.Should().Be(expectedMinute);
    }

    /// <summary>
    /// 測試時區轉換：UTC 跨日情況 (UTC 20:00 + 8 小時 = 次日 04:00)。
    /// </summary>
    [Fact]
    public void ToRowValues_WithMergedAtCrossingMidnight_ShouldConvertToNextDay()
    {
        // Arrange
        var settings = Options.Create(new GoogleSheetSettings { DisplayTimeZoneOffset = 8 });
        var parser = new GoogleSheetRowParser(settings);

        // UTC 2025-01-15 20:00 轉換為 UTC+8 後為 2025-01-16 04:00 (星期四)
        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = new DateTime(2025, 1, 15, 20, 0, 0, DateTimeKind.Utc),
            IsAutoSync = true,
        };

        // Act
        var result = parser.ToRowValues(rowData, _columnMapping);

        // Assert
        result[6].Should().Be("2025-01-16 (四) 04:00");
    }

    /// <summary>
    /// 測試使用 UTC (DisplayTimeZoneOffset = 0) 時區。
    /// </summary>
    [Fact]
    public void ToRowValues_WithUtcTimeZone_ShouldOutputUtcTime()
    {
        // Arrange
        var settings = Options.Create(new GoogleSheetSettings { DisplayTimeZoneOffset = 0 });
        var parser = new GoogleSheetRowParser(settings);

        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            IsAutoSync = true,
        };

        // Act
        var result = parser.ToRowValues(rowData, _columnMapping);

        // Assert
        // UTC 時區偏移為 0，時間不變
        result[6].Should().Be("2025-01-15 (三) 10:30");
    }

    /// <summary>
    /// 測試使用負時區 (如美西 UTC-8)。
    /// </summary>
    [Fact]
    public void ToRowValues_WithNegativeTimeZone_ShouldConvertCorrectly()
    {
        // Arrange
        var settings = Options.Create(new GoogleSheetSettings { DisplayTimeZoneOffset = -8 });
        var parser = new GoogleSheetRowParser(settings);

        // UTC 2025-01-15 10:30 轉換為 UTC-8 後為 2025-01-15 02:30 (星期三)
        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            IsAutoSync = true,
        };

        // Act
        var result = parser.ToRowValues(rowData, _columnMapping);

        // Assert
        // UTC 10:30 - 8 小時 = 02:30
        result[6].Should().Be("2025-01-15 (三) 02:30");
    }

    /// <summary>
    /// 測試讀寫往返 (round-trip)：寫入後讀取應得到相同的 UTC 時間。
    /// </summary>
    [Fact]
    public void RoundTrip_WriteAndRead_ShouldPreserveUtcTime()
    {
        // Arrange
        var originalUtcTime = new DateTime(2025, 1, 15, 2, 30, 0, DateTimeKind.Utc);
        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = originalUtcTime,
            IsAutoSync = true,
        };

        // Act - 寫入 (UTC → 本地時間字串)
        var rowValues = _parser.ToRowValues(rowData, _columnMapping);

        // Act - 讀取 (本地時間字串 → UTC)
        var parsedRowData = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert - 讀取後的 UTC 時間應與原始相同
        parsedRowData.MergedAt.Should().NotBeNull();
        parsedRowData.MergedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        parsedRowData.MergedAt!.Value.Should().Be(originalUtcTime);
    }

    /// <summary>
    /// 測試跨日情況的讀寫往返。
    /// UTC 2025-01-15 20:00 → 本地 2025-01-16 04:00 (UTC+8) → 讀回 UTC 2025-01-15 20:00。
    /// </summary>
    [Fact]
    public void RoundTrip_CrossingMidnight_ShouldPreserveUtcTime()
    {
        // Arrange
        var originalUtcTime = new DateTime(2025, 1, 15, 20, 0, 0, DateTimeKind.Utc);
        var rowData = new SheetRowData
        {
            RowNumber = 1,
            RepositoryName = "repo-name",
            Feature = "VSTS123 - Feature",
            Team = "Team A",
            Authors = new HashSet<string> { "John Doe" },
            PullRequestUrls = new HashSet<string> { "https://pr.url" },
            UniqueKey = "123repo-name",
            MergedAt = originalUtcTime,
            IsAutoSync = true,
        };

        // Act - 寫入
        var rowValues = _parser.ToRowValues(rowData, _columnMapping);

        // 驗證寫入的是本地時間 (2025-01-16 04:00)
        rowValues[6].Should().Be("2025-01-16 (四) 04:00");

        // Act - 讀取
        var parsedRowData = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert - 讀取後的 UTC 時間應與原始相同
        parsedRowData.MergedAt.Should().NotBeNull();
        parsedRowData.MergedAt!.Value.Should().Be(originalUtcTime);
    }

    /// <summary>
    /// 測試時間比較：從 Sheet 讀取的時間與 API 取得的 UTC 時間比較應正確。
    /// </summary>
    [Fact]
    public void ParseRow_ComparedWithApiTime_ShouldBeComparable()
    {
        // Arrange
        // 模擬 Sheet 中儲存的本地時間 (UTC+8)
        var rowValues = new List<object>
        {
            "repo-name",
            "VSTS123 - Feature",
            "Team A",
            "John Doe",
            "https://pr.url",
            "123repo-name",
            "2025-01-15 10:30:00", // 本地時間 10:30 (UTC+8)
            "TRUE",
        };

        // 模擬從 API 取得的 UTC 時間
        var apiUtcTime = new DateTime(2025, 1, 15, 2, 30, 0, DateTimeKind.Utc);

        // Act
        var parsedRowData = _parser.ParseRow(rowValues, 1, _columnMapping);

        // Assert - 解析後的時間應與 API UTC 時間相同
        parsedRowData.MergedAt.Should().NotBeNull();
        parsedRowData.MergedAt!.Value.Should().Be(apiUtcTime);
    }
}
