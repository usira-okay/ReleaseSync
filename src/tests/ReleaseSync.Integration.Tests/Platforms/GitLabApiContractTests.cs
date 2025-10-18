namespace ReleaseSync.Integration.Tests.Platforms;

using NGitLab;
using NGitLab.Models;
using FluentAssertions;

/// <summary>
/// GitLab API 回應結構契約測試
/// 驗證 NGitLab 函式庫的 MergeRequest 查詢功能
/// </summary>
public class GitLabApiContractTests
{
    /// <summary>
    /// 驗證 GitLabClient 可以正確建立連線
    /// </summary>
    [Fact]
    public void GitLabClient_ShouldBeCreatable_WithValidToken()
    {
        // Arrange
        var hostUrl = "https://gitlab.com";
        var token = "dummy-token"; // 不需要真實 token 即可建立物件

        // Act
        Action act = () => new GitLabClient(hostUrl, token);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 驗證 MergeRequest 模型包含所需屬性
    /// </summary>
    [Fact]
    public void MergeRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act - 使用反射檢查 MergeRequest 類型
        var mergeRequestType = typeof(MergeRequest);

        // Assert - 驗證所有關鍵屬性存在
        mergeRequestType.Should().HaveProperty("Id");
        mergeRequestType.Should().HaveProperty("Iid"); // Internal ID (Number)
        mergeRequestType.Should().HaveProperty("Title");
        mergeRequestType.Should().HaveProperty("Description");
        mergeRequestType.Should().HaveProperty("SourceBranch");
        mergeRequestType.Should().HaveProperty("TargetBranch");
        mergeRequestType.Should().HaveProperty("CreatedAt");
        mergeRequestType.Should().HaveProperty("MergedAt");
        mergeRequestType.Should().HaveProperty("State");
        mergeRequestType.Should().HaveProperty("Author");
        mergeRequestType.Should().HaveProperty("WebUrl");
    }

    /// <summary>
    /// 驗證 Author 模型包含使用者資訊屬性
    /// </summary>
    [Fact]
    public void Author_ShouldHaveUserInfoProperties()
    {
        // Arrange & Act
        var authorType = typeof(Author);

        // Assert
        authorType.Should().HaveProperty("Username");
        authorType.Should().HaveProperty("Name"); // Display Name
    }

    /// <summary>
    /// 驗證 MergeRequestQuery 支援日期篩選參數
    /// </summary>
    [Fact]
    public void MergeRequestQuery_ShouldSupportDateFiltering()
    {
        // Arrange & Act
        var queryType = typeof(MergeRequestQuery);

        // Assert - 驗證查詢物件可以設定日期篩選
        // NGitLab 使用 CreatedAfter 和 CreatedBefore 進行日期篩選
        queryType.Should().HaveProperty("CreatedAfter");
        queryType.Should().HaveProperty("CreatedBefore");
        queryType.Should().HaveProperty("State"); // 用於篩選 merged/opened 等狀態
    }

    /// <summary>
    /// 驗證 MergeRequest 狀態列舉包含預期值
    /// </summary>
    [Fact]
    public void MergeRequestState_ShouldContainExpectedValues()
    {
        // Arrange & Act
        var stateType = typeof(MergeRequestState);

        // Assert - NGitLab.Models.MergeRequestState 是列舉
        stateType.Should().BeEnum();

        // 驗證關鍵狀態值存在
        var stateNames = Enum.GetNames(stateType);
        stateNames.Should().Contain("opened");
        stateNames.Should().Contain("merged");
        stateNames.Should().Contain("closed");
    }

    /// <summary>
    /// 驗證 ProjectId 可以從字串建立
    /// </summary>
    [Fact]
    public void ProjectId_ShouldBeCreatableFromString()
    {
        // Arrange
        var projectPath = "group/project";

        // Act
        Action act = () => new ProjectId(projectPath);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// 驗證 IMergeRequestClient 介面包含查詢方法
    /// </summary>
    [Fact]
    public void IMergeRequestClient_ShouldHaveQueryMethods()
    {
        // Arrange & Act
        var clientType = typeof(IMergeRequestClient);

        // Assert
        // 驗證包含 GetAll 方法 (用於查詢 MergeRequests)
        var getAllMethod = clientType.GetMethod("GetAll");
        getAllMethod.Should().NotBeNull();

        // 驗證方法參數接受 MergeRequestQuery
        var parameters = getAllMethod!.GetParameters();
        parameters.Should().ContainSingle(p => p.ParameterType == typeof(MergeRequestQuery));
    }
}
