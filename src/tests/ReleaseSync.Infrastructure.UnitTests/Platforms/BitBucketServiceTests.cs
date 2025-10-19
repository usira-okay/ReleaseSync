namespace ReleaseSync.Infrastructure.UnitTests.Platforms;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Platforms.BitBucket;
using Xunit;

/// <summary>
/// BitBucketService 單元測試
/// </summary>
public class BitBucketServiceTests
{
    private readonly IPullRequestRepository _mockRepository;
    private readonly ILogger<BitBucketService> _mockLogger;
    private readonly IOptions<BitBucketSettings> _options;

    public BitBucketServiceTests()
    {
        _mockRepository = Substitute.For<IPullRequestRepository>();
        _mockLogger = Substitute.For<ILogger<BitBucketService>>();

        var settings = new BitBucketSettings
        {
            ApiUrl = "https://api.bitbucket.org/2.0",
            Email = "test@example.com",
            AccessToken = "test-password",
            Projects = new List<BitBucketProjectSettings>
            {
                new() { WorkspaceAndRepo = "workspace/repo1", TargetBranches = new List<string> { "main", "master" } },
                new() { WorkspaceAndRepo = "workspace/repo2", TargetBranches = new List<string> { "main", "master" } }
            }
        };

        _options = Options.Create(settings);
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

        var service = new BitBucketService(_mockRepository, _options, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // 兩個 projects 各回傳一個 PR

        // 驗證第一個 PR 的內容
        var firstPr = result.First();
        firstPr.Platform.Should().Be("BitBucket");
        firstPr.Title.Should().Be("Test PR 1");
        firstPr.Number.Should().Be(42);

        // 驗證 Repository 被正確呼叫 (2 個 repositories)
        await _mockRepository.Received(2).GetPullRequestsAsync(
            Arg.Any<string>(),
            Arg.Is<DateRange>(dr => dr.StartDate == dateRange.StartDate && dr.EndDate == dateRange.EndDate),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// 測試當沒有 Repository 設定時回傳空清單
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldReturnEmptyList_WhenNoProjectsConfigured()
    {
        // Arrange
        var emptySettings = new BitBucketSettings
        {
            ApiUrl = "https://api.bitbucket.org/2.0",
            Email = "test@example.com",
            AccessToken = "test-password",
            Projects = new List<BitBucketProjectSettings>() // 空清單
        };

        var emptyOptions = Options.Create(emptySettings);
        var service = new BitBucketService(_mockRepository, emptyOptions, _mockLogger);

        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().BeEmpty();
    }


    /// <summary>
    /// 測試當 Repository 拋出例外時返回空列表（容錯處理）
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldReturnEmptyList_WhenRepositoryFails()
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
            .Returns<IEnumerable<PullRequestInfo>>(x => throw new Exception("BitBucket API 回應 401 Unauthorized"));

        var service = new BitBucketService(_mockRepository, _options, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // 容錯處理：單一專案失敗不影響其他專案
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

        var service = new BitBucketService(_mockRepository, _options, _mockLogger);

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
                "workspace/repo1",
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
                    RepositoryName = "workspace/repo1"
                }
            });

        // 第二個 repo 回傳 1 筆
        _mockRepository
            .GetPullRequestsAsync(
                "workspace/repo2",
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
                    RepositoryName = "workspace/repo2"
                }
            });

        var service = new BitBucketService(_mockRepository, _options, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(pr => pr.RepositoryName == "workspace/repo1");
        result.Should().Contain(pr => pr.RepositoryName == "workspace/repo2");
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

        var service = new BitBucketService(_mockRepository, _options, _mockLogger);

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
                "workspace/repo1",
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns<IEnumerable<PullRequestInfo>>(x => throw new Exception("Repository not found"));

        // 第二個 repo 成功
        _mockRepository
            .GetPullRequestsAsync(
                "workspace/repo2",
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
                    RepositoryName = "workspace/repo2"
                }
            });

        var service = new BitBucketService(_mockRepository, _options, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert - 第一個失敗但不影響第二個，應該回傳第二個 repo 的結果
        result.Should().HaveCount(1);
        result.First().RepositoryName.Should().Be("workspace/repo2");
        result.First().Title.Should().Be("PR from repo2");
    }
}
