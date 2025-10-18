namespace ReleaseSync.Application.UnitTests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;

/// <summary>
/// SyncOrchestrator 單元測試
/// 測試多平台協調邏輯與部分失敗處理
/// </summary>
public class SyncOrchestratorTests
{
    private readonly ILogger<SyncOrchestrator> _mockLogger;

    public SyncOrchestratorTests()
    {
        _mockLogger = Substitute.For<ILogger<SyncOrchestrator>>();
    }

    /// <summary>
    /// 測試成功同步單一平台
    /// </summary>
    [Fact]
    public async Task SyncAsync_ShouldReturnSuccessResult_WhenSinglePlatformSucceeds()
    {
        // Arrange
        var mockGitLabService = CreateMockPlatformService("GitLab", pullRequestCount: 5);
        var orchestrator = new SyncOrchestrator(
            gitLabService: mockGitLabService,
            bitBucketService: null,
            azureDevOpsService: null,
            workItemIdParser: null,
            logger: _mockLogger
        );

        var request = new SyncRequest
        {
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            EnableGitLab = true,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue();
        result.TotalPullRequestCount.Should().Be(5);
        result.PlatformStatuses.Should().HaveCount(1);
        result.PlatformStatuses.First().PlatformName.Should().Be("GitLab");
        result.PlatformStatuses.First().IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// 測試成功同步多個平台
    /// </summary>
    [Fact]
    public async Task SyncAsync_ShouldMergeResults_WhenMultiplePlatformsSucceed()
    {
        // Arrange
        var mockGitLabService = CreateMockPlatformService("GitLab", pullRequestCount: 3);
        var mockBitBucketService = CreateMockPlatformService("BitBucket", pullRequestCount: 2);

        var orchestrator = new SyncOrchestrator(
            gitLabService: mockGitLabService,
            bitBucketService: mockBitBucketService,
            azureDevOpsService: null,
            workItemIdParser: null,
            logger: _mockLogger
        );

        var request = new SyncRequest
        {
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            EnableGitLab = true,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeTrue();
        result.TotalPullRequestCount.Should().Be(5); // 3 + 2
        result.PlatformStatuses.Should().HaveCount(2);
        result.PlatformStatuses.Should().Contain(s => s.PlatformName == "GitLab" && s.IsSuccess);
        result.PlatformStatuses.Should().Contain(s => s.PlatformName == "BitBucket" && s.IsSuccess);
    }

    /// <summary>
    /// 測試部分平台失敗時,成功的平台仍繼續執行
    /// </summary>
    [Fact]
    public async Task SyncAsync_ShouldContinueOtherPlatforms_WhenOnePlatformFails()
    {
        // Arrange
        var mockGitLabService = CreateMockPlatformService("GitLab", pullRequestCount: 3);
        var mockBitBucketService = CreateFailingPlatformService("BitBucket API 錯誤");

        var orchestrator = new SyncOrchestrator(
            gitLabService: mockGitLabService,
            bitBucketService: mockBitBucketService,
            azureDevOpsService: null,
            workItemIdParser: null,
            logger: _mockLogger
        );

        var request = new SyncRequest
        {
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            EnableGitLab = true,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse(); // 不是完全成功
        result.IsPartiallySuccessful.Should().BeTrue(); // 部分成功
        result.TotalPullRequestCount.Should().Be(3); // 只有 GitLab 的 3 筆
        result.PlatformStatuses.Should().HaveCount(2);

        // GitLab 成功
        result.PlatformStatuses.Should().Contain(s =>
            s.PlatformName == "GitLab" && s.IsSuccess && s.PullRequestCount == 3);

        // BitBucket 失敗
        result.PlatformStatuses.Should().Contain(s =>
            s.PlatformName == "BitBucket" && !s.IsSuccess && s.ErrorMessage.Contains("BitBucket API 錯誤"));
    }

    /// <summary>
    /// 測試所有平台失敗時的處理
    /// </summary>
    [Fact]
    public async Task SyncAsync_ShouldReturnFailureResult_WhenAllPlatformsFail()
    {
        // Arrange
        var mockGitLabService = CreateFailingPlatformService("GitLab 連線失敗");
        var mockBitBucketService = CreateFailingPlatformService("BitBucket 連線失敗");

        var orchestrator = new SyncOrchestrator(
            gitLabService: mockGitLabService,
            bitBucketService: mockBitBucketService,
            azureDevOpsService: null,
            workItemIdParser: null,
            logger: _mockLogger
        );

        var request = new SyncRequest
        {
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            EnableGitLab = true,
            EnableBitBucket = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsFullySuccessful.Should().BeFalse();
        result.IsPartiallySuccessful.Should().BeFalse(); // 沒有任何平台成功
        result.TotalPullRequestCount.Should().Be(0);
        result.PlatformStatuses.Should().HaveCount(2);
        result.PlatformStatuses.Should().OnlyContain(s => !s.IsSuccess);
    }

    /// <summary>
    /// 測試未啟用任何平台時拋出例外
    /// </summary>
    [Fact]
    public async Task SyncAsync_ShouldThrowException_WhenNoPlatformsEnabled()
    {
        // Arrange
        var orchestrator = new SyncOrchestrator(
            gitLabService: null,
            bitBucketService: null,
            azureDevOpsService: null,
            workItemIdParser: null,
            logger: _mockLogger
        );

        var request = new SyncRequest
        {
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            EnableGitLab = false,
            EnableBitBucket = false,
            EnableAzureDevOps = false
        };

        // Act
        Func<Task> act = async () => await orchestrator.SyncAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*至少須啟用一個平台*");
    }

    /// <summary>
    /// 測試 CancellationToken 正確傳遞至平台服務
    /// </summary>
    [Fact]
    public async Task SyncAsync_ShouldPassCancellationToken_ToPlatformServices()
    {
        // Arrange
        var mockGitLabService = CreateMockPlatformService("GitLab", pullRequestCount: 1);
        var orchestrator = new SyncOrchestrator(
            gitLabService: mockGitLabService,
            bitBucketService: null,
            azureDevOpsService: null,
            workItemIdParser: null,
            logger: _mockLogger
        );

        var request = new SyncRequest
        {
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            EnableGitLab = true
        };

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Act
        await orchestrator.SyncAsync(request, cancellationToken);

        // Assert
        await mockGitLabService.Received().GetPullRequestsAsync(
            Arg.Any<DateRange>(),
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    /// <summary>
    /// 測試日期範圍驗證
    /// </summary>
    [Fact]
    public async Task SyncAsync_ShouldThrowException_WhenDateRangeIsInvalid()
    {
        // Arrange
        var mockGitLabService = CreateMockPlatformService("GitLab", pullRequestCount: 1);
        var orchestrator = new SyncOrchestrator(
            gitLabService: mockGitLabService,
            bitBucketService: null,
            azureDevOpsService: null,
            workItemIdParser: null,
            logger: _mockLogger
        );

        var request = new SyncRequest
        {
            StartDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), // 晚於 EndDate
            EndDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EnableGitLab = true
        };

        // Act
        Func<Task> act = async () => await orchestrator.SyncAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*不能晚於結束日期*");
    }

    // ========== Helper Methods ==========

    /// <summary>
    /// 建立模擬的平台服務 (成功情境)
    /// </summary>
    private IPlatformService CreateMockPlatformService(string platformName, int pullRequestCount)
    {
        var mockService = Substitute.For<IPlatformService>();

        var pullRequests = Enumerable.Range(1, pullRequestCount)
            .Select(i => new PullRequestInfo
            {
                Platform = platformName,
                Id = $"{i}",
                Number = i,
                Title = $"Test PR {i}",
                SourceBranch = new BranchName($"feature/{i}"),
                TargetBranch = new BranchName("main"),
                CreatedAt = DateTime.UtcNow,
                State = "merged",
                AuthorUsername = $"user{i}",
                RepositoryName = $"repo/{i}"
            })
            .ToList();

        mockService
            .GetPullRequestsAsync(Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(pullRequests);

        return mockService;
    }

    /// <summary>
    /// 建立模擬的平台服務 (失敗情境)
    /// </summary>
    private IPlatformService CreateFailingPlatformService(string errorMessage)
    {
        var mockService = Substitute.For<IPlatformService>();

        mockService
            .GetPullRequestsAsync(Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception(errorMessage));

        return mockService;
    }
}

/// <summary>
/// 平台服務介面 (用於測試)
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// 取得 Pull Requests
    /// </summary>
    Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default);
}
