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

    [Fact(DisplayName = "ExportAsync: data 為 null 應拋出例外")]
    public async Task ExportAsync_NullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDirectory, "output.json");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _exporter.ExportAsync(null!, outputPath));
    }

    [Fact(DisplayName = "ExportAsync: 成功匯出 JSON 檔案")]
    public async Task ExportAsync_ValidInput_ShouldCreateJsonFile()
    {
        // Arrange
        var repositoryData = CreateTestRepositoryBasedOutput();
        var outputPath = Path.Combine(_testOutputDirectory, "output.json");

        // Act
        await _exporter.ExportAsync(repositoryData, outputPath);

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
        var deserializedResult = JsonSerializer.Deserialize<RepositoryBasedOutputDto>(jsonContent, options);
        deserializedResult.Should().NotBeNull();
        deserializedResult!.Repositories.Should().HaveCount(repositoryData.Repositories.Count);
    }

    [Fact(DisplayName = "ExportAsync: 輸出 JSON 應為 camelCase 格式")]
    public async Task ExportAsync_OutputFormat_ShouldBeCamelCase()
    {
        // Arrange
        var repositoryData = CreateTestRepositoryBasedOutput();
        var outputPath = Path.Combine(_testOutputDirectory, "output_camelcase.json");

        // Act
        await _exporter.ExportAsync(repositoryData, outputPath);

        // Assert
        var jsonContent = await File.ReadAllTextAsync(outputPath);

        // 驗證使用 camelCase 命名 (而非 PascalCase)
        jsonContent.Should().Contain("\"startDate\"");
        jsonContent.Should().Contain("\"endDate\"");
        jsonContent.Should().Contain("\"repositories\"");
        jsonContent.Should().NotContain("\"StartDate\"");
        jsonContent.Should().NotContain("\"Repositories\"");
    }

    [Fact(DisplayName = "ExportAsync: 輸出 JSON 應為格式化 (indented) 格式")]
    public async Task ExportAsync_OutputFormat_ShouldBeIndented()
    {
        // Arrange
        var repositoryData = CreateTestRepositoryBasedOutput();
        var outputPath = Path.Combine(_testOutputDirectory, "output_indented.json");

        // Act
        await _exporter.ExportAsync(repositoryData, outputPath);

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
        var repositoryData = CreateTestRepositoryBasedOutput();
        var outputPath = Path.Combine(_testOutputDirectory, "existing.json");

        // 先建立檔案
        await File.WriteAllTextAsync(outputPath, "existing content");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _exporter.ExportAsync(repositoryData, outputPath, overwrite: false));

        exception.Message.Should().Contain("輸出檔案已存在");
        exception.Message.Should().Contain("--force");
    }

    [Fact(DisplayName = "ExportAsync: 檔案已存在且 overwrite=true 應成功覆蓋")]
    public async Task ExportAsync_FileExistsWithOverwrite_ShouldOverwriteFile()
    {
        // Arrange
        var repositoryData = CreateTestRepositoryBasedOutput();
        var outputPath = Path.Combine(_testOutputDirectory, "overwrite.json");

        // 先建立檔案
        await File.WriteAllTextAsync(outputPath, "old content");

        // Act
        await _exporter.ExportAsync(repositoryData, outputPath, overwrite: true);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = await File.ReadAllTextAsync(outputPath);
        jsonContent.Should().NotContain("old content");
        jsonContent.Should().Contain("\"repositories\":");
    }

    [Fact(DisplayName = "ExportAsync: 目錄不存在應自動建立")]
    public async Task ExportAsync_DirectoryNotExists_ShouldCreateDirectory()
    {
        // Arrange
        var repositoryData = CreateTestRepositoryBasedOutput();
        var nestedPath = Path.Combine(_testOutputDirectory, "nested", "deep", "output.json");

        // Act
        await _exporter.ExportAsync(repositoryData, nestedPath);

        // Assert
        File.Exists(nestedPath).Should().BeTrue();
        Directory.Exists(Path.GetDirectoryName(nestedPath)).Should().BeTrue();
    }

    [Fact(DisplayName = "ExportAsync: CancellationToken 應正確傳遞")]
    public async Task ExportAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var repositoryData = CreateTestRepositoryBasedOutput();
        var outputPath = Path.Combine(_testOutputDirectory, "cancelled.json");
        var cts = new CancellationTokenSource();

        // 立即取消
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _exporter.ExportAsync(repositoryData, outputPath, cancellationToken: cts.Token));
    }

    [Fact(DisplayName = "ExportAsync: 空的 PullRequests 應成功匯出")]
    public async Task ExportAsync_EmptyPullRequests_ShouldExportSuccessfully()
    {
        // Arrange
        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        var syncResult = new SyncResult { SyncDateRange = dateRange };
        syncResult.MarkAsCompleted();

        var syncResultDto = SyncResultDto.FromDomain(syncResult);
        var repositoryData = RepositoryBasedOutputDto.FromSyncResult(syncResultDto);
        var outputPath = Path.Combine(_testOutputDirectory, "empty.json");

        // Act
        await _exporter.ExportAsync(repositoryData, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = await File.ReadAllTextAsync(outputPath);
        jsonContent.Should().Contain("\"repositories\": []");
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

    #region RepositoryBasedOutputDto 測試 (T018 - TDD Red Phase)

    /// <summary>
    /// 測試新 DTO 序列化
    /// </summary>
    [Fact(DisplayName = "ExportAsync: RepositoryBasedDto 應正確序列化")]
    public async Task ExportAsync_RepositoryBasedDto_SerializesCorrectly()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDirectory, "repository-based.json");
        var data = new RepositoryBasedOutputDto
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            Repositories = new List<RepositoryGroupDto>
            {
                new RepositoryGroupDto
                {
                    RepositoryName = "test-repo",
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        new RepositoryPullRequestDto
                        {
                            PullRequestTitle = "Feature A",
                            SourceBranch = "feature/a",
                            TargetBranch = "main",
                            MergedAt = new DateTime(2025, 1, 15),
                            AuthorUserId = "user123",
                            AuthorDisplayName = "John Doe",
                            PullRequestUrl = "https://gitlab.com/test",
                            WorkItem = null
                        }
                    }
                }
            }
        };

        // Act
        await _exporter.ExportAsync(data, outputPath, overwrite: true);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(outputPath);
        json.Should().NotBeNullOrWhiteSpace();
        
        // 驗證 JSON 結構
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("startDate").GetDateTime().Should().Be(new DateTime(2025, 1, 1));
        root.GetProperty("endDate").GetDateTime().Should().Be(new DateTime(2025, 1, 31));
        root.GetProperty("repositories").GetArrayLength().Should().Be(1);
    }

    /// <summary>
    /// 測試 null Work Item 序列化為 JSON null
    /// </summary>
    [Fact(DisplayName = "ExportAsync: RepositoryBasedDto 應正確處理 null WorkItem")]
    public async Task ExportAsync_RepositoryBasedDto_HandlesNullWorkItem()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDirectory, "repo-null-workitem.json");
        var data = new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new RepositoryGroupDto
                {
                    RepositoryName = "test-repo",
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        new RepositoryPullRequestDto
                        {
                            PullRequestTitle = "PR without Work Item",
                            SourceBranch = "feature",
                            TargetBranch = "main",
                            MergedAt = null,
                            AuthorUserId = null,
                            AuthorDisplayName = null,
                            PullRequestUrl = null,
                            WorkItem = null
                        }
                    }
                }
            }
        };

        // Act
        await _exporter.ExportAsync(data, outputPath, overwrite: true);

        // Assert
        var json = await File.ReadAllTextAsync(outputPath);
        
        // 驗證 WorkItem 為 JSON null
        using var document = JsonDocument.Parse(json);
        var pr = document.RootElement
            .GetProperty("repositories")[0]
            .GetProperty("pullRequests")[0];
        
        pr.GetProperty("workItem").ValueKind.Should().Be(JsonValueKind.Null);
    }

    /// <summary>
    /// 測試 RepositoryBasedDto 使用 camelCase
    /// </summary>
    [Fact(DisplayName = "ExportAsync: RepositoryBasedDto 應使用 camelCase 命名")]
    public async Task ExportAsync_RepositoryBasedDto_UsesCamelCase()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDirectory, "repo-camelcase.json");
        var data = new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new RepositoryGroupDto
                {
                    RepositoryName = "test-repo",
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        new RepositoryPullRequestDto
                        {
                            PullRequestTitle = "Test",
                            SourceBranch = "feature",
                            TargetBranch = "main",
                            MergedAt = null,
                            AuthorUserId = null,
                            AuthorDisplayName = null,
                            PullRequestUrl = null,
                            WorkItem = new PullRequestWorkItemDto
                            {
                                WorkItemId = 123,
                                WorkItemTitle = "Task",
                                WorkItemTeam = "Team A",
                                WorkItemType = "Task",
                                WorkItemUrl = "https://example.com"
                            }
                        }
                    }
                }
            }
        };

        // Act
        await _exporter.ExportAsync(data, outputPath, overwrite: true);

        // Assert
        var json = await File.ReadAllTextAsync(outputPath);
        
        // 驗證所有欄位使用 camelCase
        json.Should().Contain("\"repositories\":");
        json.Should().Contain("\"repositoryName\":");
        json.Should().Contain("\"platform\":");
        json.Should().Contain("\"pullRequests\":");
        json.Should().Contain("\"pullRequestTitle\":");
        json.Should().Contain("\"workItemId\":");
        json.Should().Contain("\"workItemTitle\":");
        
        // 不應包含 PascalCase
        json.Should().NotContain("\"RepositoryName\":");
        json.Should().NotContain("\"PullRequests\":");
    }

    /// <summary>
    /// 測試 RepositoryBasedDto 中文字元不跳脫
    /// </summary>
    [Fact(DisplayName = "ExportAsync: RepositoryBasedDto 應正確處理中文字元")]
    public async Task ExportAsync_RepositoryBasedDto_HandlesChineseCharacters()
    {
        // Arrange
        var outputPath = Path.Combine(_testOutputDirectory, "repo-chinese.json");
        var data = new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new RepositoryGroupDto
                {
                    RepositoryName = "測試專案",
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        new RepositoryPullRequestDto
                        {
                            PullRequestTitle = "新增使用者管理功能",
                            SourceBranch = "feature/使用者管理",
                            TargetBranch = "main",
                            MergedAt = null,
                            AuthorUserId = null,
                            AuthorDisplayName = "王小明",
                            PullRequestUrl = null,
                            WorkItem = new PullRequestWorkItemDto
                            {
                                WorkItemId = 999,
                                WorkItemTitle = "實作使用者登入功能",
                                WorkItemTeam = "後端團隊",
                                WorkItemType = "使用者故事",
                                WorkItemUrl = null
                            }
                        }
                    }
                }
            }
        };

        // Act
        await _exporter.ExportAsync(data, outputPath, overwrite: true);

        // Assert
        var json = await File.ReadAllTextAsync(outputPath);
        
        // 驗證中文字元未被跳脫
        json.Should().Contain("測試專案");
        json.Should().Contain("新增使用者管理功能");
        json.Should().Contain("王小明");
        json.Should().Contain("實作使用者登入功能");
        json.Should().Contain("後端團隊");
        
        // 不應包含 Unicode 跳脫序列
        json.Should().NotContain("\\u");
    }

    #endregion

    #region 輔助方法

    /// <summary>
    /// 建立測試用的 RepositoryBasedOutputDto
    /// </summary>
    private RepositoryBasedOutputDto CreateTestRepositoryBasedOutput()
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

        var syncResultDto = SyncResultDto.FromDomain(syncResult);
        return RepositoryBasedOutputDto.FromSyncResult(syncResultDto);
    }

    #endregion
}
