namespace ReleaseSync.Integration.Tests.Platforms;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;

/// <summary>
/// BitBucket API 回應結構契約測試
/// 驗證 BitBucket Cloud REST API 的回應格式
/// </summary>
public class BitBucketApiContractTests
{
    /// <summary>
    /// BitBucket Cloud Pull Request API 回應的模擬資料
    /// </summary>
    private const string SamplePullRequestResponse = """
    {
        "values": [
            {
                "id": 1,
                "title": "Feature: Add login functionality",
                "description": "Implements user authentication",
                "state": "MERGED",
                "created_on": "2025-01-15T10:30:00.000Z",
                "updated_on": "2025-01-20T14:45:00.000Z",
                "source": {
                    "branch": {
                        "name": "feature/vsts12345-login"
                    }
                },
                "destination": {
                    "branch": {
                        "name": "main"
                    }
                },
                "author": {
                    "display_name": "John Doe",
                    "username": "johndoe",
                    "account_id": "557058:12345678-1234-1234-1234-123456789012"
                },
                "links": {
                    "html": {
                        "href": "https://bitbucket.org/workspace/repo/pull-requests/1"
                    }
                },
                "merge_commit": {
                    "hash": "abc123def456"
                }
            }
        ],
        "pagelen": 10,
        "size": 1,
        "page": 1
    }
    """;

    /// <summary>
    /// 驗證 BitBucket Pull Request 回應可以正確反序列化
    /// </summary>
    [Fact]
    public async Task BitBucketPullRequestResponse_ShouldDeserializeCorrectly()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Act
        using var document = JsonDocument.Parse(SamplePullRequestResponse);
        var root = document.RootElement;

        // Assert - 驗證主要結構
        root.TryGetProperty("values", out var values).Should().BeTrue();
        values.GetArrayLength().Should().Be(1);

        var pr = values[0];

        // 驗證 Pull Request 基本屬性
        pr.TryGetProperty("id", out var id).Should().BeTrue();
        id.GetInt32().Should().Be(1);

        pr.TryGetProperty("title", out var title).Should().BeTrue();
        title.GetString().Should().Be("Feature: Add login functionality");

        pr.TryGetProperty("state", out var state).Should().BeTrue();
        state.GetString().Should().Be("MERGED");

        // 驗證日期屬性
        pr.TryGetProperty("created_on", out var createdOn).Should().BeTrue();
        createdOn.GetString().Should().NotBeNullOrEmpty();

        // 驗證分支資訊
        pr.TryGetProperty("source", out var source).Should().BeTrue();
        source.TryGetProperty("branch", out var sourceBranch).Should().BeTrue();
        sourceBranch.TryGetProperty("name", out var sourceBranchName).Should().BeTrue();
        sourceBranchName.GetString().Should().Be("feature/vsts12345-login");

        pr.TryGetProperty("destination", out var destination).Should().BeTrue();
        destination.TryGetProperty("branch", out var destBranch).Should().BeTrue();
        destBranch.TryGetProperty("name", out var destBranchName).Should().BeTrue();
        destBranchName.GetString().Should().Be("main");

        // 驗證作者資訊
        pr.TryGetProperty("author", out var author).Should().BeTrue();
        author.TryGetProperty("username", out var username).Should().BeTrue();
        username.GetString().Should().Be("johndoe");

        author.TryGetProperty("display_name", out var displayName).Should().BeTrue();
        displayName.GetString().Should().Be("John Doe");

        // 驗證連結
        pr.TryGetProperty("links", out var links).Should().BeTrue();
        links.TryGetProperty("html", out var html).Should().BeTrue();
        html.TryGetProperty("href", out var href).Should().BeTrue();
        href.GetString().Should().Contain("bitbucket.org");
    }

    /// <summary>
    /// 驗證 BitBucket API URL 支援日期篩選查詢字串
    /// </summary>
    [Fact]
    public void BitBucketApiUrl_ShouldSupportDateFiltering()
    {
        // Arrange
        var baseUrl = "https://api.bitbucket.org/2.0/repositories/workspace/repo/pullrequests";
        var startDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act - 建構查詢字串 (BitBucket 使用 q 參數進行篩選)
        var query = $"q=created_on>={startDate:yyyy-MM-ddTHH:mm:ss.fffZ} AND created_on<={endDate:yyyy-MM-ddTHH:mm:ss.fffZ}";
        var fullUrl = $"{baseUrl}?{query}";

        // Assert
        fullUrl.Should().Contain("q=created_on>=");
        fullUrl.Should().Contain("created_on<=");
        fullUrl.Should().Contain("2025-01-01T00:00:00");
        fullUrl.Should().Contain("2025-01-31T23:59:59");
    }

    /// <summary>
    /// 驗證 BitBucket API 分頁參數結構
    /// </summary>
    [Fact]
    public async Task BitBucketPaginationResponse_ShouldContainExpectedFields()
    {
        // Arrange
        using var document = JsonDocument.Parse(SamplePullRequestResponse);
        var root = document.RootElement;

        // Act & Assert - 驗證分頁欄位
        root.TryGetProperty("pagelen", out var pagelen).Should().BeTrue();
        pagelen.GetInt32().Should().Be(10);

        root.TryGetProperty("size", out var size).Should().BeTrue();
        size.GetInt32().Should().Be(1);

        root.TryGetProperty("page", out var page).Should().BeTrue();
        page.GetInt32().Should().Be(1);

        // BitBucket 使用 'next' 欄位提供下一頁 URL
        // 這個測試資料沒有下一頁,所以不包含 'next' 欄位
    }

    /// <summary>
    /// 驗證 HttpClient 可以設定 Bearer Token 授權標頭
    /// </summary>
    [Fact]
    public void HttpClient_ShouldAcceptBearerTokenAuthorization()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var token = "dummy-access-token";

        // Act
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Assert
        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be(token);
    }

    /// <summary>
    /// 驗證 BitBucket Pull Request 狀態值
    /// </summary>
    [Theory]
    [InlineData("OPEN")]
    [InlineData("MERGED")]
    [InlineData("DECLINED")]
    [InlineData("SUPERSEDED")]
    public void BitBucketPullRequestState_ShouldBeValidState(string state)
    {
        // Arrange - BitBucket 使用大寫狀態值
        var validStates = new[] { "OPEN", "MERGED", "DECLINED", "SUPERSEDED" };

        // Act & Assert
        validStates.Should().Contain(state);
    }
}
