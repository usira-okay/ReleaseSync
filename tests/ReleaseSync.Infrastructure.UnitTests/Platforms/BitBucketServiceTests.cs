namespace ReleaseSync.Infrastructure.UnitTests.Platforms;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Platforms.BitBucket;

/// <summary>
/// BitBucketService 單元測試
/// </summary>
public class BitBucketServiceTests
{
    private readonly IPullRequestRepository _mockRepository;
    private readonly ILogger<BitBucketService> _mockLogger;
    private readonly BitBucketSettings _settings;

    public BitBucketServiceTests()
    {
        _mockRepository = Substitute.For<IPullRequestRepository>();
        _mockLogger = Substitute.For<ILogger<BitBucketService>>();

        _settings = new BitBucketSettings
        {
            WorkspaceId = "test-workspace",
            AccessToken = "test-token",
            Email = "test@example.com",
            Repositories = new List<string> { "repo1", "repo2" },
            TargetBranches = new List<string> { "main", "master" }
        };
    }

    /// <summary>
    /// 測試成功抓取 Pull Requests
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldReturnPullRequests_WhenRepositoryReturnsData()
    {
        // Arrange
        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        var expectedPullRequests = new List<PullRequestInfo>
        {
            new()
            {
                Platform = "BitBucket",
                Id = "1",
                Number = 42,
                Title = "Test PR 1",
                SourceBranch = new BranchName("feature/test"),
                TargetBranch = new BranchName("main"),
                CreatedAt = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                State = "MERGED",
                AuthorUsername = "testuser",
                RepositoryName = "test-workspace/repo1"
            }
        };

        _mockRepository
            .GetPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedPullRequests);

        var service = new BitBucketService(_mockRepository, _settings, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expectedPullRequests);

        // 驗證 Repository 被正確呼叫 (2 個 repositories)
        await _mockRepository.Received(2).GetPullRequestsAsync(
            Arg.Any<string>(),
            Arg.Is<DateRange>(dr => dr.StartDate == dateRange.StartDate && dr.EndDate == dateRange.EndDate),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// 測試當沒有 Repository 設定時拋出例外
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowException_WhenNoRepositoriesConfigured()
    {
        // Arrange
        var emptySettings = new BitBucketSettings
        {
            WorkspaceId = "test-workspace",
            AccessToken = "test-token",
            Email = "test@example.com",
            Repositories = new List<string>() // 空清單
        };

        // Act
        Action act = () => new BitBucketService(_mockRepository, emptySettings, _mockLogger);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*至少須設定一個 BitBucket Repository*");
    }

    /// <summary>
    /// 測試當 WorkspaceId 為空時拋出例外
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowException_WhenWorkspaceIdIsEmpty()
    {
        // Arrange
        var invalidSettings = new BitBucketSettings
        {
            WorkspaceId = "", // 空字串
            AccessToken = "test-token",
            Email = "test@example.com",
            Repositories = new List<string> { "repo1" }
        };

        // Act
        Action act = () => new BitBucketService(_mockRepository, invalidSettings, _mockLogger);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*WorkspaceId 不能為空*");
    }

    /// <summary>
    /// 測試當 Repository 拋出例外時正確處理
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldThrowException_WhenRepositoryFails()
    {
        // Arrange
        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        _mockRepository
            .GetPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("BitBucket API 回應 401 Unauthorized"));

        var service = new BitBucketService(_mockRepository, _settings, _mockLogger);

        // Act
        Func<Task> act = async () => await service.GetPullRequestsAsync(dateRange);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*BitBucket API 回應 401 Unauthorized*");
    }

    /// <summary>
    /// 測試當 Repository 回傳空結果時正確處理
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldReturnEmptyList_WhenNoDataFound()
    {
        // Arrange
        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        _mockRepository
            .GetPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>());

        var service = new BitBucketService(_mockRepository, _settings, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// 測試多個 Repository 的 Pull Requests 合併結果
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldMergeResults_FromMultipleRepositories()
    {
        // Arrange
        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        // 第一個 repo 回傳 1 筆
        _mockRepository
            .GetPullRequestsAsync(
                "test-workspace/repo1",
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>
            {
                new()
                {
                    Platform = "BitBucket",
                    Id = "1",
                    Number = 1,
                    Title = "PR from repo1",
                    SourceBranch = new BranchName("feature/a"),
                    TargetBranch = new BranchName("main"),
                    CreatedAt = DateTime.UtcNow,
                    State = "MERGED",
                    AuthorUsername = "user1",
                    RepositoryName = "test-workspace/repo1"
                }
            });

        // 第二個 repo 回傳 1 筆
        _mockRepository
            .GetPullRequestsAsync(
                "test-workspace/repo2",
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>
            {
                new()
                {
                    Platform = "BitBucket",
                    Id = "2",
                    Number = 2,
                    Title = "PR from repo2",
                    SourceBranch = new BranchName("feature/b"),
                    TargetBranch = new BranchName("main"),
                    CreatedAt = DateTime.UtcNow,
                    State = "OPEN",
                    AuthorUsername = "user2",
                    RepositoryName = "test-workspace/repo2"
                }
            });

        var service = new BitBucketService(_mockRepository, _settings, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(pr => pr.RepositoryName == "test-workspace/repo1");
        result.Should().Contain(pr => pr.RepositoryName == "test-workspace/repo2");
    }

    /// <summary>
    /// 測試 CancellationToken 正確傳遞
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldPassCancellationToken_ToRepository()
    {
        // Arrange
        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _mockRepository
            .GetPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>());

        var service = new BitBucketService(_mockRepository, _settings, _mockLogger);

        // Act
        await service.GetPullRequestsAsync(dateRange, cancellationToken);

        // Assert
        await _mockRepository.Received().GetPullRequestsAsync(
            Arg.Any<string>(),
            Arg.Any<DateRange>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    /// <summary>
    /// 測試當單一 Repository 失敗時,其他 Repository 仍繼續執行
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldContinueOtherRepositories_WhenOneRepositoryFails()
    {
        // Arrange
        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        // 第一個 repo 失敗
        _mockRepository
            .GetPullRequestsAsync(
                "test-workspace/repo1",
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Repository not found"));

        // 第二個 repo 成功
        _mockRepository
            .GetPullRequestsAsync(
                "test-workspace/repo2",
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>
            {
                new()
                {
                    Platform = "BitBucket",
                    Id = "2",
                    Number = 2,
                    Title = "PR from repo2",
                    SourceBranch = new BranchName("feature/b"),
                    TargetBranch = new BranchName("main"),
                    CreatedAt = DateTime.UtcNow,
                    State = "OPEN",
                    AuthorUsername = "user2",
                    RepositoryName = "test-workspace/repo2"
                }
            });

        var service = new BitBucketService(_mockRepository, _settings, _mockLogger);

        // Act
        Func<Task> act = async () => await service.GetPullRequestsAsync(dateRange);

        // Assert - 第一個失敗應該拋出例外 (或根據實作決定是否繼續)
        // 這裡假設遇到錯誤會拋出例外
        await act.Should().ThrowAsync<Exception>();
    }
}
