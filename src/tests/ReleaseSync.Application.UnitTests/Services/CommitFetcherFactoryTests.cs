namespace ReleaseSync.Application.UnitTests.Services;

using Moq;
using ReleaseSync.Application.Services;
using ReleaseSync.Application.Services.CommitFetchers;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Repositories;
using Xunit;

/// <summary>
/// CommitFetcherFactory 測試
/// </summary>
public class CommitFetcherFactoryTests
{
    private readonly Mock<IPullRequestRepository> _mockPRRepository;
    private readonly Mock<IGitRepository> _mockGitRepository;

    public CommitFetcherFactoryTests()
    {
        _mockPRRepository = new Mock<IPullRequestRepository>();
        _mockGitRepository = new Mock<IGitRepository>();
    }

    /// <summary>
    /// Create 應在 FetchMode.DateRange 時建立 DateRangeFetcher
    /// </summary>
    [Fact]
    public void Create_WithDateRangeMode_ShouldCreateDateRangeFetcher()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);
        var config = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.DateRange,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            ReleaseBranch = null
        };

        // Act
        var fetcher = factory.Create(config);

        // Assert
        Assert.NotNull(fetcher);
        Assert.IsType<DateRangeFetcher>(fetcher);
    }

    /// <summary>
    /// Create 應在 FetchMode.ReleaseBranch (最新版) 時建立 LatestReleaseFetcher
    /// </summary>
    [Fact]
    public void Create_WithReleaseBranchModeAndLatestRelease_ShouldCreateLatestReleaseFetcher()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);
        var config = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260120",
            IsLatestRelease = true // 明確標記為最新版
        };

        // Act
        var fetcher = factory.Create(config);

        // Assert
        Assert.NotNull(fetcher);
        Assert.IsType<LatestReleaseFetcher>(fetcher);
    }

    /// <summary>
    /// Create 應在 FetchMode.ReleaseBranch (舊版) 時建立 HistoricalReleaseFetcher
    /// </summary>
    [Fact]
    public void Create_WithReleaseBranchModeAndHistoricalRelease_ShouldCreateHistoricalReleaseFetcher()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);
        var config = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260113",
            IsLatestRelease = false,
            NextReleaseBranch = "release/20260120" // 提供下一版
        };

        // Act
        var fetcher = factory.Create(config);

        // Assert
        Assert.NotNull(fetcher);
        Assert.IsType<HistoricalReleaseFetcher>(fetcher);
    }

    /// <summary>
    /// Create 應在 DateRange 模式但缺少 StartDate 時拋出例外
    /// </summary>
    [Fact]
    public void Create_WithDateRangeModeButMissingStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);
        var config = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.DateRange,
            StartDate = null,
            EndDate = new DateTime(2026, 1, 31)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("StartDate", exception.Message);
    }

    /// <summary>
    /// Create 應在 DateRange 模式但缺少 EndDate 時拋出例外
    /// </summary>
    [Fact]
    public void Create_WithDateRangeModeButMissingEndDate_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);
        var config = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.DateRange,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = null
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("EndDate", exception.Message);
    }

    /// <summary>
    /// Create 應在 ReleaseBranch 模式但缺少 ReleaseBranch 時拋出例外
    /// </summary>
    [Fact]
    public void Create_WithReleaseBranchModeButMissingReleaseBranch_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);
        var config = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = null
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("ReleaseBranch", exception.Message);
    }

    /// <summary>
    /// Create 應在 ReleaseBranch 模式（舊版）但缺少 NextReleaseBranch 時拋出例外
    /// </summary>
    [Fact]
    public void Create_WithHistoricalReleaseButMissingNextRelease_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);
        var config = new FetcherConfiguration
        {
            RepositoryName = "payment/payment.adminapi",
            TargetBranch = "master",
            FetchMode = FetchMode.ReleaseBranch,
            ReleaseBranch = "release/20260113",
            IsLatestRelease = false,
            NextReleaseBranch = null
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("NextReleaseBranch", exception.Message);
    }

    /// <summary>
    /// Create 應拒絕 null 的 configuration
    /// </summary>
    [Fact]
    public void Create_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new CommitFetcherFactory(_mockPRRepository.Object, _mockGitRepository.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 PR repository
    /// </summary>
    [Fact]
    public void Constructor_WithNullPRRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommitFetcherFactory(null!, _mockGitRepository.Object));
    }

    /// <summary>
    /// 建構子應拒絕 null 的 Git repository
    /// </summary>
    [Fact]
    public void Constructor_WithNullGitRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommitFetcherFactory(_mockPRRepository.Object, null!));
    }
}

/// <summary>
/// Fetcher 配置（用於測試）
/// </summary>
public class FetcherConfiguration
{
    public required string RepositoryName { get; init; }
    public required string TargetBranch { get; init; }
    public required FetchMode FetchMode { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? ReleaseBranch { get; init; }
    public bool IsLatestRelease { get; init; }
    public string? NextReleaseBranch { get; init; }
}
