using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Platforms.AzureDevOps;
using FluentAssertions;

namespace ReleaseSync.Infrastructure.UnitTests.Platforms;

/// <summary>
/// AzureDevOpsService 單元測試
/// </summary>
public class AzureDevOpsServiceTests
{
    private readonly IWorkItemRepository _mockRepository;
    private readonly IWorkItemIdParser _mockParser;
    private readonly ILogger<AzureDevOpsService> _mockLogger;
    private readonly AzureDevOpsService _service;

    public AzureDevOpsServiceTests()
    {
        _mockRepository = Substitute.For<IWorkItemRepository>();
        _mockParser = Substitute.For<IWorkItemIdParser>();
        _mockLogger = Substitute.For<ILogger<AzureDevOpsService>>();
        _service = new AzureDevOpsService(_mockRepository, _mockParser, _mockLogger);
    }

    #region GetWorkItemAsync Tests

    /// <summary>
    /// 測試成功取得 Work Item
    /// </summary>
    [Fact]
    public async Task GetWorkItemAsync_Should_Return_WorkItem_When_Exists()
    {
        // Arrange
        var workItemId = new WorkItemId(12345);
        var expectedWorkItem = new WorkItemInfo
        {
            Id = workItemId,
            Title = "Test User Story",
            Type = "User Story",
            State = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepository.GetWorkItemAsync(workItemId, true, Arg.Any<CancellationToken>())
            .Returns(expectedWorkItem);

        // Act
        var result = await _service.GetWorkItemAsync(workItemId, includeParent: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedWorkItem);
        result!.Id.Value.Should().Be(12345);
        result.Title.Should().Be("Test User Story");
        result.Type.Should().Be("User Story");
        result.State.Should().Be("Active");
    }

    /// <summary>
    /// 測試當 Work Item 不存在時回傳 null
    /// </summary>
    [Fact]
    public async Task GetWorkItemAsync_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var workItemId = new WorkItemId(99999);

        _mockRepository.GetWorkItemAsync(workItemId, true, Arg.Any<CancellationToken>())
            .Returns((WorkItemInfo?)null);

        // Act
        var result = await _service.GetWorkItemAsync(workItemId, includeParent: true);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 測試當 Repository 拋出例外時應回傳 null 且記錄錯誤
    /// </summary>
    [Fact]
    public async Task GetWorkItemAsync_Should_Return_Null_And_Log_Error_When_Exception_Occurs()
    {
        // Arrange
        var workItemId = new WorkItemId(12345);
        var expectedException = new HttpRequestException("Network error");

        _mockRepository.GetWorkItemAsync(workItemId, true, Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var result = await _service.GetWorkItemAsync(workItemId, includeParent: true);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 測試 includeParent 參數正確傳遞給 Repository
    /// </summary>
    [Fact]
    public async Task GetWorkItemAsync_Should_Pass_IncludeParent_Parameter_To_Repository()
    {
        // Arrange
        var workItemId = new WorkItemId(12345);
        var workItem = new WorkItemInfo
        {
            Id = workItemId,
            Title = "Test",
            Type = "Bug",
            State = "New",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepository.GetWorkItemAsync(workItemId, false, Arg.Any<CancellationToken>())
            .Returns(workItem);

        // Act
        await _service.GetWorkItemAsync(workItemId, includeParent: false);

        // Assert
        await _mockRepository.Received(1).GetWorkItemAsync(
            workItemId,
            false,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetWorkItemFromBranchAsync Tests

    /// <summary>
    /// 測試從 Branch 名稱成功解析並取得 Work Item
    /// </summary>
    [Fact]
    public async Task GetWorkItemFromBranchAsync_Should_Parse_And_Return_WorkItem()
    {
        // Arrange
        var branchName = new BranchName("feature/12345-add-feature");
        var workItemId = new WorkItemId(12345);
        var workItem = new WorkItemInfo
        {
            Id = workItemId,
            Title = "Add new feature",
            Type = "User Story",
            State = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow
        };

        _mockParser.TryParseWorkItemId(branchName, out Arg.Any<WorkItemId>())
            .Returns(callInfo =>
            {
                callInfo[1] = workItemId;
                return true;
            });

        _mockRepository.GetWorkItemAsync(workItemId, true, Arg.Any<CancellationToken>())
            .Returns(workItem);

        // Act
        var result = await _service.GetWorkItemFromBranchAsync(branchName, includeParent: true);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Value.Should().Be(12345);
        result.Title.Should().Be("Add new feature");
    }

    /// <summary>
    /// 測試當無法從 Branch 名稱解析 Work Item ID 時回傳 null
    /// </summary>
    [Fact]
    public async Task GetWorkItemFromBranchAsync_Should_Return_Null_When_Parse_Fails()
    {
        // Arrange
        var branchName = new BranchName("main");

        _mockParser.TryParseWorkItemId(branchName, out Arg.Any<WorkItemId>())
            .Returns(callInfo =>
            {
                callInfo[1] = null;
                return false;
            });

        // Act
        var result = await _service.GetWorkItemFromBranchAsync(branchName, includeParent: true);

        // Assert
        result.Should().BeNull();
        await _mockRepository.DidNotReceive().GetWorkItemAsync(
            Arg.Any<WorkItemId>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// 測試解析成功但 Work Item 不存在時回傳 null
    /// </summary>
    [Fact]
    public async Task GetWorkItemFromBranchAsync_Should_Return_Null_When_WorkItem_Not_Found()
    {
        // Arrange
        var branchName = new BranchName("feature/99999-non-existent");
        var workItemId = new WorkItemId(99999);

        _mockParser.TryParseWorkItemId(branchName, out Arg.Any<WorkItemId>())
            .Returns(callInfo =>
            {
                callInfo[1] = workItemId;
                return true;
            });

        _mockRepository.GetWorkItemAsync(workItemId, true, Arg.Any<CancellationToken>())
            .Returns((WorkItemInfo?)null);

        // Act
        var result = await _service.GetWorkItemFromBranchAsync(branchName, includeParent: true);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetWorkItemsAsync Tests

    /// <summary>
    /// 測試批次取得多個 Work Items
    /// </summary>
    [Fact]
    public async Task GetWorkItemsAsync_Should_Return_Multiple_WorkItems()
    {
        // Arrange
        var workItemIds = new List<WorkItemId>
        {
            new WorkItemId(1001),
            new WorkItemId(1002),
            new WorkItemId(1003)
        };

        var expectedWorkItems = new List<WorkItemInfo>
        {
            new WorkItemInfo
            {
                Id = new WorkItemId(1001),
                Title = "Work Item 1",
                Type = "User Story",
                State = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            },
            new WorkItemInfo
            {
                Id = new WorkItemId(1002),
                Title = "Work Item 2",
                Type = "Bug",
                State = "Resolved",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow
            },
            new WorkItemInfo
            {
                Id = new WorkItemId(1003),
                Title = "Work Item 3",
                Type = "Task",
                State = "Closed",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockRepository.GetWorkItemsAsync(workItemIds, true, Arg.Any<CancellationToken>())
            .Returns(expectedWorkItems);

        // Act
        var result = await _service.GetWorkItemsAsync(workItemIds, includeParent: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(w => w.Id.Value).Should().Contain(new[] { 1001, 1002, 1003 });
    }

    /// <summary>
    /// 測試空的 Work Item ID 清單回傳空結果
    /// </summary>
    [Fact]
    public async Task GetWorkItemsAsync_Should_Return_Empty_When_Input_Is_Empty()
    {
        // Arrange
        var emptyWorkItemIds = new List<WorkItemId>();

        _mockRepository.GetWorkItemsAsync(emptyWorkItemIds, true, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<WorkItemInfo>());

        // Act
        var result = await _service.GetWorkItemsAsync(emptyWorkItemIds, includeParent: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// 測試當 Repository 拋出例外時回傳空集合
    /// </summary>
    [Fact]
    public async Task GetWorkItemsAsync_Should_Return_Empty_And_Log_Error_When_Exception_Occurs()
    {
        // Arrange
        var workItemIds = new List<WorkItemId>
        {
            new WorkItemId(1001),
            new WorkItemId(1002)
        };

        var expectedException = new HttpRequestException("API Error");

        _mockRepository.GetWorkItemsAsync(workItemIds, true, Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var result = await _service.GetWorkItemsAsync(workItemIds, includeParent: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// 測試 includeParent 參數正確傳遞給 Repository
    /// </summary>
    [Fact]
    public async Task GetWorkItemsAsync_Should_Pass_IncludeParent_Parameter_To_Repository()
    {
        // Arrange
        var workItemIds = new List<WorkItemId>
        {
            new WorkItemId(1001)
        };

        var workItems = new List<WorkItemInfo>
        {
            new WorkItemInfo
            {
                Id = new WorkItemId(1001),
                Title = "Test",
                Type = "Task",
                State = "New",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockRepository.GetWorkItemsAsync(workItemIds, false, Arg.Any<CancellationToken>())
            .Returns(workItems);

        // Act
        await _service.GetWorkItemsAsync(workItemIds, includeParent: false);

        // Assert
        await _mockRepository.Received(1).GetWorkItemsAsync(
            workItemIds,
            false,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// 測試建構子驗證參數
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_When_Repository_Is_Null()
    {
        // Act & Assert
        var act = () => new AzureDevOpsService(
            null!,
            _mockParser,
            _mockLogger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    /// <summary>
    /// 測試建構子驗證 Parser 參數
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_When_Parser_Is_Null()
    {
        // Act & Assert
        var act = () => new AzureDevOpsService(
            _mockRepository,
            null!,
            _mockLogger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("parser");
    }

    /// <summary>
    /// 測試建構子驗證 Logger 參數
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        var act = () => new AzureDevOpsService(
            _mockRepository,
            _mockParser,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
