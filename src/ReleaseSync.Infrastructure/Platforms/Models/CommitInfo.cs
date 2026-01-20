namespace ReleaseSync.Infrastructure.Platforms.Models;

/// <summary>
/// Commit 資訊
/// </summary>
public record CommitInfo
{
    /// <summary>
    /// Commit SHA
    /// </summary>
    public required string Sha { get; init; }

    /// <summary>
    /// 簡短 SHA
    /// </summary>
    public string ShortSha => Sha.Length >= 8 ? Sha[..8] : Sha;

    /// <summary>
    /// Commit 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 作者名稱
    /// </summary>
    public string? AuthorName { get; init; }

    /// <summary>
    /// Commit 日期
    /// </summary>
    public DateTimeOffset? Date { get; init; }
}
