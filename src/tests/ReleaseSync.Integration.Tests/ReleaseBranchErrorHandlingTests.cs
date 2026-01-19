namespace ReleaseSync.Integration.Tests;

using Moq;
using ReleaseSync.Application.Services;
using ReleaseSync.Application.Services.CommitFetchers;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using Xunit;

/// <summary>
/// Release Branch 錯誤處理整合測試
/// </summary>
public class ReleaseBranchErrorHandlingTests
{
    /// <summary>
    /// 當 ReleaseBranch 不存在時,系統應拋出 InvalidOperationException 並終止執行
    /// </summary>
    [Fact]
    public async Task WhenReleaseBranchDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockGitRepository = new Mock<IGitRepository>();
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var nonExistentReleaseBranch = new ReleaseBranchName("release/20269999"); // 不存在的分支

        mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                repositoryName,
                nonExistentReleaseBranch.Value,
                targetBranch.Value,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                $"Branch '{nonExistentReleaseBranch.Value}' 不存在於 repository '{repositoryName}'"));

        var fetcher = new LatestReleaseFetcher(
            mockGitRepository.Object,
            repositoryName,
            targetBranch,
            nonExistentReleaseBranch);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await fetcher.FetchCommitsAsync());

        Assert.Contains("不存在", exception.Message);
        Assert.Contains(nonExistentReleaseBranch.Value, exception.Message);
        Assert.Contains(repositoryName, exception.Message);
    }

    /// <summary>
    /// 當 NextReleaseBranch 不存在時（Historical 場景）,系統應拋出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task WhenNextReleaseBranchDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockGitRepository = new Mock<IGitRepository>();
        var repositoryName = "payment/payment.adminapi";
        var currentRelease = new ReleaseBranchName("release/20260113");
        var nonExistentNextRelease = new ReleaseBranchName("release/20269999");

        mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                repositoryName,
                currentRelease.Value,
                nonExistentNextRelease.Value,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                $"Branch '{nonExistentNextRelease.Value}' 不存在於 repository '{repositoryName}'"));

        var fetcher = new HistoricalReleaseFetcher(
            mockGitRepository.Object,
            repositoryName,
            currentRelease,
            nonExistentNextRelease);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await fetcher.FetchCommitsAsync());

        Assert.Contains("不存在", exception.Message);
    }

    /// <summary>
    /// CommitFetcherFactory 應在 ReleaseBranch 模式但未提供 ReleaseBranch 時拋出 ArgumentException
    /// </summary>
    [Fact]
    public void CommitFetcherFactory_WithReleaseBranchModeButMissingBranch_ShouldThrowArgumentException()
    {
        // Arrange
        var mockPRRepository = new Mock<IPullRequestRepository>();
        var mockGitRepository = new Mock<IGitRepository>();
        var factory = new CommitFetcherFactory(mockPRRepository.Object, mockGitRepository.Object);

        var invalidConfig = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = null, // 缺少 ReleaseBranch
            IsLatestRelease = true
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.Create(invalidConfig));
        Assert.Contains("ReleaseBranch", exception.Message);
        Assert.Contains("FetchMode.ReleaseBranch", exception.Message);
    }

    /// <summary>
    /// 驗證錯誤訊息包含足夠的上下文資訊以利除錯
    /// </summary>
    [Fact]
    public async Task ErrorMessage_ShouldContainSufficientContextForDebugging()
    {
        // Arrange
        var mockGitRepository = new Mock<IGitRepository>();
        var repositoryName = "payment/payment.adminapi";
        var targetBranch = new BranchName("master");
        var missingBranch = new ReleaseBranchName("release/20260120");

        mockGitRepository
            .Setup(r => r.GetCommitDiffsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                $"Branch '{missingBranch.Value}' 不存在於 repository '{repositoryName}'。" +
                $"請確認：\n" +
                $"1. Release branch 已建立\n" +
                $"2. Branch 命名格式正確（release/yyyyMMdd）\n" +
                $"3. 您有足夠的權限存取此 repository"));

        var fetcher = new LatestReleaseFetcher(
            mockGitRepository.Object,
            repositoryName,
            targetBranch,
            missingBranch);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await fetcher.FetchCommitsAsync());

        // Assert - 驗證錯誤訊息包含除錯資訊
        Assert.Contains(missingBranch.Value, exception.Message);
        Assert.Contains(repositoryName, exception.Message);
        Assert.Contains("請確認", exception.Message);
    }
}
