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
using ReleaseSync.Infrastructure.Platforms.GitLab;

/// <summary>
/// GitLabService 單元測試
/// </summary>
public class GitLabServiceTests
{
    private readonly IPullRequestRepository _mockRepository;
    private readonly ILogger<GitLabService> _mockLogger;
    private readonly GitLabSettings _settings;

    public GitLabServiceTests()
    {
        _mockRepository = Substitute.For<IPullRequestRepository>();
        _mockLogger = Substitute.For<ILogger<GitLabService>>();

        _settings = new GitLabSettings
        {
            HostUrl = "https://gitlab.com",
            PersonalAccessToken = "test-token",
            Projects = new List<string> { "group/project1", "group/project2" },
            TargetBranches = new List<string> { "main", "develop" }
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
                Platform = "GitLab",
                Id = "1",
                Number = 123,
                Title = "Test MR 1",
                SourceBranch = new BranchName("feature/test"),
                TargetBranch = new BranchName("main"),
                CreatedAt = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                State = "merged",
                AuthorUsername = "testuser",
                RepositoryName = "group/project1"
            }
        };

        _mockRepository
            .GetPullRequestsAsync(
                Arg.Any<string>(),
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedPullRequests);

        var service = new GitLabService(_mockRepository, _settings, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expectedPullRequests);

        // 驗證 Repository 被正確呼叫
        await _mockRepository.Received(2).GetPullRequestsAsync(
            Arg.Any<string>(),
            Arg.Is<DateRange>(dr => dr.StartDate == dateRange.StartDate && dr.EndDate == dateRange.EndDate),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// 測試當沒有專案設定時拋出例外
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowException_WhenNoProjectsConfigured()
    {
        // Arrange
        var emptySettings = new GitLabSettings
        {
            HostUrl = "https://gitlab.com",
            PersonalAccessToken = "test-token",
            Projects = new List<string>() // 空專案清單
        };

        // Act
        Action act = () => new GitLabService(_mockRepository, emptySettings, _mockLogger);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*至少須設定一個 GitLab 專案*");
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
            .ThrowsAsync(new Exception("GitLab API 連線失敗"));

        var service = new GitLabService(_mockRepository, _settings, _mockLogger);

        // Act
        Func<Task> act = async () => await service.GetPullRequestsAsync(dateRange);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*GitLab API 連線失敗*");
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

        var service = new GitLabService(_mockRepository, _settings, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// 測試多個專案的 Pull Requests 合併結果
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_ShouldMergeResults_FromMultipleProjects()
    {
        // Arrange
        var dateRange = new DateRange(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        // 第一個專案回傳 1 筆
        _mockRepository
            .GetPullRequestsAsync(
                "group/project1",
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>
            {
                new()
                {
                    Platform = "GitLab",
                    Id = "1",
                    Number = 1,
                    Title = "MR from project1",
                    SourceBranch = new BranchName("feature/a"),
                    TargetBranch = new BranchName("main"),
                    CreatedAt = DateTime.UtcNow,
                    State = "merged",
                    AuthorUsername = "user1",
                    RepositoryName = "group/project1"
                }
            });

        // 第二個專案回傳 1 筆
        _mockRepository
            .GetPullRequestsAsync(
                "group/project2",
                Arg.Any<DateRange>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>
            {
                new()
                {
                    Platform = "GitLab",
                    Id = "2",
                    Number = 2,
                    Title = "MR from project2",
                    SourceBranch = new BranchName("feature/b"),
                    TargetBranch = new BranchName("main"),
                    CreatedAt = DateTime.UtcNow,
                    State = "opened",
                    AuthorUsername = "user2",
                    RepositoryName = "group/project2"
                }
            });

        var service = new GitLabService(_mockRepository, _settings, _mockLogger);

        // Act
        var result = await service.GetPullRequestsAsync(dateRange);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(pr => pr.RepositoryName == "group/project1");
        result.Should().Contain(pr => pr.RepositoryName == "group/project2");
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

        var service = new GitLabService(_mockRepository, _settings, _mockLogger);

        // Act
        await service.GetPullRequestsAsync(dateRange, cancellationToken);

        // Assert
        await _mockRepository.Received().GetPullRequestsAsync(
            Arg.Any<string>(),
            Arg.Any<DateRange>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }
}
