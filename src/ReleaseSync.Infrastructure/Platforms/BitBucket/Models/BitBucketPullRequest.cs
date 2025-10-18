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
    /// Account ID
    /// </summary>
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; }
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
