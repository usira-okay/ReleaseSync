namespace ReleaseSync.Domain.Models;

/// <summary>
/// Pull Request / Merge Request 資訊實體
/// </summary>
public class PullRequestInfo
{
    /// <summary>
    /// 平台類型 (GitLab, BitBucket)
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// PR/MR 在平台上的唯一識別碼
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// PR/MR 編號 (通常為數字)
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 來源分支名稱
    /// </summary>
    public required BranchName SourceBranch { get; init; }

    /// <summary>
    /// 目標分支名稱
    /// </summary>
    public required BranchName TargetBranch { get; init; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 合併時間 (UTC),若未合併則為 null
    /// </summary>
    public DateTime? MergedAt { get; init; }

    /// <summary>
    /// 狀態 (Open, Merged, Declined, Closed)
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// 作者在版控平台的使用者 ID (GitLab: 數字 ID, BitBucket: UUID)
    /// </summary>
    public string? AuthorUserId { get; init; }

    /// <summary>
    /// 作者顯示名稱
    /// </summary>
    public string? AuthorDisplayName { get; init; }

    /// <summary>
    /// Repository 名稱 (例如: owner/repo)
    /// </summary>
    public required string RepositoryName { get; init; }

    /// <summary>
    /// 關聯的 Work Item (若有從 Branch 名稱解析出)
    /// </summary>
    public WorkItemInfo? AssociatedWorkItem { get; set; }

    /// <summary>
    /// PR/MR 的 URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// 驗證實體是否有效
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Platform))
            throw new ArgumentException("Platform 不能為空");

        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Id 不能為空");

        if (Number <= 0)
            throw new ArgumentException($"Number 必須為正整數: {Number}");

        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title 不能為空");

        SourceBranch?.Validate();
        TargetBranch?.Validate();

        // 允許合理的時間誤差 (5分鐘) 以避免時區或時鐘同步問題
        var maxAllowedTime = DateTime.UtcNow.AddMinutes(5);
        if (CreatedAt > maxAllowedTime)
            throw new ArgumentException($"CreatedAt 不能為未來時間: {CreatedAt} (目前 UTC: {DateTime.UtcNow})");

        if (MergedAt.HasValue && MergedAt.Value < CreatedAt)
            throw new ArgumentException("MergedAt 不能早於 CreatedAt");
    }

    /// <summary>
    /// 是否已合併
    /// </summary>
    public bool IsMerged => State.Equals("Merged", StringComparison.OrdinalIgnoreCase) && MergedAt.HasValue;

    /// <summary>
    /// 計算 PR/MR 存活時間 (從建立到合併,或建立到現在)
    /// </summary>
    public TimeSpan GetLifetime()
    {
        var endTime = MergedAt ?? DateTime.UtcNow;
        return endTime - CreatedAt;
    }
}
