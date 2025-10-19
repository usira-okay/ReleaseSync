using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Parsers;
using FluentAssertions;

namespace ReleaseSync.Infrastructure.UnitTests.Parsers;

/// <summary>
/// RegexWorkItemIdParser 單元測試
/// </summary>
public class RegexWorkItemIdParserTests
{
    private readonly ILogger<RegexWorkItemIdParser> _mockLogger;

    public RegexWorkItemIdParserTests()
    {
        _mockLogger = Substitute.For<ILogger<RegexWorkItemIdParser>>();
    }

    /// <summary>
    /// 測試基本的 Work Item ID 解析 (格式: feature/123-description)
    /// </summary>
    [Fact]
    public void Should_Parse_WorkItemId_From_Standard_Branch_Name()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Standard Pattern",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/12345-add-new-feature");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeTrue();
        workItemId.Should().NotBeNull();
        workItemId!.Value.Should().Be(12345);
    }

    /// <summary>
    /// 測試大小寫不敏感的解析 (IgnoreCase = true)
    /// </summary>
    [Fact]
    public void Should_Parse_WorkItemId_Case_Insensitive()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Case Insensitive Pattern",
                    Regex = @"VSTS(\d+)",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/vsts12345-description");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeTrue();
        workItemId.Should().NotBeNull();
        workItemId!.Value.Should().Be(12345);
    }

    /// <summary>
    /// 測試大小寫敏感的解析 (IgnoreCase = false)
    /// </summary>
    [Fact]
    public void Should_Not_Parse_WorkItemId_When_Case_Sensitive_Mismatch()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Case Sensitive Pattern",
                    Regex = @"VSTS(\d+)",
                    IgnoreCase = false,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/vsts12345-description");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }

    /// <summary>
    /// 測試多重 Pattern 解析 (第一個匹配成功即回傳)
    /// </summary>
    [Fact]
    public void Should_Try_Multiple_Patterns_Until_Match()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Pattern 1",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                },
                new WorkItemIdPattern
                {
                    Name = "Pattern 2",
                    Regex = @"bugfix/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                },
                new WorkItemIdPattern
                {
                    Name = "Pattern 3",
                    Regex = @"vsts(\d+)",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("bugfix/99999-fix-crash");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeTrue();
        workItemId.Should().NotBeNull();
        workItemId!.Value.Should().Be(99999);
    }

    /// <summary>
    /// 測試自訂 Capture Group 索引
    /// </summary>
    [Fact]
    public void Should_Use_Custom_Capture_Group()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Custom Group Pattern",
                    Regex = @"(feature|bugfix)/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 2  // 第二個 capture group
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/54321-test");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeTrue();
        workItemId.Should().NotBeNull();
        workItemId!.Value.Should().Be(54321);
    }

    /// <summary>
    /// 測試當無任何 Pattern 匹配時應回傳 false
    /// </summary>
    [Fact]
    public void Should_Return_False_When_No_Pattern_Matches()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Standard Pattern",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("main");  // 無法匹配任何 pattern

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }

    /// <summary>
    /// 測試當 Pattern 清單為空時應回傳 false
    /// </summary>
    [Fact]
    public void Should_Return_False_When_No_Patterns_Configured()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>()  // 空清單
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/12345-test");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }

    /// <summary>
    /// 測試 ParseWorkItemId 方法在成功時應回傳 WorkItemId
    /// </summary>
    [Fact]
    public void ParseWorkItemId_Should_Return_WorkItemId_On_Success()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Standard Pattern",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/12345-test");

        // Act
        var workItemId = parser.ParseWorkItemId(branchName);

        // Assert
        workItemId.Should().NotBeNull();
        workItemId.Value.Should().Be(12345);
    }

    /// <summary>
    /// 測試 ParseWorkItemId 方法在失敗時應拋出 FormatException
    /// </summary>
    [Fact]
    public void ParseWorkItemId_Should_Throw_FormatException_On_Failure()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Standard Pattern",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("main");

        // Act & Assert
        var act = () => parser.ParseWorkItemId(branchName);
        act.Should().Throw<FormatException>()
            .WithMessage("*無法從 Branch 名稱解析 Work Item ID*");
    }

    /// <summary>
    /// 測試當解析結果為 0 或負數時應視為無效
    /// </summary>
    [Fact]
    public void Should_Reject_Zero_Or_Negative_WorkItemId()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Standard Pattern",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/0-invalid");  // Work Item ID = 0

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }

    /// <summary>
    /// 測試 OnParseFailure = "LogWarningAndContinue" 行為
    /// </summary>
    [Fact]
    public void Should_Log_Warning_And_Continue_On_Parse_Failure()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Standard Pattern",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            },
            ParsingBehavior = new ParsingBehaviorSettings
            {
                OnParseFailure = "LogWarningAndContinue"
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("main");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }

    /// <summary>
    /// 測試無效的 Regex 模式應記錄錯誤並繼續
    /// </summary>
    [Fact]
    public void Should_Handle_Invalid_Regex_Pattern_Gracefully()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Invalid Pattern",
                    Regex = @"[invalid(regex",  // 無效的 Regex
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("feature/12345-test");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeFalse();
        workItemId.Should().BeNull();
    }

    /// <summary>
    /// 測試複雜的 Branch 名稱解析 (包含 refs/heads/ 前綴)
    /// </summary>
    [Fact]
    public void Should_Parse_WorkItemId_From_Full_Ref_Branch_Name()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            WorkItemIdPatterns = new List<WorkItemIdPattern>
            {
                new WorkItemIdPattern
                {
                    Name = "Standard Pattern",
                    Regex = @"feature/(\d+)-",
                    IgnoreCase = true,
                    CaptureGroup = 1
                }
            }
        };

        var parser = new RegexWorkItemIdParser(settings, _mockLogger);
        var branchName = new BranchName("refs/heads/feature/12345-add-feature");

        // Act
        var result = parser.TryParseWorkItemId(branchName, out var workItemId);

        // Assert
        result.Should().BeTrue();
        workItemId.Should().NotBeNull();
        workItemId!.Value.Should().Be(12345);
    }
}
