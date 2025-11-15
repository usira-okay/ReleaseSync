using System.Text.Json.Serialization;

namespace ReleaseSync.Application.DTOs;

/// <summary>
/// Repository 為中心的輸出格式 DTO (頂層)
/// 用於將同步結果以 Repository 為主體輸出,PR/MR 作為子項目分組
/// </summary>
public record RepositoryBasedOutputDto
{
    /// <summary>
    /// 查詢開始日期 (UTC)
    /// </summary>
    [JsonPropertyName("startDate")]
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// 查詢結束日期 (UTC)
    /// </summary>
    [JsonPropertyName("endDate")]
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Repository 分組清單,每個 Repository 包含其關聯的 Pull Requests
    /// </summary>
    [JsonPropertyName("repositories")]
    public required List<RepositoryGroupDto> Repositories { get; init; }

    /// <summary>
    /// 從 SyncResultDto 轉換為 Repository-based 格式
    /// </summary>
    /// <param name="syncResult">同步結果 DTO</param>
    /// <returns>Repository 為中心的輸出 DTO</returns>
    public static RepositoryBasedOutputDto FromSyncResult(SyncResultDto syncResult)
    {
        // 將 Pull Requests 按 (RepositoryName, Platform) 分組
        var repositoryGroups = syncResult.PullRequests
            .GroupBy(pr => new { pr.RepositoryName, pr.Platform })
            .Select(group => new RepositoryGroupDto
            {
                RepositoryName = ExtractRepositoryName(group.Key.RepositoryName),
                Platform = group.Key.Platform,
                PullRequests = group.Select(pr => new RepositoryPullRequestDto
                {
                    WorkItem = pr.AssociatedWorkItem != null
                        ? PullRequestWorkItemDto.FromWorkItemDto(pr.AssociatedWorkItem)
                        : null,
                    PullRequestTitle = pr.Title,
                    SourceBranch = pr.SourceBranch,
                    TargetBranch = pr.TargetBranch,
                    MergedAt = pr.MergedAt,
                    AuthorUserId = pr.AuthorUserId,
                    AuthorDisplayName = pr.AuthorDisplayName,
                    PullRequestUrl = pr.Url
                }).ToList()
            })
            .ToList();

        return new RepositoryBasedOutputDto
        {
            StartDate = syncResult.StartDate,
            EndDate = syncResult.EndDate,
            Repositories = repositoryGroups
        };
    }

    /// <summary>
    /// 從完整的 Repository 路徑中提取 Repository 名稱
    /// </summary>
    /// <param name="repositoryName">完整 Repository 路徑</param>
    /// <returns>提取的 Repository 名稱</returns>
    /// <remarks>
    /// 提取規則:
    /// - "owner/repo" → "repo"
    /// - "org/team/project" → "project"
    /// - "standalone" → "standalone"
    /// - "" → ""
    /// </remarks>
    private static string ExtractRepositoryName(string repositoryName)
    {
        // 使用 '/' 分割並取最後部分
        var parts = repositoryName.Split('/');
        return parts[^1]; // C# 9.0 Index from End 語法
    }
}

/// <summary>
/// Repository 分組 DTO,代表單一 Repository 及其關聯的所有 Pull Requests
/// </summary>
public record RepositoryGroupDto
{
    /// <summary>
    /// Repository 簡短名稱 (已從完整路徑提取)
    /// </summary>
    [JsonPropertyName("repositoryName")]
    public required string RepositoryName { get; init; }

    /// <summary>
    /// 平台名稱 (GitLab, BitBucket, AzureDevOps)
    /// </summary>
    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    /// <summary>
    /// 此 Repository 的 Pull Requests 清單
    /// </summary>
    [JsonPropertyName("pullRequests")]
    public required List<RepositoryPullRequestDto> PullRequests { get; init; }
}

/// <summary>
/// 簡化的 Pull Request DTO,包含 Work Item 關聯,用於 Repository 分組輸出
/// </summary>
public record RepositoryPullRequestDto
{
    /// <summary>
    /// 關聯的 Work Item (可為 null)
    /// </summary>
    [JsonPropertyName("workItem")]
    public PullRequestWorkItemDto? WorkItem { get; init; }

    /// <summary>
    /// Pull Request 標題
    /// </summary>
    [JsonPropertyName("pullRequestTitle")]
    public required string PullRequestTitle { get; init; }

    /// <summary>
    /// 來源分支名稱
    /// </summary>
    [JsonPropertyName("sourceBranch")]
    public required string SourceBranch { get; init; }

    /// <summary>
    /// 目標分支名稱
    /// </summary>
    [JsonPropertyName("targetBranch")]
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 合併時間 (UTC)
    /// </summary>
    [JsonPropertyName("mergedAt")]
    public DateTime? MergedAt { get; init; }

    /// <summary>
    /// 作者在版控平台的使用者 ID
    /// </summary>
    [JsonPropertyName("authorUserId")]
    public string? AuthorUserId { get; init; }

    /// <summary>
    /// 作者顯示名稱
    /// </summary>
    [JsonPropertyName("authorDisplayName")]
    public string? AuthorDisplayName { get; init; }

    /// <summary>
    /// Pull Request URL
    /// </summary>
    [JsonPropertyName("pullRequestUrl")]
    public string? PullRequestUrl { get; init; }
}

/// <summary>
/// Work Item 基本資訊,用於 PR 關聯 (簡化版本,不含 Parent 層級結構)
/// </summary>
public record PullRequestWorkItemDto
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
    /// Work Item 所屬團隊 (已經過 TeamMapping 轉換)
    /// </summary>
    [JsonPropertyName("workItemTeam")]
    public string? WorkItemTeam { get; init; }

    /// <summary>
    /// Work Item 類型 (Task, Bug, User Story, Feature 等)
    /// </summary>
    [JsonPropertyName("workItemType")]
    public required string WorkItemType { get; init; }

    /// <summary>
    /// Work Item URL
    /// </summary>
    [JsonPropertyName("workItemUrl")]
    public string? WorkItemUrl { get; init; }

    /// <summary>
    /// 從 WorkItemDto 轉換為簡化的 Work Item DTO
    /// </summary>
    /// <param name="workItem">完整 Work Item DTO</param>
    /// <returns>簡化的 Work Item DTO</returns>
    public static PullRequestWorkItemDto FromWorkItemDto(WorkItemDto workItem)
    {
        return new PullRequestWorkItemDto
        {
            WorkItemId = workItem.Id,
            WorkItemTitle = workItem.Title,
            WorkItemTeam = workItem.Team,
            WorkItemType = workItem.Type,
            WorkItemUrl = workItem.Url
        };
    }
}
