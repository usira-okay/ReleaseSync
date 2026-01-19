namespace ReleaseSync.Application.UnitTests.Services.CommitFetchers;

using Moq;
using ReleaseSync.Application.Services.CommitFetchers;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using Xunit;

/// <summary>
/// LatestReleaseFetcher 測試
/// 場景：Release Branch（最新版）↔ TargetBranch 比對
/// </summary>
public class LatestReleaseFetcherTests
{
    private readonly Mock<IGitRepository> _mockGitRepository;

    public LatestReleaseFetcherTests()
    {
        _mockGitRepository = new Mock<IGitRepository>();
    }

    /// <summary>
    /// FetchCommitsAsync 應取得 TargetBranch 有但 ReleaseBranch 沒有的 commits
    /// </summary>
    [Fact]
    public async Task FetchCommitsAsync_ShouldGetCommitsInTargetBranchButNotInReleaseBranch()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var releaseBranch = new ReleaseBranchName("release/20260120");

        var expectedCommits = new List<CommitHash>
        {
            new CommitHash("a1b2c3d"),
            new CommitHash("e4f5g6h")
        };

        _mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                repositoryName,
                releaseBranch.Value,
                targetBranch.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCommits);

        var fetcher = new LatestReleaseFetcher(
            _mockGitRepository.Object,
            repositoryName,
            targetBranch,
            releaseBranch);

        // Act
        var result = await fetcher.FetchCommitsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repositoryName, result.RepositoryName);
        Assert.Equal(releaseBranch.Value, result.BaseBranch.Value);
        Assert.Equal(targetBranch.Value, result.CompareBranch.Value);
        Assert.Equal(2, result.CommitCount);

        _mockGitRepository.Verify(
            r => r.GetCommitDiffsAsync(
                repositoryName,
                releaseBranch.Value,
                targetBranch.Value,
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
        var targetBranch = new BranchName("master");
        var releaseBranch = new ReleaseBranchName("release/20260120");

        _mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommitHash>());

        var fetcher = new LatestReleaseFetcher(
            _mockGitRepository.Object,
            repositoryName,
            targetBranch,
            releaseBranch);

        // Act
        var result = await fetcher.FetchCommitsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.CommitCount);
    }

    /// <summary>
    /// FetchCommitsAsync 應在 ReleaseBranch 不存在時拋出例外
    /// </summary>
    [Fact]
    public async Task FetchCommitsAsync_WithNonExistentReleaseBranch_ShouldThrowException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var releaseBranch = new ReleaseBranchName("release/20260120");

        _mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Branch '{releaseBranch.Value}' 不存在"));

        var fetcher = new LatestReleaseFetcher(
            _mockGitRepository.Object,
            repositoryName,
            targetBranch,
            releaseBranch);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fetcher.FetchCommitsAsync());
        Assert.Contains("不存在", exception.Message);
    }

    /// <summary>
    /// 建構子應拒絕 null 的 git repository
    /// </summary>
    [Fact]
    public void Constructor_WithNullGitRepository_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var releaseBranch = new ReleaseBranchName("release/20260120");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LatestReleaseFetcher(null!, repositoryName, targetBranch, releaseBranch));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 repository name
    /// </summary>
    [Fact]
    public void Constructor_WithNullRepositoryName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var targetBranch = new BranchName("master");
        var releaseBranch = new ReleaseBranchName("release/20260120");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LatestReleaseFetcher(_mockGitRepository.Object, null!, targetBranch, releaseBranch));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 target branch
    /// </summary>
    [Fact]
    public void Constructor_WithNullTargetBranch_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var releaseBranch = new ReleaseBranchName("release/20260120");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LatestReleaseFetcher(_mockGitRepository.Object, repositoryName, null!, releaseBranch));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 release branch
    /// </summary>
    [Fact]
    public void Constructor_WithNullReleaseBranch_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LatestReleaseFetcher(_mockGitRepository.Object, repositoryName, targetBranch, null!));
    }
}
