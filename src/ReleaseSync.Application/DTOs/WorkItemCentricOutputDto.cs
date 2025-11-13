using System.Text.Json.Serialization;

namespace ReleaseSync.Application.DTOs;

/// <summary>
/// Work Item 為中心的輸出格式 DTO
/// 用於將同步結果以 Work Item 為主體輸出,PR/MR 作為子項目
/// </summary>
public class WorkItemCentricOutputDto
{
    /// <summary>
    /// 撈取開始時間
    /// </summary>
    [JsonPropertyName("startDate")]
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// 撈取結束時間
    /// </summary>
    [JsonPropertyName("endDate")]
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Work Item 列表(以 Work Item 為主體分組)
    /// </summary>
    [JsonPropertyName("workItems")]
    public required List<WorkItemWithPullRequestsDto> WorkItems { get; init; }

    /// <summary>
    /// 從 SyncResultDto 轉換為 Work Item 為中心的格式
    /// </summary>
    public static WorkItemCentricOutputDto FromSyncResult(SyncResultDto syncResult)
    {
        // 將 PR 按 Work Item 分組
        var workItemGroups = syncResult.PullRequests
            .Where(pr => pr.AssociatedWorkItem != null)
            .GroupBy(pr => pr.AssociatedWorkItem!.Id)
            .ToList();

        var workItems = workItemGroups.Select(group =>
        {
            var firstPr = group.First();
            var workItem = firstPr.AssociatedWorkItem!;

            // 取得 Parent User Story (如果存在)
            var parentUserStory = GetParentUserStory(workItem);

            return new WorkItemWithPullRequestsDto
            {
                WorkItemId = parentUserStory?.Id ?? workItem.Id,
                WorkItemTitle = parentUserStory?.Title ?? workItem.Title,
                WorkItemTeam = workItem.Team,
                WorkItemType = parentUserStory?.Type ?? workItem.Type,
                WorkItemUrl = parentUserStory?.Url ?? workItem.Url,
                PullRequests = group.Select(pr => new SimplifiedPullRequestDto
                {
                    Platform = pr.Platform,
                    Title = pr.Title,
                    SourceBranch = pr.SourceBranch,
                    TargetBranch = pr.TargetBranch,
                    MergedAt = pr.MergedAt,
                    AuthorUserId = pr.AuthorUserId,
                    AuthorDisplayName = pr.AuthorDisplayName,
                    Url = pr.Url,
                    RepositoryName = ExtractRepositoryName(pr.RepositoryName)
                }).ToList()
            };
        }).ToList();

        return new WorkItemCentricOutputDto
        {
            StartDate = syncResult.StartDate,
            EndDate = syncResult.EndDate,
            WorkItems = workItems
        };
    }

    /// <summary>
    /// 從完整的 Repository 路徑中提取 Repository 名稱
    /// 例如: "owner/repo" -> "repo", "single" -> "single"
    /// </summary>
    private static string ExtractRepositoryName(string repositoryName)
    {
        var parts = repositoryName.Split('/');
        return parts.Length == 2 ? parts[1] : repositoryName;
    }

    /// <summary>
    /// 取得 Parent User Story
    /// 遞迴查找,直到找到 Type 為 "User Story" 的 Parent
    /// </summary>
    private static WorkItemDto? GetParentUserStory(WorkItemDto workItem)
    {
        var current = workItem;
        while (current.ParentWorkItem != null)
        {
            if (current.ParentWorkItem.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
            {
                return current.ParentWorkItem;
            }
            current = current.ParentWorkItem;
        }

        // 如果自己是 User Story,返回自己
        if (workItem.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase))
        {
            return workItem;
        }

        return null;
    }
}

/// <summary>
/// Work Item 與其關聯的 Pull Requests
/// </summary>
public class WorkItemWithPullRequestsDto
{
    /// <summary>
    /// Work Item ID
    /// </summary>
    [JsonPropertyName("workItemId")]
    public required int WorkItemId { get; init; }

    /// <summary>
    /// Work Item 標題
    /// </summary>
    [JsonPropertyName("workItemTitle")]
    public required string WorkItemTitle { get; init; }

    /// <summary>
    /// Work Item 所屬團隊
    /// </summary>
    [JsonPropertyName("workItemTeam")]
    public required string? WorkItemTeam { get; init; }

    /// <summary>
    /// Work Item 類型
    /// </summary>
    [JsonPropertyName("workItemType")]
    public required string WorkItemType { get; init; }

    /// <summary>
    /// Work Item URL
    /// </summary>
    [JsonPropertyName("workItemUrl")]
    public required string? WorkItemUrl { get; init; }

    /// <summary>
    /// 關聯的 Pull Requests
    /// </summary>
    [JsonPropertyName("pullRequests")]
    public required List<SimplifiedPullRequestDto> PullRequests { get; init; }
}

/// <summary>
/// 簡化的 Pull Request DTO (不包含 Work Item 資訊)
/// </summary>
public class SimplifiedPullRequestDto
{
    /// <summary>
    /// 平台名稱 (GitLab, BitBucket)
    /// </summary>
    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    /// <summary>
    /// Pull Request 標題
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// 來源分支
    /// </summary>
    [JsonPropertyName("sourceBranch")]
    public required string SourceBranch { get; init; }

    /// <summary>
    /// 目標分支
    /// </summary>
    [JsonPropertyName("targetBranch")]
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 合併時間
    /// </summary>
    [JsonPropertyName("mergedAt")]
    public required DateTime? MergedAt { get; init; }

    /// <summary>
    /// 作者在版控平台的使用者 ID (GitLab: 數字 ID, BitBucket: UUID)
    /// </summary>
    [JsonPropertyName("authorUserId")]
    public required string? AuthorUserId { get; init; }

    /// <summary>
    /// 作者顯示名稱
    /// </summary>
    [JsonPropertyName("authorDisplayName")]
    public required string? AuthorDisplayName { get; init; }

    /// <summary>
    /// Pull Request URL
    /// </summary>
    [JsonPropertyName("url")]
    public required string? Url { get; init; }

    /// <summary>
    /// Repository 名稱
    /// </summary>
    [JsonPropertyName("repositoryName")]
    public required string RepositoryName { get; init; }
}
