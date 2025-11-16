using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Console.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Services;
using FluentAssertions;

namespace ReleaseSync.Console.UnitTests.Services;

/// <summary>
/// WorkItemEnricher 單元測試
/// </summary>
public class WorkItemEnricherTests
{
    private readonly ILogger<WorkItemEnricher> _mockLogger;
    private readonly IWorkItemService _mockWorkItemService;
    private readonly IWorkItemIdParser _mockWorkItemIdParser;

    public WorkItemEnricherTests()
    {
        _mockLogger = Substitute.For<ILogger<WorkItemEnricher>>();
        _mockWorkItemService = Substitute.For<IWorkItemService>();
        _mockWorkItemIdParser = Substitute.For<IWorkItemIdParser>();
    }

    /// <summary>
    /// 測試成功抓取 Work Item 的情況
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Keep_PR_When_WorkItem_Found()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, _mockWorkItemService, _mockWorkItemIdParser);

        var workItem = CreateWorkItemInfo(12345, "Test Work Item");
        _mockWorkItemService.GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkItemInfo?>(workItem));

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto("feature/VSTS12345-test")
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().HaveCount(1);
        result.PullRequests[0].AssociatedWorkItem.Should().NotBeNull();
        result.PullRequests[0].AssociatedWorkItem!.Id.Should().Be(12345);
    }

    /// <summary>
    /// 測試 VSTS000000 開頭的佔位符情況 - PR 應該保留但 WorkItem 為 null
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Keep_PR_With_Null_WorkItem_When_Branch_Is_Placeholder()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, _mockWorkItemService, _mockWorkItemIdParser);

        var placeholderId = new WorkItemId(0);
        _mockWorkItemIdParser.TryParseWorkItemId(
            Arg.Any<BranchName>(),
            out Arg.Any<WorkItemId>())
            .Returns(x =>
            {
                x[1] = placeholderId;
                return true;
            });

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto("feature/VSTS000000-test")
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().HaveCount(1);
        result.PullRequests[0].AssociatedWorkItem.Should().BeNull();
    }

    /// <summary>
    /// 測試 Work Item 抓取失敗 (API 返回 null) 的情況 - PR 應該被移除
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Remove_PR_When_WorkItem_Fetch_Fails()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, _mockWorkItemService, _mockWorkItemIdParser);

        _mockWorkItemService.GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkItemInfo?>(null));

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto("feature/VSTS99999-test")
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().BeEmpty();
    }

    /// <summary>
    /// 測試混合情況 - 成功、佔位符、失敗
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Handle_Mixed_Scenarios()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, _mockWorkItemService, _mockWorkItemIdParser);

        var successBranch = new BranchName("feature/VSTS12345-success");
        var placeholderBranch = new BranchName("feature/VSTS000000-placeholder");
        var failureBranch = new BranchName("feature/VSTS99999-failure");

        // 設定 Parser 行為
        _mockWorkItemIdParser.TryParseWorkItemId(
            Arg.Is<BranchName>(b => b.Value == placeholderBranch.Value),
            out Arg.Any<WorkItemId>())
            .Returns(x =>
            {
                x[1] = new WorkItemId(0);
                return true;
            });

        _mockWorkItemIdParser.TryParseWorkItemId(
            Arg.Is<BranchName>(b => b.Value != placeholderBranch.Value),
            out Arg.Any<WorkItemId>())
            .Returns(x =>
            {
                x[1] = null!;
                return false;
            });

        // 設定 Service 行為
        _mockWorkItemService.GetWorkItemFromBranchAsync(
            Arg.Is<BranchName>(b => b.Value == successBranch.Value),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkItemInfo?>(CreateWorkItemInfo(12345, "Success Work Item")));

        _mockWorkItemService.GetWorkItemFromBranchAsync(
            Arg.Is<BranchName>(b => b.Value == failureBranch.Value),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkItemInfo?>(null));

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto(successBranch.Value),
            CreatePullRequestDto(placeholderBranch.Value),
            CreatePullRequestDto(failureBranch.Value)
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().HaveCount(2); // 成功 + 佔位符
        result.PullRequests.Should().Contain(p => p.SourceBranch == successBranch.Value);
        result.PullRequests.Should().Contain(p => p.SourceBranch == placeholderBranch.Value);
        result.PullRequests.Should().NotContain(p => p.SourceBranch == failureBranch.Value);

        // 驗證成功的 PR 有 WorkItem
        var successPr = result.PullRequests.First(p => p.SourceBranch == successBranch.Value);
        successPr.AssociatedWorkItem.Should().NotBeNull();
        successPr.AssociatedWorkItem!.Id.Should().Be(12345);

        // 驗證佔位符的 PR 沒有 WorkItem
        var placeholderPr = result.PullRequests.First(p => p.SourceBranch == placeholderBranch.Value);
        placeholderPr.AssociatedWorkItem.Should().BeNull();
    }

    /// <summary>
    /// 測試當 WorkItemService 未設定時應跳過整合
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Skip_When_WorkItemService_Is_Null()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, null, null);

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto("feature/VSTS12345-test")
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().HaveCount(1);
        result.PullRequests[0].AssociatedWorkItem.Should().BeNull();
    }

    /// <summary>
    /// 測試當 WorkItemIdParser 未設定時,仍然可以運作
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Work_Without_WorkItemIdParser()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, _mockWorkItemService, null);

        var workItem = CreateWorkItemInfo(12345, "Test Work Item");
        _mockWorkItemService.GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkItemInfo?>(workItem));

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto("feature/VSTS12345-test")
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().HaveCount(1);
        result.PullRequests[0].AssociatedWorkItem.Should().NotBeNull();
    }

    /// <summary>
    /// 測試當抓取 Work Item 發生例外時,PR 應該被移除
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Remove_PR_When_Exception_Occurs()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, _mockWorkItemService, _mockWorkItemIdParser);

        _mockWorkItemService.GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns<WorkItemInfo?>(_ => throw new Exception("Network error"));

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto("feature/VSTS12345-test")
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().BeEmpty();
    }

    /// <summary>
    /// 測試 Work Item 服務返回佔位符 Work Item (ID=0) 的情況
    /// </summary>
    [Fact]
    public async Task EnrichAsync_Should_Keep_PR_With_Null_WorkItem_When_Service_Returns_Placeholder()
    {
        // Arrange
        var enricher = new WorkItemEnricher(_mockLogger, _mockWorkItemService, _mockWorkItemIdParser);

        var placeholderWorkItem = new WorkItemInfo
        {
            Id = new WorkItemId(0),
            Title = "Placeholder",
            Type = "Placeholder",
            State = "N/A",
            AssignedTo = "N/A",
            Team = "N/A",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockWorkItemService.GetWorkItemFromBranchAsync(
            Arg.Any<BranchName>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkItemInfo?>(placeholderWorkItem));

        var result = CreateSyncResultDto(new List<PullRequestDto>
        {
            CreatePullRequestDto("feature/VSTS000000-test")
        });

        // Act
        await enricher.EnrichAsync(result);

        // Assert
        result.PullRequests.Should().HaveCount(1);
        result.PullRequests[0].AssociatedWorkItem.Should().BeNull();
    }

    private static SyncResultDto CreateSyncResultDto(List<PullRequestDto> pullRequests)
    {
        return new SyncResultDto
        {
            SyncStartedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            IsFullySuccessful = true,
            IsPartiallySuccessful = true,
            TotalPullRequestCount = pullRequests.Count,
            LinkedWorkItemCount = 0,
            PullRequests = pullRequests,
            PlatformStatuses = new List<PlatformStatusDto>()
        };
    }

    private static PullRequestDto CreatePullRequestDto(string sourceBranch)
    {
        return new PullRequestDto
        {
            Platform = "AzureDevOps",
            Number = 1,
            Title = "Test PR",
            SourceBranch = sourceBranch,
            TargetBranch = "main",
            CreatedAt = DateTime.UtcNow,
            State = "Active",
            RepositoryName = "TestRepo"
        };
    }

    private static WorkItemInfo CreateWorkItemInfo(int id, string title)
    {
        return new WorkItemInfo
        {
            Id = new WorkItemId(id),
            Title = title,
            Type = "User Story",
            State = "Active",
            AssignedTo = "Test User",
            Team = "Test Team",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
