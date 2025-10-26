using FluentAssertions;
using Microsoft.Extensions.Options;
using ReleaseSync.Infrastructure.Configuration;
using ReleaseSync.Infrastructure.Services;

namespace ReleaseSync.Infrastructure.UnitTests.Services;

/// <summary>
/// UserMappingService 單元測試
/// </summary>
public class UserMappingServiceTests
{
    /// <summary>
    /// 測試 GitLab 使用者映射成功
    /// </summary>
    [Fact]
    public void Should_Map_GitLab_User_To_DisplayName()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("GitLab", "john.doe", "John D.");

        // Assert
        result.Should().Be("John Doe");
    }

    /// <summary>
    /// 測試 BitBucket 使用者映射成功
    /// </summary>
    [Fact]
    public void Should_Map_BitBucket_User_To_DisplayName()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("BitBucket", "jdoe", "J. Doe");

        // Assert
        result.Should().Be("John Doe");
    }

    /// <summary>
    /// 測試當無映射時應回傳預設值
    /// </summary>
    [Fact]
    public void Should_Return_Default_DisplayName_When_No_Mapping_Found()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("GitLab", "jane.smith", "Jane Smith");

        // Assert
        result.Should().Be("Jane Smith");
    }

    /// <summary>
    /// 測試當無映射且無預設值時應回傳使用者名稱
    /// </summary>
    [Fact]
    public void Should_Return_Username_When_No_Mapping_And_No_Default()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>()
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("GitLab", "jane.smith");

        // Assert
        result.Should().Be("jane.smith");
    }

    /// <summary>
    /// 測試空使用者名稱應回傳預設值或 "Unknown"
    /// </summary>
    [Fact]
    public void Should_Return_Default_Or_Unknown_When_Username_Is_Empty()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>()
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var resultWithDefault = service.GetDisplayName("GitLab", "", "Default User");
        var resultWithoutDefault = service.GetDisplayName("GitLab", "");

        // Assert
        resultWithDefault.Should().Be("Default User");
        resultWithoutDefault.Should().Be("Unknown");
    }

    /// <summary>
    /// 測試空白使用者名稱應回傳預設值或 "Unknown"
    /// </summary>
    [Fact]
    public void Should_Return_Default_Or_Unknown_When_Username_Is_Whitespace()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>()
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("GitLab", "   ", "Default User");

        // Assert
        result.Should().Be("Default User");
    }

    /// <summary>
    /// 測試大小寫不敏感的使用者名稱匹配
    /// </summary>
    [Fact]
    public void Should_Match_Username_Case_Insensitive()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result1 = service.GetDisplayName("GitLab", "JOHN.DOE");
        var result2 = service.GetDisplayName("GitLab", "John.Doe");
        var result3 = service.GetDisplayName("BitBucket", "JDOE");

        // Assert
        result1.Should().Be("John Doe");
        result2.Should().Be("John Doe");
        result3.Should().Be("John Doe");
    }

    /// <summary>
    /// 測試不區分平台大小寫
    /// </summary>
    [Fact]
    public void Should_Match_Platform_Case_Insensitive()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result1 = service.GetDisplayName("gitlab", "john.doe");
        var result2 = service.GetDisplayName("GITLAB", "john.doe");
        var result3 = service.GetDisplayName("bitbucket", "jdoe");
        var result4 = service.GetDisplayName("BITBUCKET", "jdoe");

        // Assert
        result1.Should().Be("John Doe");
        result2.Should().Be("John Doe");
        result3.Should().Be("John Doe");
        result4.Should().Be("John Doe");
    }

    /// <summary>
    /// 測試未知平台應回傳預設值
    /// </summary>
    [Fact]
    public void Should_Return_Default_When_Platform_Is_Unknown()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("GitHub", "john.doe", "John GitHub");

        // Assert
        result.Should().Be("John GitHub");
    }

    /// <summary>
    /// 測試多個映射配置
    /// </summary>
    [Fact]
    public void Should_Handle_Multiple_Mappings()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                },
                new UserMapping
                {
                    GitLabUserId = "jane.smith",
                    BitBucketUserId = "jsmith",
                    DisplayName = "Jane Smith"
                },
                new UserMapping
                {
                    GitLabUserId = "bob.jones",
                    BitBucketUserId = "bjones",
                    DisplayName = "Bob Jones"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result1 = service.GetDisplayName("GitLab", "john.doe");
        var result2 = service.GetDisplayName("GitLab", "jane.smith");
        var result3 = service.GetDisplayName("BitBucket", "bjones");

        // Assert
        result1.Should().Be("John Doe");
        result2.Should().Be("Jane Smith");
        result3.Should().Be("Bob Jones");
    }

    /// <summary>
    /// 測試只有 GitLab ID 的映射
    /// </summary>
    [Fact]
    public void Should_Handle_Mapping_With_Only_GitLab_Id()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = null,
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var gitlabResult = service.GetDisplayName("GitLab", "john.doe");
        var bitbucketResult = service.GetDisplayName("BitBucket", "john.doe", "Default");

        // Assert
        gitlabResult.Should().Be("John Doe");
        bitbucketResult.Should().Be("Default");
    }

    /// <summary>
    /// 測試只有 BitBucket ID 的映射
    /// </summary>
    [Fact]
    public void Should_Handle_Mapping_With_Only_BitBucket_Id()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = null,
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var bitbucketResult = service.GetDisplayName("BitBucket", "jdoe");
        var gitlabResult = service.GetDisplayName("GitLab", "jdoe", "Default");

        // Assert
        bitbucketResult.Should().Be("John Doe");
        gitlabResult.Should().Be("Default");
    }

    /// <summary>
    /// 測試空映射清單
    /// </summary>
    [Fact]
    public void Should_Handle_Empty_Mappings_List()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>()
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("GitLab", "john.doe", "Default Name");

        // Assert
        result.Should().Be("Default Name");
    }

    /// <summary>
    /// 測試中文使用者名稱映射
    /// </summary>
    [Fact]
    public void Should_Handle_Chinese_DisplayName()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "zhang.san",
                    BitBucketUserId = "zsan",
                    DisplayName = "張三"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.GetDisplayName("GitLab", "zhang.san");

        // Assert
        result.Should().Be("張三");
    }

    // ========== T009: HasMapping 方法測試 ==========

    /// <summary>
    /// 測試已對應使用者返回 true
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_True_For_Mapped_User()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var gitlabResult = service.HasMapping("GitLab", "john.doe");
        var bitbucketResult = service.HasMapping("BitBucket", "jdoe");

        // Assert
        gitlabResult.Should().BeTrue();
        bitbucketResult.Should().BeTrue();
    }

    /// <summary>
    /// 測試未對應使用者返回 false
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_False_For_Unmapped_User()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var gitlabResult = service.HasMapping("GitLab", "jane.smith");
        var bitbucketResult = service.HasMapping("BitBucket", "jsmith");

        // Assert
        gitlabResult.Should().BeFalse();
        bitbucketResult.Should().BeFalse();
    }

    /// <summary>
    /// 測試空 UserMapping 返回 true (向後相容)
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_True_When_Mappings_Are_Empty()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>()
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var gitlabResult = service.HasMapping("GitLab", "anyone");
        var bitbucketResult = service.HasMapping("BitBucket", "anyone");

        // Assert
        gitlabResult.Should().BeTrue("空 Mapping 時應不過濾任何使用者 (向後相容)");
        bitbucketResult.Should().BeTrue("空 Mapping 時應不過濾任何使用者 (向後相容)");
    }

    /// <summary>
    /// 測試大小寫不敏感比對
    /// </summary>
    [Fact]
    public void HasMapping_Should_Be_Case_Insensitive()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result1 = service.HasMapping("GitLab", "JOHN.DOE");
        var result2 = service.HasMapping("GitLab", "John.Doe");
        var result3 = service.HasMapping("BitBucket", "JDOE");
        var result4 = service.HasMapping("bitbucket", "JDoe");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
        result4.Should().BeTrue();
    }

    /// <summary>
    /// 測試 null 或空字串使用者名稱返回 false
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_False_For_Null_Or_Empty_Username()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var nullResult = service.HasMapping("GitLab", null);
        var emptyResult = service.HasMapping("GitLab", "");
        var whitespaceResult = service.HasMapping("GitLab", "   ");

        // Assert
        nullResult.Should().BeFalse();
        emptyResult.Should().BeFalse();
        whitespaceResult.Should().BeFalse();
    }

    /// <summary>
    /// 測試未知平台返回 false
    /// </summary>
    [Fact]
    public void HasMapping_Should_Return_False_For_Unknown_Platform()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.HasMapping("GitHub", "john.doe");

        // Assert
        result.Should().BeFalse();
    }

    // ========== IsFilteringEnabled 方法測試 ==========

    /// <summary>
    /// 測試有 Mapping 時 IsFilteringEnabled 返回 true
    /// </summary>
    [Fact]
    public void IsFilteringEnabled_Should_Return_True_When_Mappings_Exist()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>
            {
                new UserMapping
                {
                    GitLabUserId = "john.doe",
                    BitBucketUserId = "jdoe",
                    DisplayName = "John Doe"
                }
            }
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.IsFilteringEnabled();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 測試無 Mapping 時 IsFilteringEnabled 返回 false
    /// </summary>
    [Fact]
    public void IsFilteringEnabled_Should_Return_False_When_Mappings_Are_Empty()
    {
        // Arrange
        var settings = new UserMappingSettings
        {
            Mappings = new List<UserMapping>()
        };

        var options = Options.Create(settings);
        var service = new UserMappingService(options);

        // Act
        var result = service.IsFilteringEnabled();

        // Assert
        result.Should().BeFalse();
    }
}
