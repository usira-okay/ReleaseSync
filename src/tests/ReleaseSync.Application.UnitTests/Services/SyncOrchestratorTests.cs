namespace ReleaseSync.Application.UnitTests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Services;
using Xunit;

/// <summary>
/// SyncOrchestrator 單元測試
/// </summary>
public class SyncOrchestratorTests
{
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestratorTests()
    {
        _logger = Substitute.For<ILogger<SyncOrchestrator>>();
    }

    #region 建構子測試

    [Fact(DisplayName = "建構子: platformServices 為 null 應拋出例外")]
    public void Constructor_NullPlatformServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SyncOrchestrator(null!, _logger));

        exception.ParamName.Should().Be("platformServices");
    }

    [Fact(DisplayName = "建構子: logger 為 null 應拋出例外")]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var platformServices = Substitute.For<IEnumerable<IPlatformService>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SyncOrchestrator(platformServices, null!));

        exception.ParamName.Should().Be("logger");
    }

    [Fact(DisplayName = "建構子: workItemService 可為 null")]
    public void Constructor_NullWorkItemService_ShouldNotThrow()
    {
        // Arrange
        var platformServices = Substitute.For<IEnumerable<IPlatformService>>();

        // Act
        var orchestrator = new SyncOrchestrator(platformServices, _logger, workItemService: null);

        // Assert
        orchestrator.Should().NotBeNull();
    }

    #endregion

    #region SyncAsync 驗證測試

    [Fact(DisplayName = "SyncAsync: request 為 null 應拋出例外")]
    public async Task SyncAsync_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var platformServices = new List<IPlatformService>();
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            orchestrator.SyncAsync(null!));
    }

    [Fact(DisplayName = "SyncAsync: 未啟用任何平台應拋出例外")]
    public async Task SyncAsync_NoPlatformEnabled_ShouldThrowArgumentException()
    {
        // Arrange
        var platformServices = new List<IPlatformService>();
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = false,
            EnableBitBucket = false
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.SyncAsync(request));

        exception.Message.Should().Contain("至少須啟用一個平台");
    }

    [Fact(DisplayName = "SyncAsync: 起始日期晚於結束日期應拋出例外")]
    public async Task SyncAsync_InvalidDateRange_ShouldThrowArgumentException()
    {
        // Arrange
        var platformServices = new List<IPlatformService>();
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-7),
            EnableGitLab = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.SyncAsync(request));
    }

    #endregion

    #region 單一平台同步測試

    [Fact(DisplayName = "SyncAsync: GitLab 單一平台同步應成功")]
    public async Task SyncAsync_SinglePlatformGitLab_ShouldSucceed()
    {
        // Arrange
        var mockGitLabService = CreateMockPlatformService("GitLab", 5);
        var platformServices = new List<IPlatformService> { mockGitLabService };
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableBitBucket = false
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalPullRequestCount.Should().Be(5);
        result.IsFullySuccessful.Should().BeTrue();
        result.PlatformStatuses.Should().ContainSingle();
        result.PlatformStatuses.First().PlatformName.Should().Be("GitLab");
        result.PlatformStatuses.First().IsSuccess.Should().BeTrue();
        result.PlatformStatuses.First().PullRequestCount.Should().Be(5);
    }

    [Fact(DisplayName = "SyncAsync: BitBucket 單一平台同步應成功")]
    public async Task SyncAsync_SinglePlatformBitBucket_ShouldSucceed()
    {
        // Arrange
        var mockBitBucketService = CreateMockPlatformService("BitBucket", 3);
        var platformServices = new List<IPlatformService> { mockBitBucketService };
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = false,
            EnableBitBucket = true
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalPullRequestCount.Should().Be(3);
        result.IsFullySuccessful.Should().BeTrue();
        result.PlatformStatuses.Should().ContainSingle();
        result.PlatformStatuses.First().PlatformName.Should().Be("BitBucket");
    }

    #endregion

    #region 多平台同步測試

    [Fact(DisplayName = "SyncAsync: 多平台同步應並行執行並聚合結果")]
    public async Task SyncAsync_MultiplePlatforms_ShouldAggregateResults()
    {
        // Arrange
        var mockGitLabService = CreateMockPlatformService("GitLab", 5);
        var mockBitBucketService = CreateMockPlatformService("BitBucket", 3);
        var platformServices = new List<IPlatformService>
        {
            mockGitLabService,
            mockBitBucketService
        };
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableBitBucket = true
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalPullRequestCount.Should().Be(8); // 5 + 3
        result.IsFullySuccessful.Should().BeTrue();
        result.PlatformStatuses.Should().HaveCount(2);
        result.PlatformStatuses.Should().Contain(s => s.PlatformName == "GitLab" && s.IsSuccess);
        result.PlatformStatuses.Should().Contain(s => s.PlatformName == "BitBucket" && s.IsSuccess);
    }

    #endregion

    #region 錯誤處理測試

    [Fact(DisplayName = "SyncAsync: 單一平台失敗應記錄錯誤但不中斷流程")]
    public async Task SyncAsync_SinglePlatformFailure_ShouldRecordFailureAndContinue()
    {
        // Arrange
        var mockFailedService = CreateFailingPlatformService("GitLab", "API 連線失敗");
        var mockSuccessService = CreateMockPlatformService("BitBucket", 3);
        var platformServices = new List<IPlatformService>
        {
            mockFailedService,
            mockSuccessService
        };
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableBitBucket = true
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalPullRequestCount.Should().Be(3); // 只有 BitBucket 成功
        result.IsFullySuccessful.Should().BeFalse();
        result.IsPartiallySuccessful.Should().BeTrue();
        result.PlatformStatuses.Should().HaveCount(2);

        var failedStatus = result.PlatformStatuses.First(s => s.PlatformName == "GitLab");
        failedStatus.IsSuccess.Should().BeFalse();
        failedStatus.ErrorMessage.Should().Contain("API 連線失敗");

        var successStatus = result.PlatformStatuses.First(s => s.PlatformName == "BitBucket");
        successStatus.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "SyncAsync: 所有平台失敗應返回完全失敗狀態")]
    public async Task SyncAsync_AllPlatformsFailure_ShouldReturnFullyFailed()
    {
        // Arrange
        var mockFailedService1 = CreateFailingPlatformService("GitLab", "GitLab API 錯誤");
        var mockFailedService2 = CreateFailingPlatformService("BitBucket", "BitBucket API 錯誤");
        var platformServices = new List<IPlatformService>
        {
            mockFailedService1,
            mockFailedService2
        };
        var orchestrator = new SyncOrchestrator(platformServices, _logger);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableBitBucket = true
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalPullRequestCount.Should().Be(0);
        result.IsFullySuccessful.Should().BeFalse();
        result.IsPartiallySuccessful.Should().BeFalse();
        result.PlatformStatuses.Should().HaveCount(2);
        result.PlatformStatuses.Should().OnlyContain(s => !s.IsSuccess);
    }

    #endregion

    #region Work Item 整合測試

    [Fact(DisplayName = "SyncAsync: 啟用 AzureDevOps 應關聯 Work Items")]
    public async Task SyncAsync_WithAzureDevOpsEnabled_ShouldEnrichWithWorkItems()
    {
        // Arrange
        var mockPlatformService = CreateMockPlatformService("GitLab", 2);
        var mockWorkItemService = CreateMockWorkItemService();
        var platformServices = new List<IPlatformService> { mockPlatformService };
        var orchestrator = new SyncOrchestrator(platformServices, _logger, mockWorkItemService);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableAzureDevOps = true
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalPullRequestCount.Should().Be(2);
        result.LinkedWorkItemCount.Should().BeGreaterThan(0);

        // 驗證 Work Item Service 有被呼叫
        await mockWorkItemService.Received().GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "SyncAsync: 未啟用 AzureDevOps 不應呼叫 Work Item 服務")]
    public async Task SyncAsync_WithoutAzureDevOps_ShouldNotCallWorkItemService()
    {
        // Arrange
        var mockPlatformService = CreateMockPlatformService("GitLab", 2);
        var mockWorkItemService = CreateMockWorkItemService();
        var platformServices = new List<IPlatformService> { mockPlatformService };
        var orchestrator = new SyncOrchestrator(platformServices, _logger, mockWorkItemService);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableAzureDevOps = false
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.LinkedWorkItemCount.Should().Be(0);

        // 驗證 Work Item Service 沒有被呼叫
        await mockWorkItemService.DidNotReceive().GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "SyncAsync: Work Item Service 為 null 但啟用 AzureDevOps 應記錄警告")]
    public async Task SyncAsync_NullWorkItemServiceWithAzureDevOpsEnabled_ShouldLogWarning()
    {
        // Arrange
        var mockPlatformService = CreateMockPlatformService("GitLab", 2);
        var platformServices = new List<IPlatformService> { mockPlatformService };
        var orchestrator = new SyncOrchestrator(platformServices, _logger, workItemService: null);

        var request = new SyncRequest
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            EnableGitLab = true,
            EnableAzureDevOps = true
        };

        // Act
        var result = await orchestrator.SyncAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.LinkedWorkItemCount.Should().Be(0);

        // 驗證有記錄警告 (檢查 logger 被呼叫且 LogLevel 為 Warning)
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region 輔助方法

    /// <summary>
    /// 建立模擬的 Platform Service
    /// </summary>
    private IPlatformService CreateMockPlatformService(string platformName, int prCount)
    {
        var service = Substitute.For<IPlatformService>();
        service.PlatformName.Returns(platformName);

        var pullRequests = Enumerable.Range(1, prCount)
            .Select(i => new PullRequestInfo
            {
                Platform = platformName,
                Id = $"{platformName}-{i}",
                Number = i,
                Title = $"Test PR #{i}",
                SourceBranch = new BranchName($"feature/test-{i}"),
                TargetBranch = new BranchName("main"),
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                State = "Merged",
                AuthorUsername = "testuser",
                RepositoryName = "test/repo"
            })
            .ToList();

        service.GetPullRequestsAsync(
            Arg.Any<DateRange>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<PullRequestInfo>>(pullRequests));

        return service;
    }

    /// <summary>
    /// 建立會失敗的 Platform Service
    /// </summary>
    private IPlatformService CreateFailingPlatformService(string platformName, string errorMessage)
    {
        var service = Substitute.For<IPlatformService>();
        service.PlatformName.Returns(platformName);

        service.GetPullRequestsAsync(
            Arg.Any<DateRange>(),
            Arg.Any<CancellationToken>())
            .Returns<IEnumerable<PullRequestInfo>>(_ => throw new Exception(errorMessage));

        return service;
    }

    /// <summary>
    /// 建立模擬的 Work Item Service
    /// </summary>
    private IWorkItemService CreateMockWorkItemService()
    {
        var service = Substitute.For<IWorkItemService>();

        var mockWorkItem = new WorkItemInfo
        {
            Id = new WorkItemId(1234),
            Title = "Test Work Item",
            Type = "User Story",
            State = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        service.GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkItemInfo?>(mockWorkItem));

        return service;
    }

    #endregion
}
