using FluentAssertions;
using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Platforms.Models;
using ReleaseSync.Infrastructure.Services;
using Xunit;

namespace ReleaseSync.Infrastructure.UnitTests.Services;

/// <summary>
/// ReleaseBranchVersionResolver 單元測試
/// </summary>
public class ReleaseBranchVersionResolverTests
{
    private readonly ReleaseBranchVersionResolver _resolver = new();

    #region IsLatestVersion Tests

    [Fact]
    public void IsLatestVersion_WithLatestBranch_ShouldReturnTrue()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260120");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260113", CommitSha = "abc123" },
            new() { Name = "release/20260120", CommitSha = "def456" },
            new() { Name = "main", CommitSha = "ghi789" }
        };

        // Act
        var result = _resolver.IsLatestVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLatestVersion_WithOlderBranch_ShouldReturnFalse()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260113");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260113", CommitSha = "abc123" },
            new() { Name = "release/20260120", CommitSha = "def456" },
            new() { Name = "main", CommitSha = "ghi789" }
        };

        // Act
        var result = _resolver.IsLatestVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLatestVersion_WithOnlyOneBranch_ShouldReturnTrue()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260113");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260113", CommitSha = "abc123" }
        };

        // Act
        var result = _resolver.IsLatestVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLatestVersion_WithEmptyBranches_ShouldReturnTrue()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260113");
        var allBranches = new List<BranchInfo>();

        // Act
        var result = _resolver.IsLatestVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLatestVersion_WithNonReleaseBranches_ShouldIgnoreThem()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260113");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260113", CommitSha = "abc123" },
            new() { Name = "main", CommitSha = "def456" },
            new() { Name = "develop", CommitSha = "ghi789" },
            new() { Name = "feature/some-feature", CommitSha = "jkl012" }
        };

        // Act
        var result = _resolver.IsLatestVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetNextVersion Tests

    [Fact]
    public void GetNextVersion_WithNewerBranchExists_ShouldReturnNextVersion()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260113");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260106", CommitSha = "aaa111" },
            new() { Name = "release/20260113", CommitSha = "bbb222" },
            new() { Name = "release/20260120", CommitSha = "ccc333" },
            new() { Name = "release/20260127", CommitSha = "ddd444" }
        };

        // Act
        var result = _resolver.GetNextVersion(currentBranch, allBranches);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("release/20260120");
    }

    [Fact]
    public void GetNextVersion_WithLatestBranch_ShouldReturnNull()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260120");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260113", CommitSha = "abc123" },
            new() { Name = "release/20260120", CommitSha = "def456" }
        };

        // Act
        var result = _resolver.GetNextVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetNextVersion_WithOnlyCurrentBranch_ShouldReturnNull()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260113");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260113", CommitSha = "abc123" }
        };

        // Act
        var result = _resolver.GetNextVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetNextVersion_WithEmptyBranches_ShouldReturnNull()
    {
        // Arrange
        var currentBranch = ReleaseBranchName.Parse("release/20260113");
        var allBranches = new List<BranchInfo>();

        // Act
        var result = _resolver.GetNextVersion(currentBranch, allBranches);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetNextVersion_ShouldReturnImmediateNextVersion_NotLatest()
    {
        // Arrange - 確保返回的是下一個版本，而非最新版本
        var currentBranch = ReleaseBranchName.Parse("release/20260106");
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260106", CommitSha = "aaa111" },
            new() { Name = "release/20260113", CommitSha = "bbb222" },
            new() { Name = "release/20260120", CommitSha = "ccc333" }
        };

        // Act
        var result = _resolver.GetNextVersion(currentBranch, allBranches);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("release/20260113"); // 應該是下一個版本，不是最新版本
    }

    #endregion

    #region GetAllReleaseBranches Tests

    [Fact]
    public void GetAllReleaseBranches_ShouldFilterOnlyReleaseBranches()
    {
        // Arrange
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260106", CommitSha = "aaa111" },
            new() { Name = "main", CommitSha = "bbb222" },
            new() { Name = "release/20260113", CommitSha = "ccc333" },
            new() { Name = "develop", CommitSha = "ddd444" },
            new() { Name = "release/20260120", CommitSha = "eee555" },
            new() { Name = "feature/some-feature", CommitSha = "fff666" }
        };

        // Act
        var result = _resolver.GetAllReleaseBranches(allBranches);

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.Value).Should().BeEquivalentTo(new[]
        {
            "release/20260106",
            "release/20260113",
            "release/20260120"
        });
    }

    [Fact]
    public void GetAllReleaseBranches_ShouldReturnInAscendingOrder()
    {
        // Arrange
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260120", CommitSha = "aaa111" },
            new() { Name = "release/20260106", CommitSha = "bbb222" },
            new() { Name = "release/20260113", CommitSha = "ccc333" }
        };

        // Act
        var result = _resolver.GetAllReleaseBranches(allBranches).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Value.Should().Be("release/20260106");
        result[1].Value.Should().Be("release/20260113");
        result[2].Value.Should().Be("release/20260120");
    }

    [Fact]
    public void GetAllReleaseBranches_WithNonReleaseBranchesOnly_ShouldReturnEmpty()
    {
        // Arrange
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "main", CommitSha = "aaa111" },
            new() { Name = "develop", CommitSha = "bbb222" }
        };

        // Act
        var result = _resolver.GetAllReleaseBranches(allBranches);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllReleaseBranches_WithInvalidFormatBranches_ShouldSkipThem()
    {
        // Arrange
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260113", CommitSha = "aaa111" },
            new() { Name = "release/invalid", CommitSha = "bbb222" },
            new() { Name = "release/2026-01-20", CommitSha = "ccc333" }, // 錯誤格式
            new() { Name = "release/20260120", CommitSha = "ddd444" }
        };

        // Act
        var result = _resolver.GetAllReleaseBranches(allBranches);

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Value).Should().BeEquivalentTo(new[]
        {
            "release/20260113",
            "release/20260120"
        });
    }

    #endregion

    #region GetLatestReleaseBranch Tests

    [Fact]
    public void GetLatestReleaseBranch_ShouldReturnLatest()
    {
        // Arrange
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "release/20260106", CommitSha = "aaa111" },
            new() { Name = "release/20260113", CommitSha = "bbb222" },
            new() { Name = "release/20260120", CommitSha = "ccc333" }
        };

        // Act
        var result = _resolver.GetLatestReleaseBranch(allBranches);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("release/20260120");
    }

    [Fact]
    public void GetLatestReleaseBranch_WithNoReleaseBranches_ShouldReturnNull()
    {
        // Arrange
        var allBranches = new List<BranchInfo>
        {
            new() { Name = "main", CommitSha = "aaa111" },
            new() { Name = "develop", CommitSha = "bbb222" }
        };

        // Act
        var result = _resolver.GetLatestReleaseBranch(allBranches);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
