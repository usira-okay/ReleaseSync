namespace ReleaseSync.Application.UnitTests.Services.CommitFetchers;

using Moq;
using ReleaseSync.Application.Services.CommitFetchers;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using Xunit;

/// <summary>
/// DateRangeFetcher 測試
/// </summary>
public class DateRangeFetcherTests
{
    private readonly Mock<IPullRequestRepository> _mockRepository;

    public DateRangeFetcherTests()
    {
        _mockRepository = new Mock<IPullRequestRepository>();
    }

    /// <summary>
    /// FetchCommitsAsync 應使用 DateRange 抓取 PR
    /// </summary>
    [Fact]
    public async Task FetchCommitsAsync_ShouldUseDateRangeToFetchPullRequests()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var dateRange = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        var expectedPRs = new List<PullRequestInfo>
        {
            new PullRequestInfo
            {
                RepositoryName = repositoryName,
                Title = "PR 1",
                MergedAt = new DateTime(2026, 1, 15)
            },
            new PullRequestInfo
            {
                RepositoryName = repositoryName,
                Title = "PR 2",
                MergedAt = new DateTime(2026, 1, 20)
            }
        };

        _mockRepository
            .Setup(r => r.GetPullRequestsAsync(
                repositoryName,
                dateRange,
                It.Is<IEnumerable<string>>(branches => branches.Contains(targetBranch.Value)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPRs);

        var fetcher = new DateRangeFetcher(
            _mockRepository.Object,
            repositoryName,
            targetBranch,
            dateRange);

        // Act
        var result = await fetcher.FetchCommitsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repositoryName, result.RepositoryName);
        Assert.Equal(2, result.PullRequests.Count());

        _mockRepository.Verify(
            r => r.GetPullRequestsAsync(
                repositoryName,
                dateRange,
                It.Is<IEnumerable<string>>(branches => branches.Contains(targetBranch.Value)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// FetchCommitsAsync 應在沒有 PR 時返回空結果
    /// </summary>
    [Fact]
    public async Task FetchCommitsAsync_WithNoPullRequests_ShouldReturnEmptyResult()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var dateRange = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        _mockRepository
            .Setup(r => r.GetPullRequestsAsync(
                It.IsAny<string>(),
                It.IsAny<DateRange>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PullRequestInfo>());

        var fetcher = new DateRangeFetcher(
            _mockRepository.Object,
            repositoryName,
            targetBranch,
            dateRange);

        // Act
        var result = await fetcher.FetchCommitsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.PullRequests);
    }

    /// <summary>
    /// 建構子應拒絕 null 的 repository
    /// </summary>
    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var dateRange = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DateRangeFetcher(null!, repositoryName, targetBranch, dateRange));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 repository name
    /// </summary>
    [Fact]
    public void Constructor_WithNullRepositoryName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var targetBranch = new BranchName("master");
        var dateRange = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DateRangeFetcher(_mockRepository.Object, null!, targetBranch, dateRange));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 target branch
    /// </summary>
    [Fact]
    public void Constructor_WithNullTargetBranch_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var dateRange = new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DateRangeFetcher(_mockRepository.Object, repositoryName, null!, dateRange));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 date range
    /// </summary>
    [Fact]
    public void Constructor_WithNullDateRange_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DateRangeFetcher(_mockRepository.Object, repositoryName, targetBranch, null!));
    }
}
