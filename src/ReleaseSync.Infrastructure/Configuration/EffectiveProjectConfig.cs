using ReleaseSync.Domain.Models;

namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// 解析後的有效專案配置（合併全域與專案層級設定）
/// </summary>
public record EffectiveProjectConfig
{
    /// <summary>
    /// 專案識別碼
    /// </summary>
    public required string ProjectIdentifier { get; init; }

    /// <summary>
    /// 目標分支
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 抓取模式
    /// </summary>
    public required FetchMode FetchMode { get; init; }

    /// <summary>
    /// Release Branch（僅 FetchMode = ReleaseBranch 時有效）
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 開始日期（僅 FetchMode = DateRange 時有效）
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 結束日期（僅 FetchMode = DateRange 時有效）
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 從專案設定與全域設定解析有效配置
    /// </summary>
    /// <typeparam name="TProjectSettings">專案設定類型</typeparam>
    /// <param name="project">專案設定</param>
    /// <param name="global">全域同步選項設定</param>
    /// <param name="getIdentifier">取得專案識別碼的委派</param>
    /// <param name="getTargetBranch">取得目標分支的委派</param>
    /// <param name="getFetchMode">取得 FetchMode（可為 null）的委派</param>
    /// <param name="getReleaseBranch">取得 ReleaseBranch（可為 null）的委派</param>
    /// <param name="getStartDate">取得 StartDate（可為 null）的委派</param>
    /// <param name="getEndDate">取得 EndDate（可為 null）的委派</param>
    /// <returns>解析後的有效專案配置</returns>
    public static EffectiveProjectConfig Resolve<TProjectSettings>(
        TProjectSettings project,
        SyncOptionsSettings global,
        Func<TProjectSettings, string> getIdentifier,
        Func<TProjectSettings, string> getTargetBranch,
        Func<TProjectSettings, FetchMode?> getFetchMode,
        Func<TProjectSettings, string?> getReleaseBranch,
        Func<TProjectSettings, DateTime?> getStartDate,
        Func<TProjectSettings, DateTime?> getEndDate)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(global);

        // 專案層級優先，若未設定則使用全域設定
        var fetchMode = getFetchMode(project) ?? global.DefaultFetchMode;
        var releaseBranch = getReleaseBranch(project) ?? global.ReleaseBranch;
        var startDate = getStartDate(project) ?? global.StartDate;
        var endDate = getEndDate(project) ?? global.EndDate;

        return new EffectiveProjectConfig
        {
            ProjectIdentifier = getIdentifier(project),
            TargetBranch = getTargetBranch(project),
            FetchMode = fetchMode,
            ReleaseBranch = releaseBranch,
            StartDate = startDate,
            EndDate = endDate
        };
    }
}
