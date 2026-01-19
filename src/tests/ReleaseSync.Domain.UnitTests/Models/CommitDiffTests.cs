namespace ReleaseSync.Domain.UnitTests.Models;

using ReleaseSync.Domain.Models;
using Xunit;

/// <summary>
/// CommitDiff model 測試
/// </summary>
public class CommitDiffTests
{
    /// <summary>
    /// 建構子應接受有效的參數
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>
        {
            new CommitHash("a1b2c3d"),
            new CommitHash("e4f5g6h")
        };

        // Act
        var commitDiff = new CommitDiff(repositoryName, baseBranch, compareBranch, commitHashes);

        // Assert
        Assert.NotNull(commitDiff);
        Assert.Equal(repositoryName, commitDiff.RepositoryName);
        Assert.Equal(baseBranch, commitDiff.BaseBranch);
        Assert.Equal(compareBranch, commitDiff.CompareBranch);
        Assert.Equal(2, commitDiff.CommitHashes.Count());
    }

    /// <summary>
    /// 建構子應拒絕 null 的 repository name
    /// </summary>
    [Fact]
    public void Constructor_WithNullRepositoryName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommitDiff(null!, baseBranch, compareBranch, commitHashes));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 base branch
    /// </summary>
    [Fact]
    public void Constructor_WithNullBaseBranch_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommitDiff(repositoryName, null!, compareBranch, commitHashes));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 compare branch
    /// </summary>
    [Fact]
    public void Constructor_WithNullCompareBranch_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var commitHashes = new List<CommitHash>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommitDiff(repositoryName, baseBranch, null!, commitHashes));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 commit hashes
    /// </summary>
    [Fact]
    public void Constructor_WithNullCommitHashes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommitDiff(repositoryName, baseBranch, compareBranch, null!));
    }

    /// <summary>
    /// 建構子應接受空的 commit hashes 集合
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyCommitHashes_ShouldCreateInstance()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>();

        // Act
        var commitDiff = new CommitDiff(repositoryName, baseBranch, compareBranch, commitHashes);

        // Assert
        Assert.NotNull(commitDiff);
        Assert.Empty(commitDiff.CommitHashes);
    }

    /// <summary>
    /// CommitCount 屬性應返回正確的 commit 數量
    /// </summary>
    [Fact]
    public void CommitCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>
        {
            new CommitHash("a1b2c3d"),
            new CommitHash("e4f5g6h"),
            new CommitHash("i7j8k9l")
        };

        // Act
        var commitDiff = new CommitDiff(repositoryName, baseBranch, compareBranch, commitHashes);

        // Assert
        Assert.Equal(3, commitDiff.CommitCount);
    }

    /// <summary>
    /// HasCommits 屬性應在有 commit 時返回 true
    /// </summary>
    [Fact]
    public void HasCommits_WithCommits_ShouldReturnTrue()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>
        {
            new CommitHash("a1b2c3d")
        };

        // Act
        var commitDiff = new CommitDiff(repositoryName, baseBranch, compareBranch, commitHashes);

        // Assert
        Assert.True(commitDiff.HasCommits);
    }

    /// <summary>
    /// HasCommits 屬性應在沒有 commit 時返回 false
    /// </summary>
    [Fact]
    public void HasCommits_WithoutCommits_ShouldReturnFalse()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>();

        // Act
        var commitDiff = new CommitDiff(repositoryName, baseBranch, compareBranch, commitHashes);

        // Assert
        Assert.False(commitDiff.HasCommits);
    }

    /// <summary>
    /// GetSummary 方法應返回正確的摘要
    /// </summary>
    [Fact]
    public void GetSummary_ShouldReturnCorrectSummary()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>
        {
            new CommitHash("a1b2c3d"),
            new CommitHash("e4f5g6h")
        };
        var commitDiff = new CommitDiff(repositoryName, baseBranch, compareBranch, commitHashes);

        // Act
        var summary = commitDiff.GetSummary();

        // Assert
        Assert.Contains(repositoryName, summary);
        Assert.Contains(baseBranch.Value, summary);
        Assert.Contains(compareBranch.Value, summary);
        Assert.Contains("2", summary); // commit 數量
    }

    /// <summary>
    /// CommitHashes 應該是不可變的（防禦性複製）
    /// </summary>
    [Fact]
    public void CommitHashes_ShouldBeImmutable()
    {
        // Arrange
        var repositoryName = "payment/payment.adminapi";
        var baseBranch = new BranchName("release/20260113");
        var compareBranch = new BranchName("release/20260120");
        var commitHashes = new List<CommitHash>
        {
            new CommitHash("a1b2c3d")
        };
        var commitDiff = new CommitDiff(repositoryName, baseBranch, compareBranch, commitHashes);

        // Act - 修改原始集合
        commitHashes.Add(new CommitHash("e4f5g6h"));

        // Assert - CommitDiff 的資料不應被影響
        Assert.Single(commitDiff.CommitHashes);
    }
}
