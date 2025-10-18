namespace ReleaseSync.Domain.Models;

/// <summary>
/// 同步結果聚合根
/// </summary>
public class SyncResult
{
    private readonly List<PullRequestInfo> _pullRequests = new();
    private readonly List<PlatformSyncStatus> _platformStatuses = new();

    /// <summary>
    /// 同步執行的時間範圍
    /// </summary>
    public required DateRange SyncDateRange { get; init; }

    /// <summary>
    /// 同步執行的開始時間 (UTC)
    /// </summary>
    public DateTime SyncStartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 同步執行的結束時間 (UTC)
    /// </summary>
    public DateTime? SyncCompletedAt { get; private set; }

    /// <summary>
    /// 所有抓取的 Pull Requests / Merge Requests
    /// </summary>
    public IReadOnlyList<PullRequestInfo> PullRequests => _pullRequests.AsReadOnly();

    /// <summary>
    /// 各平台的同步狀態
    /// </summary>
    public IReadOnlyList<PlatformSyncStatus> PlatformStatuses => _platformStatuses.AsReadOnly();

    /// <summary>
    /// 是否完全成功 (所有平台皆成功)
    /// </summary>
    public bool IsFullySuccessful =>
        _platformStatuses.Any() && _platformStatuses.All(s => s.IsSuccess);

    /// <summary>
    /// 是否部分成功 (至少一個平台成功)
    /// </summary>
    public bool IsPartiallySuccessful =>
        _platformStatuses.Any(s => s.IsSuccess);

    /// <summary>
    /// 總計抓取的 PR/MR 數量
    /// </summary>
    public int TotalPullRequestCount => _pullRequests.Count;

    /// <summary>
    /// 關聯到 Work Item 的 PR/MR 數量
    /// </summary>
    public int LinkedWorkItemCount =>
        _pullRequests.Count(pr => pr.AssociatedWorkItem != null);

    /// <summary>
    /// 新增 Pull Request
    /// </summary>
    public void AddPullRequest(PullRequestInfo pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);
        pullRequest.Validate();
        _pullRequests.Add(pullRequest);
    }

    /// <summary>
    /// 批次新增 Pull Requests
    /// </summary>
    public void AddPullRequests(IEnumerable<PullRequestInfo> pullRequests)
    {
        foreach (var pr in pullRequests)
        {
            AddPullRequest(pr);
        }
    }

    /// <summary>
    /// 記錄平台同步狀態
    /// </summary>
    public void RecordPlatformStatus(PlatformSyncStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _platformStatuses.Add(status);
    }

    /// <summary>
    /// 標記同步完成
    /// </summary>
    public void MarkAsCompleted()
    {
        SyncCompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 取得執行摘要
    /// </summary>
    public string GetSummary()
    {
        var successCount = _platformStatuses.Count(s => s.IsSuccess);
        var totalPlatforms = _platformStatuses.Count;
        var duration = SyncCompletedAt.HasValue
            ? (SyncCompletedAt.Value - SyncStartedAt).TotalSeconds
            : 0;

        return $"同步完成: {successCount}/{totalPlatforms} 平台成功, " +
               $"共抓取 {TotalPullRequestCount} 筆 PR/MR " +
               $"({LinkedWorkItemCount} 筆關聯到 Work Item), " +
               $"耗時 {duration:F2} 秒";
    }

    /// <summary>
    /// 取得失敗的平台清單
    /// </summary>
    public IEnumerable<PlatformSyncStatus> GetFailedPlatforms()
    {
        return _platformStatuses.Where(s => !s.IsSuccess);
    }
}
