using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleaseSync.Application.Services;
using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Platforms;
using FluentAssertions;

namespace ReleaseSync.Infrastructure.UnitTests.Platforms;

/// <summary>
/// BasePullRequestRepository 單元測試
/// </summary>
public class BasePullRequestRepositoryTests
{
    private readonly ILogger _mockLogger;
    private readonly IUserMappingService _mockUserMappingService;

    public BasePullRequestRepositoryTests()
    {
        _mockLogger = Substitute.For<ILogger>();
        _mockUserMappingService = Substitute.For<IUserMappingService>();
    }

    /// <summary>
    /// 測試當 UserMapping 未啟用時，應保留所有 PR
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_Should_Keep_All_PRs_When_UserMapping_Not_Enabled()
    {
        // Arrange
        _mockUserMappingService.IsFilteringEnabled().Returns(false);

        var repository = new TestPullRequestRepository(_mockLogger, _mockUserMappingService);
        repository.SetupApiResponse(new List<TestApiDto>
        {
            CreateTestApiDto("user1", "User 1"),
            CreateTestApiDto("user2", "User 2"),
            CreateTestApiDto("user3", "User 3")
        });

        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Act
        var result = await repository.GetPullRequestsAsync("TestProject", dateRange);

        // Assert
        result.Should().HaveCount(3);
    }

    /// <summary>
    /// 測試當 UserMapping 啟用時，應過濾不在清單中的使用者
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_Should_Filter_Users_Not_In_Mapping()
    {
        // Arrange
        _mockUserMappingService.IsFilteringEnabled().Returns(true);
        _mockUserMappingService.HasMapping("TestPlatform", "user1").Returns(true);
        _mockUserMappingService.HasMapping("TestPlatform", "user2").Returns(false);
        _mockUserMappingService.HasMapping("TestPlatform", "user3").Returns(true);

        var repository = new TestPullRequestRepository(_mockLogger, _mockUserMappingService);
        repository.SetupApiResponse(new List<TestApiDto>
        {
            CreateTestApiDto("user1", "User 1"),
            CreateTestApiDto("user2", "User 2"),
            CreateTestApiDto("user3", "User 3")
        });

        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Act
        var result = await repository.GetPullRequestsAsync("TestProject", dateRange);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(pr => pr.AuthorUserId == "user1");
        result.Should().Contain(pr => pr.AuthorUserId == "user3");
        result.Should().NotContain(pr => pr.AuthorUserId == "user2");
    }

    /// <summary>
    /// 測試當所有使用者都不在 UserMapping 清單中時，應返回空集合
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_Should_Return_Empty_When_No_Users_In_Mapping()
    {
        // Arrange
        _mockUserMappingService.IsFilteringEnabled().Returns(true);
        _mockUserMappingService.HasMapping("TestPlatform", Arg.Any<string>()).Returns(false);

        var repository = new TestPullRequestRepository(_mockLogger, _mockUserMappingService);
        repository.SetupApiResponse(new List<TestApiDto>
        {
            CreateTestApiDto("user1", "User 1"),
            CreateTestApiDto("user2", "User 2")
        });

        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Act
        var result = await repository.GetPullRequestsAsync("TestProject", dateRange);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// 測試當所有使用者都在 UserMapping 清單中時，應保留所有 PR
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_Should_Keep_All_PRs_When_All_Users_In_Mapping()
    {
        // Arrange
        _mockUserMappingService.IsFilteringEnabled().Returns(true);
        _mockUserMappingService.HasMapping("TestPlatform", Arg.Any<string>()).Returns(true);

        var repository = new TestPullRequestRepository(_mockLogger, _mockUserMappingService);
        repository.SetupApiResponse(new List<TestApiDto>
        {
            CreateTestApiDto("user1", "User 1"),
            CreateTestApiDto("user2", "User 2"),
            CreateTestApiDto("user3", "User 3")
        });

        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Act
        var result = await repository.GetPullRequestsAsync("TestProject", dateRange);

        // Assert
        result.Should().HaveCount(3);
    }

    /// <summary>
    /// 測試當 PR 作者 ID 為 null 時的處理
    /// </summary>
    [Fact]
    public async Task GetPullRequestsAsync_Should_Filter_PR_With_Null_AuthorUserId()
    {
        // Arrange
        _mockUserMappingService.IsFilteringEnabled().Returns(true);
        _mockUserMappingService.HasMapping("TestPlatform", (string?)null).Returns(false);
        _mockUserMappingService.HasMapping("TestPlatform", "user1").Returns(true);

        var repository = new TestPullRequestRepository(_mockLogger, _mockUserMappingService);
        repository.SetupApiResponse(new List<TestApiDto>
        {
            CreateTestApiDto("user1", "User 1"),
            CreateTestApiDto(null, "Unknown User")
        });

        var dateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Act
        var result = await repository.GetPullRequestsAsync("TestProject", dateRange);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(pr => pr.AuthorUserId == "user1");
    }

    private static TestApiDto CreateTestApiDto(string? authorId, string authorName)
    {
        return new TestApiDto
        {
            AuthorId = authorId,
            AuthorName = authorName,
            Title = $"PR by {authorName}",
            Number = new Random().Next(1, 1000)
        };
    }

    /// <summary>
    /// 測試用的 API DTO
    /// </summary>
    private class TestApiDto
    {
        public string? AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Number { get; set; }
    }

    /// <summary>
    /// 測試用的 PullRequest Repository 實作
    /// </summary>
    private class TestPullRequestRepository : BasePullRequestRepository<TestApiDto>
    {
        private List<TestApiDto> _apiResponse = new();

        protected override string PlatformName => "TestPlatform";

        public TestPullRequestRepository(
            ILogger logger,
            IUserMappingService userMappingService)
            : base(logger, userMappingService)
        {
        }

        public void SetupApiResponse(List<TestApiDto> apiResponse)
        {
            _apiResponse = apiResponse;
        }

        protected override Task<IEnumerable<TestApiDto>> FetchPullRequestsFromApiAsync(
            string projectName,
            DateRange dateRange,
            IEnumerable<string>? targetBranches,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<TestApiDto>>(_apiResponse);
        }

        protected override PullRequestInfo ConvertToPullRequestInfo(TestApiDto apiDto, string projectName)
        {
            return new PullRequestInfo
            {
                Platform = PlatformName,
                Id = apiDto.Number.ToString(),
                Number = apiDto.Number,
                Title = apiDto.Title,
                Description = null,
                SourceBranch = new BranchName("feature/test"),
                TargetBranch = new BranchName("main"),
                CreatedAt = DateTime.UtcNow,
                MergedAt = null,
                State = "Active",
                AuthorUserId = apiDto.AuthorId,
                AuthorDisplayName = apiDto.AuthorName,
                RepositoryName = projectName,
                Url = null
            };
        }
    }
}
