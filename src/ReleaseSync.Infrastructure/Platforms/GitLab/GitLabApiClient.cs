using Microsoft.Extensions.Logging;
using NGitLab;
using NGitLab.Models;

namespace ReleaseSync.Infrastructure.Platforms.GitLab;

/// <summary>
/// GitLab API 用戶端,封裝 NGitLab 函式庫
/// </summary>
public class GitLabApiClient
{
    private readonly GitLabClient _client;
    private readonly ILogger<GitLabApiClient> _logger;

    /// <summary>
    /// 建立 GitLabApiClient
    /// </summary>
    /// <param name="apiUrl">GitLab API URL (例如: https://gitlab.com/api/v4 或 https://gitlab.com)</param>
    /// <param name="personalAccessToken">Personal Access Token</param>
    /// <param name="logger">日誌記錄器</param>
    public GitLabApiClient(string apiUrl, string personalAccessToken, ILogger<GitLabApiClient> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl, nameof(apiUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(personalAccessToken, nameof(personalAccessToken));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // NGitLab 建立連線 (支援 Personal Access Token)
        _client = new GitLabClient(apiUrl, personalAccessToken);

        _logger.LogInformation("GitLabApiClient 已初始化: {ApiUrl}", apiUrl);
    }

    /// <summary>
    /// 取得指定專案的 Merge Requests
    /// </summary>
    /// <param name="projectPath">專案路徑 (例如: mygroup/myproject)</param>
    /// <param name="startDate">起始日期 (包含)</param>
    /// <param name="endDate">結束日期 (包含)</param>
    /// <param name="targetBranches">目標分支清單 (若為空則查詢所有分支)</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Merge Request 清單</returns>
    public async Task<IEnumerable<MergeRequest>> GetMergeRequestsAsync(
        string projectPath,
        DateTime startDate,
        DateTime endDate,
        IEnumerable<string>? targetBranches,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("開始抓取 GitLab MR - 專案: {ProjectPath}, 時間範圍: {StartDate} ~ {EndDate}",
                projectPath, startDate, endDate);

            // 取得專案
            var project = _client.Projects[projectPath];
            if (project == null)
            {
                _logger.LogWarning("找不到 GitLab 專案: {ProjectPath}", projectPath);
                return Enumerable.Empty<MergeRequest>();
            }

            // 查詢 Merge Requests (只查詢已合併的 MR)
            // 使用 UpdatedAfter/UpdatedBefore 先拉取資料，再用 MergedAt 過濾
            // 這樣可以確保不會遺漏在時間範圍內合併但較早建立的 MR
            var query = new MergeRequestQuery
            {
                State = MergeRequestState.merged,
                UpdatedAfter = startDate,
                UpdatedBefore = endDate,
                PerPage = 100 // 每頁 100 筆
            };

            // 使用 MergeRequest API 查詢
            var mergeRequestClient = _client.GetMergeRequest(project.Id);
            var mergeRequests = await Task.Run(() =>
                mergeRequestClient.Get(query).ToList(),
                cancellationToken);

            _logger.LogInformation("從 API 取得 {Count} 筆 MR (UpdatedAfter 過濾後) - 專案: {ProjectPath}",
                mergeRequests.Count, projectPath);

            // 使用 MergedAt 時間進行二次過濾，確保只包含在指定時間範圍內合併的 MR
            mergeRequests = mergeRequests
                .Where(mr => mr.MergedAt.HasValue &&
                             mr.MergedAt.Value >= startDate &&
                             mr.MergedAt.Value < endDate)
                .ToList();

            _logger.LogInformation("經 MergedAt 過濾後剩餘 {Count} 筆 MR - 專案: {ProjectPath}",
                mergeRequests.Count, projectPath);

            // 如果有指定目標分支,進行過濾
            if (targetBranches != null && targetBranches.Any())
            {
                var targetBranchList = targetBranches.ToList();
                mergeRequests = mergeRequests
                    .Where(mr => targetBranchList.Contains(mr.TargetBranch))
                    .ToList();

                _logger.LogInformation("已過濾目標分支: {TargetBranches}, 剩餘 {Count} 筆 MR",
                    string.Join(", ", targetBranchList), mergeRequests.Count);
            }

            _logger.LogInformation("成功抓取 {Count} 筆 GitLab MR - 專案: {ProjectPath}",
                mergeRequests.Count, projectPath);

            return mergeRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "抓取 GitLab MR 失敗 - 專案: {ProjectPath}", projectPath);
            throw new InvalidOperationException($"抓取 GitLab MR 失敗: {projectPath}", ex);
        }
    }

    /// <summary>
    /// 測試連線是否正常
    /// </summary>
    /// <returns>是否連線成功</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // 嘗試取得當前使用者資訊以驗證 Token
            await Task.Run(() => _client.Users.Current);
            _logger.LogInformation("GitLab 連線測試成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitLab 連線測試失敗");
            return false;
        }
    }
}
