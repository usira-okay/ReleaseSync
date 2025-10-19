using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Domain.Models;

namespace ReleaseSync.Integration.Tests.Helpers;

/// <summary>
/// 測試輔助類別
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// 建立測試用的 JsonFileExporter (使用模擬 Logger)
    /// </summary>
    public static JsonFileExporter CreateJsonFileExporter()
    {
        var logger = Substitute.For<ILogger<JsonFileExporter>>();
        return new JsonFileExporter(logger);
    }

    /// <summary>
    /// 建立範例 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateSampleSyncResultDto()
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
        };

        syncResult.AddPullRequest(new PullRequestInfo
        {
            Platform = "GitLab",
            Id = "12345",
            Number = 1,
            Title = "Test PR",
            SourceBranch = new BranchName("feature/test"),
            TargetBranch = new BranchName("main"),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            State = "Merged",
            AuthorUsername = "test-user",
            RepositoryName = "test/repo"
        });

        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("GitLab", 1, 1000));
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 建立包含 Work Item 的 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateSyncResultDtoWithWorkItem()
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
        };

        var pullRequest = new PullRequestInfo
        {
            Platform = "GitLab",
            Id = "12345",
            Number = 1,
            Title = "Test PR",
            SourceBranch = new BranchName("feature/vsts1234-test"),
            TargetBranch = new BranchName("main"),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            State = "Merged",
            AuthorUsername = "test-user",
            RepositoryName = "test/repo"
        };

        pullRequest.AssociatedWorkItem = new WorkItemInfo
        {
            Id = new WorkItemId(1234),
            Title = "Test Work Item",
            Type = "User Story",
            State = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        syncResult.AddPullRequest(pullRequest);
        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("GitLab", 1, 1000));
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 建立空的 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateEmptySyncResultDto()
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
        };
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 建立包含多筆資料的 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateLargeSyncResultDto(int count = 100)
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow)
        };

        for (int i = 1; i <= count; i++)
        {
            syncResult.AddPullRequest(new PullRequestInfo
            {
                Platform = i % 2 == 0 ? "GitLab" : "BitBucket",
                Id = i.ToString(),
                Number = i,
                Title = $"Test PR {i}",
                SourceBranch = new BranchName($"feature/test-{i}"),
                TargetBranch = new BranchName("main"),
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                State = i % 3 == 0 ? "Merged" : "Open",
                AuthorUsername = $"user-{i % 10}",
                RepositoryName = "test/repo"
            });
        }

        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("GitLab", count / 2, 5000));
        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("BitBucket", count / 2, 4500));
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 建立包含失敗平台的 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateSyncResultDtoWithFailedPlatform()
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
        };

        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("GitLab", 10, 2000));
        syncResult.RecordPlatformStatus(PlatformSyncStatus.Failure("BitBucket", "Authentication failed: Invalid token", 500));
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 建立包含中文內容的 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateSyncResultDtoWithChineseContent()
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
        };

        syncResult.AddPullRequest(new PullRequestInfo
        {
            Platform = "GitLab",
            Id = "12345",
            Number = 1,
            Title = "測試中文標題 Test Chinese Title",
            SourceBranch = new BranchName("feature/test"),
            TargetBranch = new BranchName("main"),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            State = "Merged",
            AuthorUsername = "test-user",
            RepositoryName = "test/repo"
        });

        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("GitLab", 1, 1000));
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 建立包含完整欄位的 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateFullSyncResultDto()
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
        };

        syncResult.AddPullRequest(new PullRequestInfo
        {
            Platform = "GitLab",
            Id = "12345",
            Number = 42,
            Title = "Test PR",
            Description = "Test Description",
            SourceBranch = new BranchName("feature/test"),
            TargetBranch = new BranchName("main"),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            MergedAt = DateTime.UtcNow.AddDays(-1),
            State = "Merged",
            AuthorUsername = "test-user",
            AuthorDisplayName = "Test User",
            RepositoryName = "test/repo",
            Url = "https://gitlab.com/test/repo/-/merge_requests/42"
        });

        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("GitLab", 1, 1000));
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }

    /// <summary>
    /// 建立包含 null 欄位的 SyncResultDto
    /// </summary>
    public static SyncResultDto CreateSyncResultDtoWithNullFields()
    {
        var syncResult = new SyncResult
        {
            SyncDateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
        };

        syncResult.AddPullRequest(new PullRequestInfo
        {
            Platform = "GitLab",
            Id = "12345",
            Number = 1,
            Title = "Test PR",
            Description = null,
            SourceBranch = new BranchName("feature/test"),
            TargetBranch = new BranchName("main"),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            MergedAt = null,
            State = "Open",
            AuthorUsername = "test-user",
            AuthorDisplayName = null,
            RepositoryName = "test/repo",
            Url = null
        });

        syncResult.RecordPlatformStatus(PlatformSyncStatus.Success("GitLab", 1, 1000));
        syncResult.MarkAsCompleted();

        return SyncResultDto.FromDomain(syncResult);
    }
}
