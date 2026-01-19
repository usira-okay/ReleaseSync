namespace ReleaseSync.Application.UnitTests.Services.CommitFetchers;

using Moq;
using ReleaseSync.Application.Services.CommitFetchers;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using Xunit;

/// <summary>
/// HistoricalReleaseFetcher 測試
/// 場景：舊版 Release Branch ↔ 新版 Release Branch 比對
/// </summary>
public class HistoricalReleaseFetcherTests
{
    private readonly Mock<IGitRepository> _mockGitRepository;

    public HistoricalReleaseFetcherTests()
    {
        _mockGitRepository = new Mock<IGitRepository>();
    }

    /// <summary>
    /// FetchCommitsAsync 應取得下一版 Release 有但當前版 Release 沒有的 commits
    /// </summary>
    [Fact]
    public async Task FetchCommitsAsync_ShouldGetCommitsInNextReleaseButNotInCurrentRelease()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260113");
        var nextRelease = new ReleaseBranchName("release/20260120");

        var expectedCommits = new List<CommitHash>
        {
            new CommitHash("a1b2c3d"),
            new CommitHash("e4f5g6h"),
            new CommitHash("i7j8k9l")
        };

        _mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                repositoryName,
                currentRelease.Value,
                nextRelease.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCommits);

        var fetcher = new HistoricalReleaseFetcher(
            _mockGitRepository.Object,
            repositoryName,
            currentRelease,
            nextRelease);

        // Act
        var result = await fetcher.FetchCommitsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repositoryName, result.RepositoryName);
        Assert.Equal(currentRelease.Value, result.BaseBranch.Value);
        Assert.Equal(nextRelease.Value, result.CompareBranch.Value);
        Assert.Equal(3, result.CommitCount);

        _mockGitRepository.Verify(
            r => r.GetCommitDiffsAsync(
                repositoryName,
                currentRelease.Value,
                nextRelease.Value,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// FetchCommitsAsync 應在沒有差異時返回空結果
    /// </summary>
    [Fact]
    public async Task FetchCommitsAsync_WithNoDifference_ShouldReturnEmptyResult()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260113");
        var nextRelease = new ReleaseBranchName("release/20260120");

        _mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommitHash>());

        var fetcher = new HistoricalReleaseFetcher(
            _mockGitRepository.Object,
            repositoryName,
            currentRelease,
            nextRelease);

        // Act
        var result = await fetcher.FetchCommitsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.CommitCount);
    }

    /// <summary>
    /// FetchCommitsAsync 應在任一 Release Branch 不存在時拋出例外
    /// </summary>
    [Fact]
    public async Task FetchCommitsAsync_WithNonExistentReleaseBranch_ShouldThrowException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260113");
        var nextRelease = new ReleaseBranchName("release/20260999"); // 不存在的日期

        _mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Branch '{nextRelease.Value}' 不存在"));

        var fetcher = new HistoricalReleaseFetcher(
            _mockGitRepository.Object,
            repositoryName,
            currentRelease,
            nextRelease);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fetcher.FetchCommitsAsync());
        Assert.Contains("不存在", exception.Message);
    }

    /// <summary>
    /// 建構子應驗證 nextRelease 必須晚於 currentRelease
    /// </summary>
    [Fact]
    public void Constructor_WithNextReleaseOlderThanCurrent_ShouldThrowArgumentException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260120"); // 較新
        var nextRelease = new ReleaseBranchName("release/20260113");    // 較舊

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new HistoricalReleaseFetcher(_mockGitRepository.Object, repositoryName, currentRelease, nextRelease));
        Assert.Contains("nextRelease", exception.Message);
        Assert.Contains("currentRelease", exception.Message);
    }

    /// <summary>
    /// 建構子應拒絕相同的 release 版本
    /// </summary>
    [Fact]
    public void Constructor_WithSameReleaseVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260120");
        var nextRelease = new ReleaseBranchName("release/20260120");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new HistoricalReleaseFetcher(_mockGitRepository.Object, repositoryName, currentRelease, nextRelease));
        Assert.Contains("nextRelease", exception.Message);
    }

    /// <summary>
    /// 建構子應拒絕 null 的 git repository
    /// </summary>
    [Fact]
    public void Constructor_WithNullGitRepository_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260113");
        var nextRelease = new ReleaseBranchName("release/20260120");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HistoricalReleaseFetcher(null!, repositoryName, currentRelease, nextRelease));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 repository name
    /// </summary>
    [Fact]
    public void Constructor_WithNullRepositoryName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var currentRelease = new ReleaseBranchName("release/20260113");
        var nextRelease = new ReleaseBranchName("release/20260120");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HistoricalReleaseFetcher(_mockGitRepository.Object, null!, currentRelease, nextRelease));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 current release
    /// </summary>
    [Fact]
    public void Constructor_WithNullCurrentRelease_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var nextRelease = new ReleaseBranchName("release/20260120");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HistoricalReleaseFetcher(_mockGitRepository.Object, repositoryName, null!, nextRelease));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 next release
    /// </summary>
    [Fact]
    public void Constructor_WithNullNextRelease_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260113");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HistoricalReleaseFetcher(_mockGitRepository.Object, repositoryName, currentRelease, null!));
    }
}
