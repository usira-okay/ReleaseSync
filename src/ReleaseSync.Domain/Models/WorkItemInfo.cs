namespace ReleaseSync.Domain.Models;

/// <summary>
/// Work Item 資訊實體
/// </summary>
public class WorkItemInfo
{
    /// <summary>
    /// Work Item 識別碼
    /// </summary>
    public required WorkItemId Id { get; init; }

    /// <summary>
    /// Work Item 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Work Item 類型 (例如: User Story, Bug, Task, Epic)
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Work Item 狀態 (例如: New, Active, Resolved, Closed)
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Parent Work Item (若存在)
    /// </summary>
    public WorkItemInfo? ParentWorkItem { get; set; }

    /// <summary>
    /// Work Item URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// 指派給誰 (Assigned To)
    /// </summary>
    public string? AssignedTo { get; init; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// 原始團隊名稱 (來自 Azure DevOps Area Path,未經過 TeamMapping 轉換)
    /// </summary>
    /// <remarks>
    /// 用於 TeamMapping 過濾判斷。
    /// 此為 Azure DevOps 中的原始團隊名稱。
    /// </remarks>
    public string? OriginalTeamName { get; init; }

    /// <summary>
    /// 團隊顯示名稱 (已經過 TeamMapping 轉換)
    /// </summary>
    /// <remarks>
    /// 用於報告顯示和 JSON 輸出。
    /// 如果有 TeamMapping 設定,此為轉換後的顯示名稱;否則為原始名稱。
    /// </remarks>
    public string? Team { get; init; }

    /// <summary>
    /// 驗證實體是否有效
    /// </summary>
    public void Validate()
    {
        Id?.Validate();

        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title 不能為空");

        if (string.IsNullOrWhiteSpace(Type))
            throw new ArgumentException("Type 不能為空");

        if (string.IsNullOrWhiteSpace(State))
            throw new ArgumentException("State 不能為空");

        // 允許合理的時間誤差 (5分鐘) 以避免時區或時鐘同步問題
        var maxAllowedTime = DateTime.UtcNow.AddMinutes(5);
        if (CreatedAt > maxAllowedTime)
            throw new ArgumentException($"CreatedAt 不能為未來時間: {CreatedAt} (目前 UTC: {DateTime.UtcNow})");

        if (UpdatedAt < CreatedAt)
            throw new ArgumentException("UpdatedAt 不能早於 CreatedAt");
    }

    /// <summary>
    /// 檢查是否有 Parent Work Item
    /// </summary>
    public bool HasParent => ParentWorkItem != null;

    /// <summary>
    /// 取得完整的 Work Item 階層路徑 (Parent > Child > GrandChild)
    /// </summary>
    public string GetHierarchyPath()
    {
        if (!HasParent || ParentWorkItem is null)
            return $"{Id} - {Title}";

        return $"{ParentWorkItem.GetHierarchyPath()} > {Id} - {Title}";
    }
}
