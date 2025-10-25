using FluentAssertions;
using Microsoft.Extensions.Options;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Services;

namespace ReleaseSync.Infrastructure.UnitTests.Services;

/// <summary>
/// TeamMappingService 單元測試
/// </summary>
public class TeamMappingServiceTests
{
    // ========== HasMapping 方法測試 ==========

    /// <summary>
    /// 測試已對應團隊返回 true
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_True_For_Mapped_Team()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                },
                new TeamMapping
                {
                    OriginalTeamName = "DailyResource",
                    DisplayName = "日常資源團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result1 = service.HasMapping("MoneyLogistic");
        var result2 = service.HasMapping("DailyResource");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    /// <summary>
    /// 測試未對應團隊返回 false
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_False_For_Unmapped_Team()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result = service.HasMapping("UnknownTeam");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 測試空 TeamMapping 返回 true (向後相容)
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_True_When_Mappings_Are_Empty()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>()
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result = service.HasMapping("AnyTeam");

        // Assert
        result.Should().BeTrue("空 TeamMapping 時應不過濾任何團隊 (向後相容)");
    }

    /// <summary>
    /// 測試大小寫不敏感比對
    /// </summary>
    [Fact]
    public void HasMapping_Should_Be_Case_Insensitive()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result1 = service.HasMapping("moneylogistic");
        var result2 = service.HasMapping("MONEYLOGISTIC");
        var result3 = service.HasMapping("MoneyLogistic");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }

    /// <summary>
    /// 測試 null 或空字串團隊名稱返回 false
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_False_For_Null_Or_Empty_TeamName()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var nullResult = service.HasMapping(null);
        var emptyResult = service.HasMapping("");
        var whitespaceResult = service.HasMapping("   ");

        // Assert
        nullResult.Should().BeFalse();
        emptyResult.Should().BeFalse();
        whitespaceResult.Should().BeFalse();
    }

    // ========== GetDisplayName 方法測試 ==========

    /// <summary>
    /// 測試返回正確的顯示名稱
    /// </summary>
    [Fact]
    public void GetDisplayName_Should_Return_Correct_DisplayName()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                },
                new TeamMapping
                {
                    OriginalTeamName = "Commerce",
                    DisplayName = "商務團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result1 = service.GetDisplayName("MoneyLogistic");
        var result2 = service.GetDisplayName("Commerce");

        // Assert
        result1.Should().Be("金流團隊");
        result2.Should().Be("商務團隊");
    }

    /// <summary>
    /// 測試無對應時返回原始名稱
    /// </summary>
    [Fact]
    public void GetDisplayName_Should_Return_Original_Name_When_No_Mapping()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result = service.GetDisplayName("UnknownTeam");

        // Assert
        result.Should().Be("UnknownTeam");
    }

    /// <summary>
    /// 測試大小寫不敏感比對
    /// </summary>
    [Fact]
    public void GetDisplayName_Should_Be_Case_Insensitive()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result1 = service.GetDisplayName("moneylogistic");
        var result2 = service.GetDisplayName("MONEYLOGISTIC");
        var result3 = service.GetDisplayName("MoneyLogistic");

        // Assert
        result1.Should().Be("金流團隊");
        result2.Should().Be("金流團隊");
        result3.Should().Be("金流團隊");
    }

    /// <summary>
    /// 測試 null 或空字串返回原始值
    /// </summary>
    [Fact]
    public void GetDisplayName_Should_Return_Original_For_Null_Or_Empty()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var nullResult = service.GetDisplayName(null);
        var emptyResult = service.GetDisplayName("");
        var whitespaceResult = service.GetDisplayName("   ");

        // Assert
        nullResult.Should().BeNull();
        emptyResult.Should().Be("");
        whitespaceResult.Should().Be("   ");
    }

    /// <summary>
    /// 測試空 TeamMapping 返回原始名稱
    /// </summary>
    [Fact]
    public void GetDisplayName_Should_Return_Original_When_Mappings_Are_Empty()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>()
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result = service.GetDisplayName("AnyTeam");

        // Assert
        result.Should().Be("AnyTeam");
    }

    // ========== IsFilteringEnabled 方法測試 ==========

    /// <summary>
    /// 測試有 TeamMapping 時返回 true
    /// </summary>
    [Fact]
    public void IsFilteringEnabled_Should_Return_True_When_Mappings_Exist()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result = service.IsFilteringEnabled();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 測試無 TeamMapping 時返回 false
    /// </summary>
    [Fact]
    public void IsFilteringEnabled_Should_Return_False_When_Mappings_Are_Empty()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>()
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act
        var result = service.IsFilteringEnabled();

        // Assert
        result.Should().BeFalse();
    }

    // ========== 多個 Mapping 處理測試 ==========

    /// <summary>
    /// 測試多個團隊映射
    /// </summary>
    [Fact]
    public void Should_Handle_Multiple_Team_Mappings()
    {
        // Arrange
        var settings = new AzureDevOpsSettings
        {
            PersonalAccessToken = "test-token",
            OrganizationUrl = "https://dev.azure.com/test",
            ProjectName = "TestProject",
            TeamMapping = new List<TeamMapping>
            {
                new TeamMapping
                {
                    OriginalTeamName = "MoneyLogistic",
                    DisplayName = "金流團隊"
                },
                new TeamMapping
                {
                    OriginalTeamName = "DailyResource",
                    DisplayName = "日常資源團隊"
                },
                new TeamMapping
                {
                    OriginalTeamName = "Commerce",
                    DisplayName = "商務團隊"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new TeamMappingService(options);

        // Act & Assert
        service.HasMapping("MoneyLogistic").Should().BeTrue();
        service.HasMapping("DailyResource").Should().BeTrue();
        service.HasMapping("Commerce").Should().BeTrue();
        service.HasMapping("UnknownTeam").Should().BeFalse();

        service.GetDisplayName("MoneyLogistic").Should().Be("金流團隊");
        service.GetDisplayName("DailyResource").Should().Be("日常資源團隊");
        service.GetDisplayName("Commerce").Should().Be("商務團隊");
    }
}
