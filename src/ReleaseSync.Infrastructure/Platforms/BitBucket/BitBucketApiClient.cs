using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReleaseSync.Infrastructure.Platforms.BitBucket.Models;

namespace ReleaseSync.Infrastructure.Platforms.BitBucket;

/// <summary>
/// BitBucket API 用戶端,使用 HttpClient 直接呼叫 REST API
/// </summary>
public class BitBucketApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BitBucketApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 建立 BitBucketApiClient
    /// </summary>
    /// <param name="httpClient">HTTP 用戶端 (應透過 IHttpClientFactory 建立)</param>
    /// <param name="appPassword">App Password 或 Access Token</param>
    /// <param name="username">使用者名稱 (使用 App Password 時需要)</param>
    /// <param name="logger">日誌記錄器</param>
    public BitBucketApiClient(
        HttpClient httpClient,
        string appPassword,
        string? username,
        ILogger<BitBucketApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(appPassword, nameof(appPassword));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 設定 Authorization header
        // 使用 Basic Auth (username:appPassword) 或 Bearer Token
        if (!string.IsNullOrWhiteSpace(username))
        {
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{username}:{appPassword}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", appPassword);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // JSON 序列化選項 (處理 BitBucket 的 snake_case 命名)
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        _logger.LogDebug("BitBucketApiClient 已初始化");
    }

    /// <summary>
    /// 取得指定 Repository 的 Pull Requests
    /// </summary>
    /// <param name="workspace">Workspace ID</param>
    /// <param name="repository">Repository 名稱</param>
    /// <param name="startDate">起始日期 (包含)</param>
    /// <param name="endDate">結束日期 (包含)</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Pull Request 清單</returns>
    public async Task<List<BitBucketPullRequest>> GetPullRequestsAsync(
        string workspace,
        string repository,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace, nameof(workspace));
        ArgumentException.ThrowIfNullOrWhiteSpace(repository, nameof(repository));

        try
        {
            _logger.LogInformation(
                "開始抓取 BitBucket PR - Workspace: {Workspace}, Repo: {Repository}, 時間範圍: {StartDate} ~ {EndDate}",
                workspace, repository, startDate, endDate);

            var allPullRequests = new List<BitBucketPullRequest>();
            string? nextPageUrl = BuildInitialUrl(workspace, repository, startDate);

            // 處理分頁
            while (!string.IsNullOrEmpty(nextPageUrl))
            {
                var response = await _httpClient.GetAsync(nextPageUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<BitBucketPullRequestsResponse>(json, _jsonOptions);

                if (result?.Values == null)
                {
                    break;
                }

                // 過濾時間範圍內的 PR
                var filteredPullRequests = result.Values
                    .Where(pr => pr.UpdatedOn >= startDate && pr.UpdatedOn <= endDate)
                    .ToList();

                allPullRequests.AddRange(filteredPullRequests);

                // 檢查是否有下一頁
                nextPageUrl = result.Next;

                // 如果所有 PR 的建立時間都早於 startDate,停止查詢
                if (result.Values.All(pr => pr.UpdatedOn < startDate))
                {
                    break;
                }

                _logger.LogDebug("已抓取 {Count} 筆 PR,繼續查詢下一頁", allPullRequests.Count);
            }

            _logger.LogInformation(
                "成功抓取 {Count} 筆 BitBucket PR - Workspace: {Workspace}, Repo: {Repository}",
                allPullRequests.Count, workspace, repository);

            return allPullRequests;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP 請求失敗 - Workspace: {Workspace}, Repo: {Repository}",
                workspace, repository);
            throw new InvalidOperationException(
                $"抓取 BitBucket PR 失敗: {workspace}/{repository}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "JSON 解析失敗 - Workspace: {Workspace}, Repo: {Repository}",
                workspace, repository);
            throw new InvalidOperationException(
                $"解析 BitBucket API 回應失敗: {workspace}/{repository}", ex);
        }
    }

    /// <summary>
    /// 建立初始查詢 URL
    /// </summary>
    private static string BuildInitialUrl(string workspace, string repository, DateTime startDate)
    {
        // BitBucket API 2.0 端點
        var baseUrl = $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repository}/pullrequests";

        // 使用 q 參數進行日期篩選 (updated_on >= startDate)
        var query = $"updated_on>={startDate:yyyy-MM-ddTHH:mm:ss.fffZ}";

        // 設定排序與分頁,只抓取已合併的 PR
        return $"{baseUrl}?q={Uri.EscapeDataString(query)}&sort=-updated_on&pagelen=50&state=MERGED";
    }

    /// <summary>
    /// 測試連線是否正常
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // 嘗試取得當前使用者資訊以驗證 Token
            var response = await _httpClient.GetAsync("https://api.bitbucket.org/2.0/user");
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("BitBucket 連線測試成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BitBucket 連線測試失敗");
            return false;
        }
    }
}

/// <summary>
/// BitBucket API 回應的分頁結構
/// </summary>
internal class BitBucketPullRequestsResponse
{
    public List<BitBucketPullRequest> Values { get; set; } = new();
    public string? Next { get; set; }
    public int? Size { get; set; }
}
