namespace ReleaseSync.Domain.Models;

/// <summary>
/// 平台同步狀態值物件
/// </summary>
public record PlatformSyncStatus
{
    /// <summary>
    /// 平台名稱 (例如: GitLab, BitBucket, AzureDevOps)
    /// </summary>
    public required string PlatformName { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 成功抓取的 PR/MR 數量
    /// </summary>
    public int PullRequestCount { get; init; }

    /// <summary>
    /// 錯誤訊息 (若失敗)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 執行時間 (毫秒)
    /// </summary>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>
    /// 建立成功狀態
    /// </summary>
    public static PlatformSyncStatus Success(string platformName, int count, long elapsedMs)
    {
        return new PlatformSyncStatus
        {
            PlatformName = platformName,
            IsSuccess = true,
            PullRequestCount = count,
            ElapsedMilliseconds = elapsedMs
        };
    }

    /// <summary>
    /// 建立失敗狀態
    /// </summary>
    public static PlatformSyncStatus Failure(string platformName, string errorMessage, long elapsedMs)
    {
        return new PlatformSyncStatus
        {
            PlatformName = platformName,
            IsSuccess = false,
            PullRequestCount = 0,
            ErrorMessage = errorMessage,
            ElapsedMilliseconds = elapsedMs
        };
    }
}
