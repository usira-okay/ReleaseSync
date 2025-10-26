namespace ReleaseSync.Application.UnitTests.Exporters;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Domain.Models;
using Xunit;

/// <summary>
/// JsonFileExporter 單元測試
/// </summary>
public class JsonFileExporterTests : IDisposable
{
    private readonly ILogger<JsonFileExporter> _logger;
    private readonly JsonFileExporter _exporter;
    private readonly string _testOutputDirectory;

    public JsonFileExporterTests()
    {
        _logger = Substitute.For<ILogger<JsonFileExporter>>();
        _exporter = new JsonFileExporter(_logger);

        // 建立測試輸出目錄
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), $"JsonFileExporterTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDirectory);
    }

    public void Dispose()
    {
        // 清理測試目錄
        if (Directory.Exists(_testOutputDirectory))
        {
            Directory.Delete(_testOutputDirectory, recursive: true);
        }
    }

    #region 建構子測試

    [Fact(DisplayName = "建構子: logger 為 null 應拋出例外")]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new JsonFileExporter(null!));

        exception.ParamName.Should().Be("logger");
    }

    #endregion

    #region ExportAsync 測試

    [Fact(DisplayName = "ExportAsync: 成功匯出 JSON 檔案")]
    public async Task ExportAsync_ValidInput_ShouldCreateJsonFile()
    {
        // Arrange
        var syncResult = CreateTestSyncResult();
        var outputPath = Path.Combine(_testOutputDirectory, "output.json");

        // Act - 使用舊格式以便測試可以反序列化為 SyncResultDto
        await _exporter.ExportAsync(syncResult, outputPath, useWorkItemCentricFormat: false);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        // 驗證 JSON 內容
        var jsonContent = await File.ReadAllTextAsync(outputPath);
        jsonContent.Should().NotBeNullOrEmpty();

        // 驗證可以反序列化 (使用相同的 JsonSerializerOptions)
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var deserializedResult = JsonSerializer.Deserialize<SyncResultDto>(jsonContent, options);
        deserializedResult.Should().NotBeNull();
        deserializedResult!.TotalPullRequestCount.Should().Be(syncResult.TotalPullRequestCount);
    }

    [Fact(DisplayName = "ExportAsync: 輸出 JSON 應為 camelCase 格式")]
    public async Task ExportAsync_OutputFormat_ShouldBeCamelCase()
    {
        // Arrange
        var syncResult = CreateTestSyncResult();
        var outputPath = Path.Combine(_testOutputDirectory, "output_camelcase.json");

        // Act - 使用舊格式測試 camelCase
        await _exporter.ExportAsync(syncResult, outputPath, useWorkItemCentricFormat: false);

        // Assert
        var jsonContent = await File.ReadAllTextAsync(outputPath);

        // 驗證使用 camelCase 命名 (而非 PascalCase)
        jsonContent.Should().Contain("\"syncStartedAt\"");
        jsonContent.Should().Contain("\"totalPullRequestCount\"");
        jsonContent.Should().Contain("\"platformStatuses\"");
        jsonContent.Should().NotContain("\"SyncStartedAt\"");
        jsonContent.Should().NotContain("\"TotalPullRequestCount\"");
    }

    [Fact(DisplayName = "ExportAsync: 輸出 JSON 應為格式化 (indented) 格式")]
    public async Task ExportAsync_OutputFormat_ShouldBeIndented()
    {
        // Arrange
        var syncResult = CreateTestSyncResult();
        var outputPath = Path.Combine(_testOutputDirectory, "output_indented.json");

        // Act
        await _exporter.ExportAsync(syncResult, outputPath);

        // Assert
        var jsonContent = await File.ReadAllTextAsync(outputPath);

        // 格式化的 JSON 應包含換行符號
        jsonContent.Should().Contain("\n");
        jsonContent.Should().Contain("  "); // 縮排
    }

    [Fact(DisplayName = "ExportAsync: 檔案已存在且 overwrite=false 應拋出例外")]
    public async Task ExportAsync_FileExistsWithoutOverwrite_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var syncResult = CreateTestSyncResult();
        var outputPath = Path.Combine(_testOutputDirectory, "existing.json");

        // 先建立檔案
        await File.WriteAllTextAsync(outputPath, "existing content");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _exporter.ExportAsync(syncResult, outputPath, overwrite: false));

        exception.Message.Should().Contain("輸出檔案已存在");
        exception.Message.Should().Contain("--force");
    }

    [Fact(DisplayName = "ExportAsync: 檔案已存在且 overwrite=true 應成功覆蓋")]
    public async Task ExportAsync_FileExistsWithOverwrite_ShouldOverwriteFile()
    {
        // Arrange
        var syncResult = CreateTestSyncResult();
        var outputPath = Path.Combine(_testOutputDirectory, "overwrite.json");

        // 先建立檔案
        await File.WriteAllTextAsync(outputPath, "old content");

        // Act - 使用舊格式
        await _exporter.ExportAsync(syncResult, outputPath, overwrite: true, useWorkItemCentricFormat: false);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = await File.ReadAllTextAsync(outputPath);
        jsonContent.Should().NotContain("old content");
        jsonContent.Should().Contain("\"totalPullRequestCount\"");
    }

    [Fact(DisplayName = "ExportAsync: 目錄不存在應自動建立")]
    public async Task ExportAsync_DirectoryNotExists_ShouldCreateDirectory()
    {
        // Arrange
        var syncResult = CreateTestSyncResult();
        var nestedPath = Path.Combine(_testOutputDirectory, "nested", "deep", "output.json");

        // Act
        await _exporter.ExportAsync(syncResult, nestedPath);

        // Assert
        File.Exists(nestedPath).Should().BeTrue();
        Directory.Exists(Path.GetDirectoryName(nestedPath)).Should().BeTrue();
    }

    [Fact(DisplayName = "ExportAsync: CancellationToken 應正確傳遞")]
    public async Task ExportAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var syncResult = CreateTestSyncResult();
        var outputPath = Path.Combine(_testOutputDirectory, "cancelled.json");
        var cts = new CancellationTokenSource();

        // 立即取消
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _exporter.ExportAsync(syncResult, outputPath, cancellationToken: cts.Token));
    }

    [Fact(DisplayName = "ExportAsync: 空的 PullRequests 應成功匯出")]
    public async Task ExportAsync_EmptyPullRequests_ShouldExportSuccessfully()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        var syncResult = new SyncResult { SyncDateRange = dateRange };
        syncResult.MarkAsCompleted();

        var syncResultDto = SyncResultDto.FromDomain(syncResult);
        var outputPath = Path.Combine(_testOutputDirectory, "empty.json");

        // Act - 使用舊格式
        await _exporter.ExportAsync(syncResultDto, outputPath, useWorkItemCentricFormat: false);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = await File.ReadAllTextAsync(outputPath);
        jsonContent.Should().Contain("\"totalPullRequestCount\": 0");
    }

    #endregion

    #region FileExists 測試

    [Fact(DisplayName = "FileExists: 檔案存在應回傳 true")]
    public void FileExists_FileExists_ShouldReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testOutputDirectory, "exists.txt");
        File.WriteAllText(filePath, "test");

        // Act
        var result = _exporter.FileExists(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "FileExists: 檔案不存在應回傳 false")]
    public void FileExists_FileNotExists_ShouldReturnFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testOutputDirectory, "not_exists.txt");

        // Act
        var result = _exporter.FileExists(filePath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 輔助方法

    /// <summary>
    /// 建立測試用的 SyncResultDto
    /// </summary>
    private SyncResultDto CreateTestSyncResult()
    {
        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        var syncResult = new SyncResult { SyncDateRange = dateRange };

        // 加入測試 PR/MR
        var pullRequest = new PullRequestInfo
        {
            Platform = "GitLab",
            Id = "1",
            Number = 1,
            Title = "Test PR",
            SourceBranch = new BranchName("feature/test"),
            TargetBranch = new BranchName("main"),
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            State = "Merged",
            RepositoryName = "test/repo"
        };

        syncResult.AddPullRequest(pullRequest);

        // 記錄平台狀態
        syncResult.RecordPlatformStatus(
            PlatformSyncStatus.Success("GitLab", 1, 1000));

        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    #endregion
}
