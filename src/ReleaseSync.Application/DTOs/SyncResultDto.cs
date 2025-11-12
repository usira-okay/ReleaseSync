using ReleaseSync.Domain.Models;

namespace ReleaseSync.Application.DTOs;

/// <summary>
/// 同步結果 DTO
/// </summary>
public class SyncResultDto
{
    /// <summary>
    /// 同步執行的開始時間 (UTC)
    /// </summary>
    public required DateTime SyncStartedAt { get; init; }

    /// <summary>
    /// 同步執行的結束時間 (UTC)
    /// </summary>
    public DateTime? SyncCompletedAt { get; init; }

    /// <summary>
    /// 起始日期 (包含)
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// 結束日期 (包含)
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// 是否完全成功 (所有平台皆成功)
    /// </summary>
    public required bool IsFullySuccessful { get; init; }

    /// <summary>
    /// 是否部分成功 (至少一個平台成功)
    /// </summary>
    public required bool IsPartiallySuccessful { get; init; }

    /// <summary>
    /// 總計抓取的 PR/MR 數量
    /// </summary>
    public required int TotalPullRequestCount { get; init; }

    /// <summary>
    /// 關聯到 Work Item 的 PR/MR 數量
    /// </summary>
    public required int LinkedWorkItemCount { get; init; }

    /// <summary>
    /// 所有抓取的 Pull Requests / Merge Requests
    /// </summary>
    public required List<PullRequestDto> PullRequests { get; init; }

    /// <summary>
    /// 各平台的同步狀態
    /// </summary>
    public required List<PlatformStatusDto> PlatformStatuses { get; init; }

    /// <summary>
    /// 從 Domain 模型轉換為 DTO
    /// </summary>
    public static SyncResultDto FromDomain(SyncResult syncResult)
    {
        return new SyncResultDto
        {
            SyncStartedAt = syncResult.SyncStartedAt,
            SyncCompletedAt = syncResult.SyncCompletedAt,
            StartDate = syncResult.SyncDateRange.StartDate,
            EndDate = syncResult.SyncDateRange.EndDate,
            IsFullySuccessful = syncResult.IsFullySuccessful,
            IsPartiallySuccessful = syncResult.IsPartiallySuccessful,
            TotalPullRequestCount = syncResult.TotalPullRequestCount,
            LinkedWorkItemCount = syncResult.LinkedWorkItemCount,
            PullRequests = syncResult.PullRequests.Select(PullRequestDto.FromDomain).ToList(),
            PlatformStatuses = syncResult.PlatformStatuses.Select(PlatformStatusDto.FromDomain).ToList()
        };
    }
}

/// <summary>
/// Pull Request DTO
/// </summary>
public class PullRequestDto
{
    /// <summary>
    /// 平台類型
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// PR/MR 編號
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
    public required string SourceBranch { get; init; }

    /// <summary>
    /// 目標分支名稱
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 合併時間 (UTC)
    /// </summary>
    public DateTime? MergedAt { get; init; }

    /// <summary>
    /// 狀態
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
    /// Repository 名稱
    /// </summary>
    public required string RepositoryName { get; init; }

    /// <summary>
    /// PR/MR 的 URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// 關聯的 Work Item
    /// </summary>
    public WorkItemDto? AssociatedWorkItem { get; set; }

    /// <summary>
    /// 從 Domain 模型轉換為 DTO
    /// </summary>
    public static PullRequestDto FromDomain(PullRequestInfo pr)
    {
        return new PullRequestDto
        {
            Platform = pr.Platform,
            Number = pr.Number,
            Title = pr.Title,
            Description = pr.Description,
            SourceBranch = pr.SourceBranch.Value,
            TargetBranch = pr.TargetBranch.Value,
            CreatedAt = pr.CreatedAt,
            MergedAt = pr.MergedAt,
            State = pr.State,
            AuthorUserId = pr.AuthorUserId,
            AuthorDisplayName = pr.AuthorDisplayName,
            RepositoryName = pr.RepositoryName,
            Url = pr.Url,
            AssociatedWorkItem = pr.AssociatedWorkItem != null
                ? WorkItemDto.FromDomain(pr.AssociatedWorkItem)
                : null
        };
    }
}

/// <summary>
/// Work Item DTO
/// </summary>
public class WorkItemDto
{
    /// <summary>
    /// Work Item ID
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Work Item 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Work Item 類型
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Work Item 狀態
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Work Item URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// 指派給誰
    /// </summary>
    public string? AssignedTo { get; init; }

    /// <summary>
    /// 所屬團隊 (已經過 TeamMapping 轉換)
    /// </summary>
    public string? Team { get; init; }

    /// <summary>
    /// Parent Work Item
    /// </summary>
    public WorkItemDto? ParentWorkItem { get; init; }

    /// <summary>
    /// 從 Domain 模型轉換為 DTO
    /// </summary>
    public static WorkItemDto FromDomain(WorkItemInfo workItem)
    {
        return new WorkItemDto
        {
            Id = workItem.Id.Value,
            Title = workItem.Title,
            Type = workItem.Type,
            State = workItem.State,
            Url = workItem.Url,
            AssignedTo = workItem.AssignedTo,
            Team = workItem.Team,
            ParentWorkItem = workItem.ParentWorkItem != null
                ? FromDomain(workItem.ParentWorkItem)
                : null
        };
    }
}

/// <summary>
/// 平台狀態 DTO
/// </summary>
public class PlatformStatusDto
{
    /// <summary>
    /// 平台名稱
    /// </summary>
    public required string PlatformName { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 成功抓取的 PR/MR 數量
    /// </summary>
    public required int PullRequestCount { get; init; }

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 執行時間 (毫秒)
    /// </summary>
    public required long ElapsedMilliseconds { get; init; }

    /// <summary>
    /// 從 Domain 模型轉換為 DTO
    /// </summary>
    public static PlatformStatusDto FromDomain(PlatformSyncStatus status)
    {
        return new PlatformStatusDto
        {
            PlatformName = status.PlatformName,
            IsSuccess = status.IsSuccess,
            PullRequestCount = status.PullRequestCount,
            ErrorMessage = status.ErrorMessage,
            ElapsedMilliseconds = status.ElapsedMilliseconds
        };
    }
}
