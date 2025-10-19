using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace ReleaseSync.Integration.Tests.Platforms;

/// <summary>
/// Azure DevOps Work Item API 契約測試
/// </summary>
/// <remarks>
/// 此測試需要有效的 Azure DevOps PAT 才能執行
/// 可透過環境變數 AZURE_DEVOPS_PAT 或 appsettings.test.secure.json 提供
/// </remarks>
public class AzureDevOpsApiContractTests
{
    private readonly HttpClient _httpClient;
    private readonly string _organizationUrl;
    private readonly string _projectName;
    private readonly bool _isConfigured;

    public AzureDevOpsApiContractTests()
    {
        _organizationUrl = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG_URL")
            ?? "https://dev.azure.com/test-org";
        _projectName = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PROJECT")
            ?? "TestProject";

        var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT")
            ?? "test-pat";

        _isConfigured = !pat.StartsWith("test-");

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_organizationUrl)
        };

        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authHeader);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 測試 Azure DevOps Work Item API 回應結構 (需要有效的 PAT)
    /// </summary>
    [Fact(Skip = "需要有效的 Azure DevOps PAT,僅在本機手動測試時啟用")]
    public async Task Should_Match_WorkItem_API_Response_Schema()
    {
        // Arrange
        if (!_isConfigured)
        {
            Assert.Fail("Azure DevOps 組態未設定,無法執行此測試");
        }

        // 使用一個已知存在的 Work Item ID (通常專案都會有 ID=1)
        var workItemId = 1;
        var apiUrl = $"/{_projectName}/_apis/wit/workitems/{workItemId}?api-version=7.1";

        // Act
        var response = await _httpClient.GetAsync(apiUrl);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            // 驗證基本結構
            root.TryGetProperty("id", out _).Should().BeTrue("應包含 id 欄位");
            root.TryGetProperty("fields", out var fields).Should().BeTrue("應包含 fields 欄位");
            root.TryGetProperty("url", out _).Should().BeTrue("應包含 url 欄位");

            // 驗證 fields 結構
            fields.TryGetProperty("System.WorkItemType", out _).Should().BeTrue("應包含 Work Item 類型");
            fields.TryGetProperty("System.State", out _).Should().BeTrue("應包含 Work Item 狀態");
            fields.TryGetProperty("System.Title", out _).Should().BeTrue("應包含 Work Item 標題");
        }
    }

    /// <summary>
    /// 測試 Azure DevOps Work Item API 在無效 ID 時應回傳 404
    /// </summary>
    [Fact]
    public async Task Should_Return_404_For_NonExistent_WorkItem()
    {
        // Arrange - 使用測試組態 (不需要真實 PAT)
        var nonExistentId = 999999999;
        var apiUrl = $"/{_projectName}/_apis/wit/workitems/{nonExistentId}?api-version=7.1";

        // Act
        var response = await _httpClient.GetAsync(apiUrl);

        // Assert - 無論組態是否正確,不存在的 Work Item 應回傳 404 或 401 (未授權)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NonAuthoritativeInformation);  // 203 for Basic Auth failure
    }

    /// <summary>
    /// 測試 Azure DevOps Batch Work Items API 回應結構 (需要有效的 PAT)
    /// </summary>
    [Fact(Skip = "需要有效的 Azure DevOps PAT,僅在本機手動測試時啟用")]
    public async Task Should_Match_Batch_WorkItems_API_Response_Schema()
    {
        // Arrange
        if (!_isConfigured)
        {
            Assert.Fail("Azure DevOps 組態未設定,無法執行此測試");
        }

        var workItemIds = new[] { 1, 2, 3 };
        var idsParam = string.Join(",", workItemIds);
        var apiUrl = $"/{_projectName}/_apis/wit/workitems?ids={idsParam}&api-version=7.1";

        // Act
        var response = await _httpClient.GetAsync(apiUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        // 驗證基本結構
        root.TryGetProperty("count", out var count).Should().BeTrue("應包含 count 欄位");
        root.TryGetProperty("value", out var value).Should().BeTrue("應包含 value 陣列");

        value.ValueKind.Should().Be(JsonValueKind.Array, "value 應為陣列");

        if (value.GetArrayLength() > 0)
        {
            var firstItem = value[0];
            firstItem.TryGetProperty("id", out _).Should().BeTrue();
            firstItem.TryGetProperty("fields", out _).Should().BeTrue();
            firstItem.TryGetProperty("url", out _).Should().BeTrue();
        }
    }

    /// <summary>
    /// 測試 Azure DevOps API 版本支援
    /// </summary>
    [Fact]
    public async Task Should_Support_API_Version_7_1()
    {
        // Arrange
        var workItemId = 1;
        var apiUrl = $"/{_projectName}/_apis/wit/workitems/{workItemId}?api-version=7.1";

        // Act
        var response = await _httpClient.GetAsync(apiUrl);

        // Assert - 應支援 API 版本 7.1 (回傳 200 或 401/404,但不應回傳 400 Bad Request)
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest,
            "API 版本 7.1 應被支援");
    }

    /// <summary>
    /// 測試 Azure DevOps API 認證失敗時應回傳 401 或 203
    /// </summary>
    [Fact]
    public async Task Should_Return_Unauthorized_With_Invalid_PAT()
    {
        // Arrange - 使用無效的 PAT
        var invalidClient = new HttpClient
        {
            BaseAddress = new Uri(_organizationUrl)
        };

        var invalidAuthHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes(":invalid-pat"));
        invalidClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", invalidAuthHeader);

        var apiUrl = $"/{_projectName}/_apis/wit/workitems/1?api-version=7.1";

        // Act
        var response = await invalidClient.GetAsync(apiUrl);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NonAuthoritativeInformation);  // 203 for Basic Auth failure
    }

    /// <summary>
    /// 測試 Work Item Relations API 回應結構 (需要有效的 PAT)
    /// </summary>
    [Fact(Skip = "需要有效的 Azure DevOps PAT,僅在本機手動測試時啟用")]
    public async Task Should_Include_Relations_In_WorkItem_Response_When_Expanded()
    {
        // Arrange
        if (!_isConfigured)
        {
            Assert.Fail("Azure DevOps 組態未設定,無法執行此測試");
        }

        var workItemId = 1;
        var apiUrl = $"/{_projectName}/_apis/wit/workitems/{workItemId}?$expand=relations&api-version=7.1";

        // Act
        var response = await _httpClient.GetAsync(apiUrl);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            // 驗證 relations 欄位存在 (即使為空陣列)
            root.TryGetProperty("relations", out var relations).Should().BeTrue(
                "使用 $expand=relations 參數時應包含 relations 欄位");

            if (relations.ValueKind == JsonValueKind.Array && relations.GetArrayLength() > 0)
            {
                var firstRelation = relations[0];
                firstRelation.TryGetProperty("rel", out _).Should().BeTrue("relation 應包含 rel 欄位");
                firstRelation.TryGetProperty("url", out _).Should().BeTrue("relation 應包含 url 欄位");
            }
        }
    }
}
