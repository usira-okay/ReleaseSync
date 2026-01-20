using ReleaseSync.Domain.Models;
using ReleaseSync.Infrastructure.Platforms.Models;

namespace ReleaseSync.Infrastructure.Services;

/// <summary>
/// Release Branch 版本解析器介面
/// </summary>
public interface IReleaseBranchVersionResolver
{
    /// <summary>
    /// 判斷指定的 Release Branch 是否為最新版本
    /// </summary>
    /// <param name="currentBranch">目前的 Release Branch</param>
    /// <param name="allBranches">所有分支清單</param>
    /// <returns>是否為最新版本</returns>
    bool IsLatestVersion(ReleaseBranchName currentBranch, IEnumerable<BranchInfo> allBranches);

    /// <summary>
    /// 取得下一個版本的 Release Branch
    /// </summary>
    /// <param name="currentBranch">目前的 Release Branch</param>
    /// <param name="allBranches">所有分支清單</param>
    /// <returns>下一個版本的 Release Branch，若為最新版本則返回 null</returns>
    ReleaseBranchName? GetNextVersion(ReleaseBranchName currentBranch, IEnumerable<BranchInfo> allBranches);

    /// <summary>
    /// 從所有分支中篩選出 Release Branch 並排序
    /// </summary>
    /// <param name="allBranches">所有分支清單</param>
    /// <returns>依日期排序的 Release Branch 清單（由舊到新）</returns>
    IEnumerable<ReleaseBranchName> GetAllReleaseBranches(IEnumerable<BranchInfo> allBranches);

    /// <summary>
    /// 取得最新的 Release Branch
    /// </summary>
    /// <param name="allBranches">所有分支清單</param>
    /// <returns>最新的 Release Branch，若無則返回 null</returns>
    ReleaseBranchName? GetLatestReleaseBranch(IEnumerable<BranchInfo> allBranches);
}

/// <summary>
/// Release Branch 版本解析器實作
/// </summary>
/// <remarks>
/// 負責解析 Release Branch 版本，判斷是否為最新版本，以及找出下一個版本。
/// Release Branch 命名格式: release/yyyyMMdd (例如: release/20260120)
/// </remarks>
public class ReleaseBranchVersionResolver : IReleaseBranchVersionResolver
{
    /// <summary>
    /// 判斷指定的 Release Branch 是否為最新版本
    /// </summary>
    public bool IsLatestVersion(ReleaseBranchName currentBranch, IEnumerable<BranchInfo> allBranches)
    {
        ArgumentNullException.ThrowIfNull(currentBranch, nameof(currentBranch));
        ArgumentNullException.ThrowIfNull(allBranches, nameof(allBranches));

        var releaseBranches = GetAllReleaseBranches(allBranches).ToList();

        if (releaseBranches.Count == 0)
        {
            // 若沒有其他 Release Branch，視為最新版本
            return true;
        }

        var latestBranch = releaseBranches.MaxBy(b => b.Date);
        return latestBranch != null && currentBranch.Date >= latestBranch.Date;
    }

    /// <summary>
    /// 取得下一個版本的 Release Branch
    /// </summary>
    public ReleaseBranchName? GetNextVersion(ReleaseBranchName currentBranch, IEnumerable<BranchInfo> allBranches)
    {
        ArgumentNullException.ThrowIfNull(currentBranch, nameof(currentBranch));
        ArgumentNullException.ThrowIfNull(allBranches, nameof(allBranches));

        var releaseBranches = GetAllReleaseBranches(allBranches)
            .OrderBy(b => b.Date)
            .ToList();

        if (releaseBranches.Count == 0)
        {
            return null;
        }

        // 找出比目前版本新的第一個版本
        var nextVersion = releaseBranches
            .Where(b => b.Date > currentBranch.Date)
            .OrderBy(b => b.Date)
            .FirstOrDefault();

        return nextVersion;
    }

    /// <summary>
    /// 從所有分支中篩選出 Release Branch 並排序
    /// </summary>
    public IEnumerable<ReleaseBranchName> GetAllReleaseBranches(IEnumerable<BranchInfo> allBranches)
    {
        ArgumentNullException.ThrowIfNull(allBranches, nameof(allBranches));

        var releaseBranches = new List<ReleaseBranchName>();

        foreach (var branch in allBranches)
        {
            if (ReleaseBranchName.TryParse(branch.Name, out var releaseBranch))
            {
                releaseBranches.Add(releaseBranch!);
            }
        }

        return releaseBranches.OrderBy(b => b.Date);
    }

    /// <summary>
    /// 取得最新的 Release Branch
    /// </summary>
    public ReleaseBranchName? GetLatestReleaseBranch(IEnumerable<BranchInfo> allBranches)
    {
        ArgumentNullException.ThrowIfNull(allBranches, nameof(allBranches));

        return GetAllReleaseBranches(allBranches)
            .MaxBy(b => b.Date);
    }
}
