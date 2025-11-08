namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// Azure DevOps 組態
/// </summary>
public class AzureDevOpsSettings
{
    /// <summary>
    /// Personal Access Token (從 User Secrets 或 appsettings.json 讀取)
    /// </summary>
    public required string PersonalAccessToken { get; init; }

    /// <summary>
    /// Azure DevOps 組織 URL
    /// </summary>
    public required string OrganizationUrl { get; init; }

    /// <summary>
    /// Azure DevOps 專案名稱 (選填,目前未使用)
    /// </summary>
    public string? ProjectName { get; init; }

    /// <summary>
    /// Work Item ID 解析 Regex 模式清單
    /// </summary>
    public List<WorkItemIdPattern> WorkItemIdPatterns { get; init; } = new();

    /// <summary>
    /// 解析行為設定
    /// </summary>
    public ParsingBehaviorSettings ParsingBehavior { get; init; } = new();

    /// <summary>
    /// 團隊對應設定 (用於過濾 Work Item)
    /// </summary>
    public List<TeamMapping> TeamMapping { get; init; } = new();
}

/// <summary>
/// Work Item ID 解析 Regex 模式
/// </summary>
public class WorkItemIdPattern
{
    /// <summary>
    /// 模式名稱 (用於日誌識別)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 正規表示式 (需包含 capture group 擷取數字)
    /// </summary>
    public required string Regex { get; init; }

    /// <summary>
    /// 是否忽略大小寫
    /// </summary>
    public bool IgnoreCase { get; init; } = true;

    /// <summary>
    /// 擷取 Work Item ID 的 capture group 索引
    /// </summary>
    public int CaptureGroup { get; init; } = 1;
}

/// <summary>
/// 解析行為設定
/// </summary>
public class ParsingBehaviorSettings
{
    /// <summary>
    /// 無法解析 Work Item ID 時的行為
    /// </summary>
    public string OnParseFailure { get; init; } = "LogWarningAndContinue";

    /// <summary>
    /// 是否在第一個匹配的 pattern 後停止
    /// </summary>
    public bool StopOnFirstMatch { get; init; } = true;
}
