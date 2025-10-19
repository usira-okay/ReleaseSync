using System.Text.Json;
using ReleaseSync.Integration.Tests.Helpers;
using FluentAssertions;

namespace ReleaseSync.Integration.Tests.Exporters;

/// <summary>
/// JSON 輸出格式驗證整合測試
/// </summary>
public class JsonExportValidationTests
{
    /// <summary>
    /// 測試匯出的 JSON 應符合預期的 Schema 結構
    /// </summary>
    [Fact]
    public async Task Should_Export_Json_With_Correct_Schema()
    {
        // Arrange
        var syncResult = TestHelper.CreateSampleSyncResultDto();
        var exporter = TestHelper.CreateJsonFileExporter();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await exporter.ExportAsync(syncResult, tempFilePath, overwrite: true);

            // Assert
            File.Exists(tempFilePath).Should().BeTrue();

            var jsonContent = await File.ReadAllTextAsync(tempFilePath);
            jsonContent.Should().NotBeNullOrEmpty();

            // 驗證 JSON 可以正確反序列化
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var root = jsonDocument.RootElement;

            // 驗證頂層結構 (camelCase 命名)
            root.TryGetProperty("syncStartedAt", out _).Should().BeTrue();
            root.TryGetProperty("syncCompletedAt", out _).Should().BeTrue();
            root.TryGetProperty("startDate", out _).Should().BeTrue();
            root.TryGetProperty("endDate", out _).Should().BeTrue();
            root.TryGetProperty("isFullySuccessful", out _).Should().BeTrue();
            root.TryGetProperty("isPartiallySuccessful", out _).Should().BeTrue();
            root.TryGetProperty("totalPullRequestCount", out _).Should().BeTrue();
            root.TryGetProperty("linkedWorkItemCount", out _).Should().BeTrue();
            root.TryGetProperty("pullRequests", out _).Should().BeTrue();
            root.TryGetProperty("platformStatuses", out _).Should().BeTrue();

            // 驗證 PlatformStatuses 結構
            var platformStatuses = root.GetProperty("platformStatuses");
            platformStatuses.GetArrayLength().Should().BeGreaterThan(0);

            var firstStatus = platformStatuses[0];
            firstStatus.TryGetProperty("platformName", out _).Should().BeTrue();
            firstStatus.TryGetProperty("isSuccess", out _).Should().BeTrue();
            firstStatus.TryGetProperty("pullRequestCount", out _).Should().BeTrue();
            firstStatus.TryGetProperty("elapsedMilliseconds", out _).Should().BeTrue();

            // 驗證 PullRequests 結構
            var pullRequests = root.GetProperty("pullRequests");
            pullRequests.GetArrayLength().Should().BeGreaterThan(0);

            var firstPR = pullRequests[0];
            firstPR.TryGetProperty("platform", out _).Should().BeTrue();
            firstPR.TryGetProperty("number", out _).Should().BeTrue();
            firstPR.TryGetProperty("title", out _).Should().BeTrue();
            firstPR.TryGetProperty("sourceBranch", out _).Should().BeTrue();
            firstPR.TryGetProperty("targetBranch", out _).Should().BeTrue();
            firstPR.TryGetProperty("createdAt", out _).Should().BeTrue();
            firstPR.TryGetProperty("state", out _).Should().BeTrue();
            firstPR.TryGetProperty("authorUsername", out _).Should().BeTrue();
            firstPR.TryGetProperty("repositoryName", out _).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 測試匯出的 JSON 應使用 UTF-8 編碼
    /// </summary>
    [Fact]
    public async Task Should_Export_Json_With_UTF8_Encoding()
    {
        // Arrange
        var syncResult = TestHelper.CreateSyncResultDtoWithChineseContent();
        var exporter = TestHelper.CreateJsonFileExporter();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await exporter.ExportAsync(syncResult, tempFilePath, overwrite: true);

            // Assert
            var jsonContent = await File.ReadAllTextAsync(tempFilePath);

            // 驗證 JSON 可正確解析中文字元（JSON 可能使用 Unicode escape 序列）
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var pullRequests = jsonDocument.RootElement.GetProperty("pullRequests");
            var firstPR = pullRequests[0];
            var title = firstPR.GetProperty("title").GetString();
            title.Should().Be("測試中文標題 Test Chinese Title");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 測試匯出的 JSON 應使用可讀的格式化縮排
    /// </summary>
    [Fact]
    public async Task Should_Export_Json_With_Indented_Format()
    {
        // Arrange
        var syncResult = TestHelper.CreateSampleSyncResultDto();
        var exporter = TestHelper.CreateJsonFileExporter();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await exporter.ExportAsync(syncResult, tempFilePath, overwrite: true);

            // Assert
            var jsonContent = await File.ReadAllTextAsync(tempFilePath);

            // 驗證 JSON 包含換行與縮排 (可讀格式)
            jsonContent.Should().Contain("\n");
            jsonContent.Should().Contain("  "); // 至少包含 2 個空格的縮排

            // 驗證 JSON 結構正確
            var jsonDocument = JsonDocument.Parse(jsonContent);
            jsonDocument.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 測試匯出的 JSON 應包含所有必要的 PullRequestDto 欄位
    /// </summary>
    [Fact]
    public async Task Should_Export_All_PullRequestDto_Fields()
    {
        // Arrange
        var syncResult = TestHelper.CreateFullSyncResultDto();
        var exporter = TestHelper.CreateJsonFileExporter();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await exporter.ExportAsync(syncResult, tempFilePath, overwrite: true);

            // Assert
            var jsonContent = await File.ReadAllTextAsync(tempFilePath);
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var pullRequests = jsonDocument.RootElement.GetProperty("pullRequests");
            var firstPR = pullRequests[0];

            firstPR.GetProperty("platform").GetString().Should().Be("GitLab");
            firstPR.GetProperty("number").GetInt32().Should().Be(42);
            firstPR.GetProperty("title").GetString().Should().Be("Test PR");
            firstPR.GetProperty("description").GetString().Should().Be("Test Description");
            firstPR.GetProperty("sourceBranch").GetString().Should().Be("feature/test");
            firstPR.GetProperty("targetBranch").GetString().Should().Be("main");
            firstPR.GetProperty("state").GetString().Should().Be("Merged");
            firstPR.GetProperty("authorUsername").GetString().Should().Be("test-user");
            firstPR.GetProperty("authorDisplayName").GetString().Should().Be("Test User");
            firstPR.GetProperty("repositoryName").GetString().Should().Be("test/repo");
            firstPR.GetProperty("url").GetString().Should().Be("https://gitlab.com/test/repo/-/merge_requests/42");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 測試匯出的 JSON 應正確處理 null 值欄位
    /// </summary>
    [Fact]
    public async Task Should_Export_Null_Fields_Correctly()
    {
        // Arrange
        var syncResult = TestHelper.CreateSyncResultDtoWithNullFields();
        var exporter = TestHelper.CreateJsonFileExporter();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await exporter.ExportAsync(syncResult, tempFilePath, overwrite: true);

            // Assert
            var jsonContent = await File.ReadAllTextAsync(tempFilePath);
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var pullRequests = jsonDocument.RootElement.GetProperty("pullRequests");
            var firstPR = pullRequests[0];

            // 驗證 null 值欄位存在且值為 null
            firstPR.TryGetProperty("description", out var description).Should().BeTrue();
            description.ValueKind.Should().Be(JsonValueKind.Null);

            firstPR.TryGetProperty("mergedAt", out var mergedAt).Should().BeTrue();
            mergedAt.ValueKind.Should().Be(JsonValueKind.Null);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 測試匯出的 JSON 應包含 AssociatedWorkItem 欄位 (若有)
    /// </summary>
    [Fact]
    public async Task Should_Export_AssociatedWorkItem_When_Present()
    {
        // Arrange
        var syncResult = TestHelper.CreateSyncResultDtoWithWorkItem();
        var exporter = TestHelper.CreateJsonFileExporter();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Act
            await exporter.ExportAsync(syncResult, tempFilePath, overwrite: true);

            // Assert
            var jsonContent = await File.ReadAllTextAsync(tempFilePath);
            var jsonDocument = JsonDocument.Parse(jsonContent);
            var pullRequests = jsonDocument.RootElement.GetProperty("pullRequests");
            var firstPR = pullRequests[0];

            firstPR.TryGetProperty("associatedWorkItem", out var workItem).Should().BeTrue();
            workItem.ValueKind.Should().Be(JsonValueKind.Object);

            workItem.GetProperty("id").GetInt32().Should().Be(1234);
            workItem.GetProperty("title").GetString().Should().Be("Test Work Item");
            workItem.GetProperty("type").GetString().Should().Be("User Story");
            workItem.GetProperty("state").GetString().Should().Be("Active");
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
