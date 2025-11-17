using FluentAssertions;
using NSubstitute;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Mappers;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Services;
using Xunit;

namespace ReleaseSync.Application.UnitTests.Mappers;

/// <summary>
/// GoogleSheetDataMapper 單元測試。
/// 測試 WorkItem != null 與 WorkItem == null 的處理邏輯。
/// </summary>
public class GoogleSheetDataMapperTests
{
    private readonly IWorkItemIdParser _mockParser;
    private readonly GoogleSheetDataMapper _mapper;

    public GoogleSheetDataMapperTests()
    {
        _mockParser = Substitute.For<IWorkItemIdParser>();
        _mapper = new GoogleSheetDataMapper(_mockParser);
    }

    #region WorkItem != null 測試

    [Fact]
    public void MapToSheetRows_WithWorkItem_ShouldMapCorrectly()
    {
        // Arrange
        var repositoryData = CreateRepositoryDataWithWorkItem(
            workItemId: 12345,
            workItemTitle: "Fix login bug",
            workItemTeam: "Backend",
            workItemUrl: "https://dev.azure.com/org/project/_workitems/edit/12345",
            repositoryName: "backend-api",
            authorName: "John Doe",
            prUrl: "https://gitlab.com/repo/mr/1");

        // Act
        var result = _mapper.MapToSheetRows(repositoryData);

        // Assert
        result.Should().HaveCount(1);
        var row = result[0];
        row.Feature.Should().Be("VSTS12345 - Fix login bug");
        row.FeatureUrl.Should().Be("https://dev.azure.com/org/project/_workitems/edit/12345");
        row.Team.Should().Be("Backend");
        row.UniqueKey.Should().Be("12345backend-api");
        row.RepositoryName.Should().Be("backend-api");
        row.Authors.Should().Contain("John Doe");
        row.PullRequestUrls.Should().Contain("https://gitlab.com/repo/mr/1");
    }

    [Fact]
    public void MapToSheetRows_WithMultiplePRsSameWorkItem_ShouldGroupCorrectly()
    {
        // Arrange
        var repositoryData = new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new()
                {
                    RepositoryName = "backend-api",
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        CreatePrWithWorkItem(12345, "Fix bug", "Backend", "John Doe", "https://gitlab.com/mr/1"),
                        CreatePrWithWorkItem(12345, "Fix bug", "Backend", "Jane Smith", "https://gitlab.com/mr/2")
                    }
                }
            }
        };

        // Act
        var result = _mapper.MapToSheetRows(repositoryData);

        // Assert
        result.Should().HaveCount(1);
        var row = result[0];
        row.Authors.Should().HaveCount(2);
        row.Authors.Should().Contain("John Doe");
        row.Authors.Should().Contain("Jane Smith");
        row.PullRequestUrls.Should().HaveCount(2);
    }

    #endregion

    #region WorkItem == null 測試 (從 SourceBranch 解析)

    [Fact]
    public void MapToSheetRows_WorkItemNullWithValidSourceBranch_ShouldParseFromSourceBranch()
    {
        // Arrange
        var repositoryData = CreateRepositoryDataWithoutWorkItem(
            repositoryName: "backend-api",
            sourceBranch: "vsts54321-fix-issue",
            prTitle: "Some PR title",
            authorName: "John Doe",
            prUrl: "https://gitlab.com/repo/mr/1");

        _mockParser.TryParseWorkItemId(
            Arg.Is<BranchName>(b => b.Value == "vsts54321-fix-issue"),
            out Arg.Any<WorkItemId>())
            .Returns(x =>
            {
                x[1] = new WorkItemId(54321);
                return true;
            });

        // Act
        var result = _mapper.MapToSheetRows(repositoryData);

        // Assert
        result.Should().HaveCount(1);
        var row = result[0];
        row.Feature.Should().Be("VSTS54321");
        row.FeatureUrl.Should().BeEmpty();
        row.Team.Should().BeEmpty();
        row.UniqueKey.Should().Be("54321backend-api");
        row.RepositoryName.Should().Be("backend-api");
        row.Authors.Should().Contain("John Doe");
    }

    [Fact]
    public void MapToSheetRows_WorkItemNullWithPRTitleContainingVSTS_ShouldParseFromTitle()
    {
        // Arrange
        var repositoryData = CreateRepositoryDataWithoutWorkItem(
            repositoryName: "backend-api",
            sourceBranch: "feature/some-feature",
            prTitle: "VSTS67890 - Implement new feature",
            authorName: "John Doe",
            prUrl: "https://gitlab.com/repo/mr/1");

        // SourceBranch 解析失敗
        _mockParser.TryParseWorkItemId(
            Arg.Is<BranchName>(b => b.Value == "feature/some-feature"),
            out Arg.Any<WorkItemId>())
            .Returns(false);

        // PR Title 解析成功
        _mockParser.TryParseWorkItemId(
            Arg.Is<BranchName>(b => b.Value == "VSTS67890 - Implement new feature"),
            out Arg.Any<WorkItemId>())
            .Returns(x =>
            {
                x[1] = new WorkItemId(67890);
                return true;
            });

        // Act
        var result = _mapper.MapToSheetRows(repositoryData);

        // Assert
        result.Should().HaveCount(1);
        var row = result[0];
        row.Feature.Should().Be("VSTS67890");
        row.UniqueKey.Should().Be("67890backend-api");
    }

    [Fact]
    public void MapToSheetRows_WorkItemNullAndCannotParse_ShouldUseZeroAsWorkItemId()
    {
        // Arrange
        var repositoryData = CreateRepositoryDataWithoutWorkItem(
            repositoryName: "backend-api",
            sourceBranch: "feature/no-vsts-id",
            prTitle: "Fix some issue without VSTS",
            authorName: "John Doe",
            prUrl: "https://gitlab.com/repo/mr/1");

        // 兩者都解析失敗
        _mockParser.TryParseWorkItemId(Arg.Any<BranchName>(), out Arg.Any<WorkItemId>())
            .Returns(false);

        // Act
        var result = _mapper.MapToSheetRows(repositoryData);

        // Assert
        result.Should().HaveCount(1);
        var row = result[0];
        row.Feature.Should().Be("VSTS0");
        row.UniqueKey.Should().Be("0backend-api");
        row.FeatureUrl.Should().BeEmpty();
        row.Team.Should().BeEmpty();
    }

    #endregion

    #region 混合測試 (WorkItem != null 與 WorkItem == null)

    [Fact]
    public void MapToSheetRows_MixedWorkItemNullAndNotNull_ShouldProcessBothSeparately()
    {
        // Arrange
        var repositoryData = new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new()
                {
                    RepositoryName = "backend-api",
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        // WorkItem != null
                        CreatePrWithWorkItem(12345, "Fix bug", "Backend", "John Doe", "https://gitlab.com/mr/1"),
                        // WorkItem == null
                        CreatePrWithoutWorkItem("vsts54321-feature", "Feature PR", "Jane Smith", "https://gitlab.com/mr/2")
                    }
                }
            }
        };

        _mockParser.TryParseWorkItemId(
            Arg.Is<BranchName>(b => b.Value == "vsts54321-feature"),
            out Arg.Any<WorkItemId>())
            .Returns(x =>
            {
                x[1] = new WorkItemId(54321);
                return true;
            });

        // Act
        var result = _mapper.MapToSheetRows(repositoryData);

        // Assert
        result.Should().HaveCount(2);

        var workItemRow = result.First(r => r.Feature.Contains("12345"));
        workItemRow.Feature.Should().Be("VSTS12345 - Fix bug");
        workItemRow.Team.Should().Be("Backend");

        var nullWorkItemRow = result.First(r => r.Feature.Contains("54321"));
        nullWorkItemRow.Feature.Should().Be("VSTS54321");
        nullWorkItemRow.Team.Should().BeEmpty();
    }

    [Fact]
    public void MapToSheetRows_MultiplePRsWithSameParsedWorkItemId_ShouldGroup()
    {
        // Arrange
        var repositoryData = new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new()
                {
                    RepositoryName = "backend-api",
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        CreatePrWithoutWorkItem("vsts99999-part1", "Part 1", "John Doe", "https://gitlab.com/mr/1"),
                        CreatePrWithoutWorkItem("vsts99999-part2", "Part 2", "Jane Smith", "https://gitlab.com/mr/2")
                    }
                }
            }
        };

        // 兩個 Branch 都解析到相同的 WorkItemId
        _mockParser.TryParseWorkItemId(Arg.Any<BranchName>(), out Arg.Any<WorkItemId>())
            .Returns(x =>
            {
                x[1] = new WorkItemId(99999);
                return true;
            });

        // Act
        var result = _mapper.MapToSheetRows(repositoryData);

        // Assert
        result.Should().HaveCount(1);
        var row = result[0];
        row.Feature.Should().Be("VSTS99999");
        row.Authors.Should().HaveCount(2);
        row.PullRequestUrls.Should().HaveCount(2);
    }

    #endregion

    #region GenerateUniqueKey 測試

    [Fact]
    public void GenerateUniqueKey_ValidInput_ShouldReturnCorrectFormat()
    {
        // Act
        var result = _mapper.GenerateUniqueKey(12345, "backend-api");

        // Assert
        result.Should().Be("12345backend-api");
    }

    [Fact]
    public void GenerateUniqueKey_ZeroWorkItemId_ShouldIncludeZero()
    {
        // Act
        var result = _mapper.GenerateUniqueKey(0, "backend-api");

        // Assert
        result.Should().Be("0backend-api");
    }

    [Fact]
    public void GenerateUniqueKey_EmptyRepositoryName_ShouldThrowException()
    {
        // Act & Assert
        var act = () => _mapper.GenerateUniqueKey(12345, "");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Helper Methods

    private static RepositoryBasedOutputDto CreateRepositoryDataWithWorkItem(
        int workItemId,
        string workItemTitle,
        string workItemTeam,
        string workItemUrl,
        string repositoryName,
        string authorName,
        string prUrl)
    {
        return new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new()
                {
                    RepositoryName = repositoryName,
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        new()
                        {
                            WorkItem = new PullRequestWorkItemDto
                            {
                                WorkItemId = workItemId,
                                WorkItemTitle = workItemTitle,
                                WorkItemTeam = workItemTeam,
                                WorkItemType = "Task",
                                WorkItemUrl = workItemUrl
                            },
                            PullRequestTitle = "PR Title",
                            SourceBranch = $"vsts{workItemId}-feature",
                            TargetBranch = "main",
                            MergedAt = DateTime.UtcNow,
                            AuthorDisplayName = authorName,
                            PullRequestUrl = prUrl
                        }
                    }
                }
            }
        };
    }

    private static RepositoryBasedOutputDto CreateRepositoryDataWithoutWorkItem(
        string repositoryName,
        string sourceBranch,
        string prTitle,
        string authorName,
        string prUrl)
    {
        return new RepositoryBasedOutputDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Repositories = new List<RepositoryGroupDto>
            {
                new()
                {
                    RepositoryName = repositoryName,
                    Platform = "GitLab",
                    PullRequests = new List<RepositoryPullRequestDto>
                    {
                        CreatePrWithoutWorkItem(sourceBranch, prTitle, authorName, prUrl)
                    }
                }
            }
        };
    }

    private static RepositoryPullRequestDto CreatePrWithWorkItem(
        int workItemId,
        string workItemTitle,
        string workItemTeam,
        string authorName,
        string prUrl)
    {
        return new RepositoryPullRequestDto
        {
            WorkItem = new PullRequestWorkItemDto
            {
                WorkItemId = workItemId,
                WorkItemTitle = workItemTitle,
                WorkItemTeam = workItemTeam,
                WorkItemType = "Task",
                WorkItemUrl = $"https://dev.azure.com/org/project/_workitems/edit/{workItemId}"
            },
            PullRequestTitle = "PR Title",
            SourceBranch = $"vsts{workItemId}-feature",
            TargetBranch = "main",
            MergedAt = DateTime.UtcNow,
            AuthorDisplayName = authorName,
            PullRequestUrl = prUrl
        };
    }

    private static RepositoryPullRequestDto CreatePrWithoutWorkItem(
        string sourceBranch,
        string prTitle,
        string authorName,
        string prUrl)
    {
        return new RepositoryPullRequestDto
        {
            WorkItem = null,
            PullRequestTitle = prTitle,
            SourceBranch = sourceBranch,
            TargetBranch = "main",
            MergedAt = DateTime.UtcNow,
            AuthorDisplayName = authorName,
            PullRequestUrl = prUrl
        };
    }

    #endregion
}
