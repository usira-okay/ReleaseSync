namespace ReleaseSync.Infrastructure.Platforms.BitBucket.Models;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// BitBucket Pull Request API 回應模型
/// </summary>
public class BitBucketPullRequest
{
    /// <summary>
    /// Pull Request ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Pull Request 標題
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// Pull Request 描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// Pull Request 狀態 (OPEN, MERGED, DECLINED, SUPERSEDED)
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// 最後更新時間
    /// </summary>
    [JsonPropertyName("updated_on")]
    public DateTime UpdatedOn { get; set; }

    /// <summary>
    /// 關閉/合併時間 (PR 被 Merged 或 Declined 時的時間)
    /// </summary>
    [JsonPropertyName("closed_on")]
    public DateTime? ClosedOn { get; set; }

    /// <summary>
    /// 來源分支資訊
    /// </summary>
    [JsonPropertyName("source")]
    public BitBucketBranch Source { get; set; }

    /// <summary>
    /// 目標分支資訊
    /// </summary>
    [JsonPropertyName("destination")]
    public BitBucketBranch Destination { get; set; }

    /// <summary>
    /// 作者資訊
    /// </summary>
    [JsonPropertyName("author")]
    public BitBucketUser Author { get; set; }

    /// <summary>
    /// Pull Request 連結資訊
    /// </summary>
    [JsonPropertyName("links")]
    public BitBucketLinks Links { get; set; }

    /// <summary>
    /// Merge Commit (若已合併)
    /// </summary>
    [JsonPropertyName("merge_commit")]
    public BitBucketCommit MergeCommit { get; set; }
}

/// <summary>
/// BitBucket 分支資訊
/// </summary>
public class BitBucketBranch
{
    /// <summary>
    /// 分支詳細資訊
    /// </summary>
    [JsonPropertyName("branch")]
    public BitBucketBranchDetail Branch { get; set; }
}

/// <summary>
/// BitBucket 分支詳細資訊
/// </summary>
public class BitBucketBranchDetail
{
    /// <summary>
    /// 分支名稱
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

/// <summary>
/// BitBucket 使用者資訊
/// </summary>
public class BitBucketUser
{
    /// <summary>
    /// 使用者名稱
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; }

    /// <summary>
    /// 顯示名稱
    /// </summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }

    /// <summary>
    /// uuid
    /// </summary>
    [JsonPropertyName("uuid")]
    public string UuId { get; set; }
}

/// <summary>
/// BitBucket 連結資訊
/// </summary>
public class BitBucketLinks
{
    /// <summary>
    /// HTML 連結
    /// </summary>
    [JsonPropertyName("html")]
    public BitBucketHref Html { get; set; }
}

/// <summary>
/// BitBucket Href 資訊
/// </summary>
public class BitBucketHref
{
    /// <summary>
    /// URL
    /// </summary>
    [JsonPropertyName("href")]
    public string Href { get; set; }
}

/// <summary>
/// BitBucket Commit 資訊
/// </summary>
public class BitBucketCommit
{
    /// <summary>
    /// Commit Hash
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
}

/// <summary>
/// BitBucket 分頁回應模型
/// </summary>
public class BitBucketPaginatedResponse<T>
{
    /// <summary>
    /// 資料清單
    /// </summary>
    [JsonPropertyName("values")]
    public List<T> Values { get; set; }

    /// <summary>
    /// 每頁資料筆數
    /// </summary>
    [JsonPropertyName("pagelen")]
    public int PageLen { get; set; }

    /// <summary>
    /// 總筆數
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// 當前頁碼
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// 下一頁 URL
    /// </summary>
    [JsonPropertyName("next")]
    public string Next { get; set; }
}

/// <summary>
/// BitBucket Refs/Branches API 回應模型
/// </summary>
public class BitBucketRef
{
    /// <summary>
    /// 分支名稱
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分支類型 (branch, tag)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 最新 Commit 資訊
    /// </summary>
    [JsonPropertyName("target")]
    public BitBucketRefTarget? Target { get; set; }
}

/// <summary>
/// BitBucket Ref Target (Commit) 資訊
/// </summary>
public class BitBucketRefTarget
{
    /// <summary>
    /// Commit Hash
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Commit 訊息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Commit 日期
    /// </summary>
    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// 作者資訊
    /// </summary>
    [JsonPropertyName("author")]
    public BitBucketCommitAuthor? Author { get; set; }
}

/// <summary>
/// BitBucket Commit 作者資訊
/// </summary>
public class BitBucketCommitAuthor
{
    /// <summary>
    /// 作者原始字串 (格式: "Name <email>")
    /// </summary>
    [JsonPropertyName("raw")]
    public string? Raw { get; set; }

    /// <summary>
    /// 使用者資訊
    /// </summary>
    [JsonPropertyName("user")]
    public BitBucketUser? User { get; set; }
}

/// <summary>
/// BitBucket Diff Stat 回應模型
/// </summary>
public class BitBucketDiffStat
{
    /// <summary>
    /// 差異狀態 (added, removed, modified, renamed)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 新增行數
    /// </summary>
    [JsonPropertyName("lines_added")]
    public int LinesAdded { get; set; }

    /// <summary>
    /// 刪除行數
    /// </summary>
    [JsonPropertyName("lines_removed")]
    public int LinesRemoved { get; set; }
}

/// <summary>
/// BitBucket Commits 列表回應中的 Commit 模型
/// </summary>
public class BitBucketCommitInfo
{
    /// <summary>
    /// Commit Hash
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Commit 訊息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Commit 日期
    /// </summary>
    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// 作者資訊
    /// </summary>
    [JsonPropertyName("author")]
    public BitBucketCommitAuthor? Author { get; set; }
}
