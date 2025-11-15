using FluentAssertions;
using ReleaseSync.Application.DTOs;
using System.Diagnostics;

namespace ReleaseSync.Application.UnitTests.DTOs;

/// <summary>
/// RepositoryBasedOutputDto 測試類別
/// 驗證 Repository 分組邏輯、名稱提取與 Work Item 對映
/// </summary>
public class RepositoryBasedOutputDtoTests
{
    #region Repository 分組邏輯測試

    /// <summary>
    /// 測試空資料處理
    /// </summary>
    [Fact]
    public void FromSyncResult_EmptyData_ReturnsEmptyRepositories()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 0,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>(),
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Should().NotBeNull();
        result.StartDate.Should().Be(new DateTime(2025, 1, 1));
        result.EndDate.Should().Be(new DateTime(2025, 1, 31));
        result.Repositories.Should().NotBeNull();
        result.Repositories.Should().BeEmpty();
    }

    /// <summary>
    /// 測試單一 Repository 分組
    /// </summary>
    [Fact]
    public void FromSyncResult_SingleRepository_GroupsCorrectly()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 2,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 123,
                    Title = "Feature A",
                    SourceBranch = "feature/a",
                    TargetBranch = "main",
                    CreatedAt = new DateTime(2025, 1, 10),
                    State = "merged",
                    RepositoryName = "owner/test-repo"
                },
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 124,
                    Title = "Feature B",
                    SourceBranch = "feature/b",
                    TargetBranch = "main",
                    CreatedAt = new DateTime(2025, 1, 15),
                    State = "merged",
                    RepositoryName = "owner/test-repo"
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories.Should().HaveCount(1);
        result.Repositories[0].RepositoryName.Should().Be("test-repo");
        result.Repositories[0].Platform.Should().Be("GitLab");
        result.Repositories[0].PullRequests.Should().HaveCount(2);
    }

    /// <summary>
    /// 測試多 Repository 依名稱與平台分組
    /// </summary>
    [Fact]
    public void FromSyncResult_MultipleRepositories_GroupsByNameAndPlatform()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 4,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 1,
                    Title = "PR 1",
                    SourceBranch = "feature/1",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "owner/repo-a"
                },
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 2,
                    Title = "PR 2",
                    SourceBranch = "feature/2",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "owner/repo-b"
                },
                new PullRequestDto
                {
                    Platform = "BitBucket",
                    Number = 3,
                    Title = "PR 3",
                    SourceBranch = "feature/3",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "team/repo-c"
                },
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 4,
                    Title = "PR 4",
                    SourceBranch = "feature/4",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "owner/repo-a"
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories.Should().HaveCount(3);
        
        // 驗證 repo-a (GitLab) 有 2 個 PRs
        var repoA = result.Repositories.FirstOrDefault(r => r.RepositoryName == "repo-a" && r.Platform == "GitLab");
        repoA.Should().NotBeNull();
        repoA!.PullRequests.Should().HaveCount(2);

        // 驗證 repo-b (GitLab) 有 1 個 PR
        var repoB = result.Repositories.FirstOrDefault(r => r.RepositoryName == "repo-b" && r.Platform == "GitLab");
        repoB.Should().NotBeNull();
        repoB!.PullRequests.Should().HaveCount(1);

        // 驗證 repo-c (BitBucket) 有 1 個 PR
        var repoC = result.Repositories.FirstOrDefault(r => r.RepositoryName == "repo-c" && r.Platform == "BitBucket");
        repoC.Should().NotBeNull();
        repoC!.PullRequests.Should().HaveCount(1);
    }

    /// <summary>
    /// 測試相同名稱但不同平台的 repository 分開處理
    /// </summary>
    [Fact]
    public void FromSyncResult_SameName_DifferentPlatforms_CreatesSeperateGroups()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 3,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 1,
                    Title = "GitLab PR",
                    SourceBranch = "feature/1",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "owner/same-repo"
                },
                new PullRequestDto
                {
                    Platform = "BitBucket",
                    Number = 2,
                    Title = "BitBucket PR",
                    SourceBranch = "feature/2",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "team/same-repo"
                },
                new PullRequestDto
                {
                    Platform = "AzureDevOps",
                    Number = 3,
                    Title = "Azure DevOps PR",
                    SourceBranch = "feature/3",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "project/same-repo"
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories.Should().HaveCount(3);

        // 驗證三個平台的 same-repo 獨立分組
        var gitlabRepo = result.Repositories.FirstOrDefault(r => r.Platform == "GitLab");
        gitlabRepo.Should().NotBeNull();
        gitlabRepo!.RepositoryName.Should().Be("same-repo");
        gitlabRepo.PullRequests.Should().HaveCount(1);

        var bitbucketRepo = result.Repositories.FirstOrDefault(r => r.Platform == "BitBucket");
        bitbucketRepo.Should().NotBeNull();
        bitbucketRepo!.RepositoryName.Should().Be("same-repo");
        bitbucketRepo.PullRequests.Should().HaveCount(1);

        var azdoRepo = result.Repositories.FirstOrDefault(r => r.Platform == "AzureDevOps");
        azdoRepo.Should().NotBeNull();
        azdoRepo!.RepositoryName.Should().Be("same-repo");
        azdoRepo.PullRequests.Should().HaveCount(1);
    }

    #endregion

    #region Repository 名稱提取測試

    /// <summary>
    /// 測試從 owner/repo 提取 repo
    /// </summary>
    [Fact]
    public void ExtractRepositoryName_WithSlash_ReturnsLastPart()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 1,
                    Title = "Test",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "owner/my-repo"
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories[0].RepositoryName.Should().Be("my-repo");
    }

    /// <summary>
    /// 測試無 slash 時返回原始名稱
    /// </summary>
    [Fact]
    public void ExtractRepositoryName_WithoutSlash_ReturnsOriginal()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 1,
                    Title = "Test",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "standalone"
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories[0].RepositoryName.Should().Be("standalone");
    }

    /// <summary>
    /// 測試多個 slash 時取最後部分
    /// </summary>
    [Fact]
    public void ExtractRepositoryName_MultipleSlashes_ReturnsLastPart()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 1,
                    Title = "Test",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "org/team/project"
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories[0].RepositoryName.Should().Be("project");
    }

    /// <summary>
    /// 測試空字串邊界情況
    /// </summary>
    [Fact]
    public void ExtractRepositoryName_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 1,
                    Title = "Test",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = ""
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories[0].RepositoryName.Should().Be("");
    }

    #endregion

    #region Work Item 關聯測試

    /// <summary>
    /// 測試無 Work Item 時明確設為 null
    /// </summary>
    [Fact]
    public void FromSyncResult_WorkItemNull_SetsWorkItemToNull()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 1,
                    Title = "Test PR without Work Item",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "owner/repo",
                    AssociatedWorkItem = null
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.Repositories[0].PullRequests[0].WorkItem.Should().BeNull();
    }

    /// <summary>
    /// 測試 Work Item 正確對映
    /// </summary>
    [Fact]
    public void FromSyncResult_WorkItemExists_MapsCorrectly()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 1,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "AzureDevOps",
                    Number = 1,
                    Title = "Test PR with Work Item",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "project/repo",
                    AssociatedWorkItem = new WorkItemDto
                    {
                        Id = 12345,
                        Title = "Implement Feature X",
                        Type = "User Story",
                        State = "Done",
                        Url = "https://dev.azure.com/org/project/_workitems/edit/12345",
                        Team = "Team Alpha"
                    }
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        var workItem = result.Repositories[0].PullRequests[0].WorkItem;
        workItem.Should().NotBeNull();
        workItem!.WorkItemId.Should().Be(12345);
        workItem.WorkItemTitle.Should().Be("Implement Feature X");
        workItem.WorkItemType.Should().Be("User Story");
        workItem.WorkItemUrl.Should().Be("https://dev.azure.com/org/project/_workitems/edit/12345");
        workItem.WorkItemTeam.Should().Be("Team Alpha");
    }

    /// <summary>
    /// 測試 Work Item Team 為 null 時的處理
    /// </summary>
    [Fact]
    public void FromSyncResult_WorkItemWithNullTeam_HandlesGracefully()
    {
        // Arrange
        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 1,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "AzureDevOps",
                    Number = 1,
                    Title = "Test PR",
                    SourceBranch = "feature",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = "project/repo",
                    AssociatedWorkItem = new WorkItemDto
                    {
                        Id = 99999,
                        Title = "Bug Fix",
                        Type = "Bug",
                        State = "Active",
                        Team = null
                    }
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        var workItem = result.Repositories[0].PullRequests[0].WorkItem;
        workItem.Should().NotBeNull();
        workItem!.WorkItemTeam.Should().BeNull();
    }

    #endregion

    #region 日期與資料完整性測試

    /// <summary>
    /// 測試 StartDate 與 EndDate 正確保留
    /// </summary>
    [Fact]
    public void FromSyncResult_PreservesDateRange()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = startDate,
            EndDate = endDate,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 0,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>(),
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        result.StartDate.Should().Be(startDate);
        result.EndDate.Should().Be(endDate);
    }

    /// <summary>
    /// 測試所有 PR 欄位正確對映
    /// </summary>
    [Fact]
    public void FromSyncResult_PreservesAllPullRequestFields()
    {
        // Arrange
        var mergedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc);

        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 1,
            LinkedWorkItemCount = 0,
            PullRequests = new List<PullRequestDto>
            {
                new PullRequestDto
                {
                    Platform = "GitLab",
                    Number = 123,
                    Title = "Detailed PR Title",
                    SourceBranch = "feature/detailed",
                    TargetBranch = "develop",
                    CreatedAt = createdAt,
                    MergedAt = mergedAt,
                    State = "merged",
                    AuthorUserId = "user123",
                    AuthorDisplayName = "John Doe",
                    RepositoryName = "owner/test-repo",
                    Url = "https://gitlab.com/owner/test-repo/-/merge_requests/123"
                }
            },
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);

        // Assert
        var pr = result.Repositories[0].PullRequests[0];
        pr.PullRequestTitle.Should().Be("Detailed PR Title");
        pr.SourceBranch.Should().Be("feature/detailed");
        pr.TargetBranch.Should().Be("develop");
        pr.MergedAt.Should().Be(mergedAt);
        pr.AuthorUserId.Should().Be("user123");
        pr.AuthorDisplayName.Should().Be("John Doe");
        pr.PullRequestUrl.Should().Be("https://gitlab.com/owner/test-repo/-/merge_requests/123");
    }

    #endregion

    #region 效能測試

    /// <summary>
    /// 測試 2000 PRs 處理效能 (需在 5 秒內完成)
    /// </summary>
    [Fact]
    public void FromSyncResult_LargeDataset_CompletesWithin5Seconds()
    {
        // Arrange - 產生 2000 筆 PRs (100 repositories × 20 PRs)
        var pullRequests = new List<PullRequestDto>();
        for (int repoIndex = 0; repoIndex < 100; repoIndex++)
        {
            for (int prIndex = 0; prIndex < 20; prIndex++)
            {
                pullRequests.Add(new PullRequestDto
                {
                    Platform = repoIndex % 3 == 0 ? "GitLab" : repoIndex % 3 == 1 ? "BitBucket" : "AzureDevOps",
                    Number = prIndex,
                    Title = $"PR {prIndex} in Repo {repoIndex}",
                    SourceBranch = $"feature/{prIndex}",
                    TargetBranch = "main",
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    RepositoryName = $"owner/repo-{repoIndex}"
                });
            }
        }

        var syncResult = new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = 2000,
            LinkedWorkItemCount = 0,
            PullRequests = pullRequests,
            PlatformStatuses = new List<PlatformStatusDto>()
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = RepositoryBasedOutputDto.FromSyncResult(syncResult);
        stopwatch.Stop();

        // Assert
        result.Repositories.Should().HaveCount(100); // 100 個不同的 repositories (每個在不同平台)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "處理 2000 筆 PRs 應在 5 秒內完成");
    }

    #endregion
}
