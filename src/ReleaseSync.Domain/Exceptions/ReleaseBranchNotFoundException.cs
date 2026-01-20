namespace ReleaseSync.Domain.Exceptions;

/// <summary>
/// 當找不到指定的 Release Branch 時拋出的例外
/// </summary>
public class ReleaseBranchNotFoundException : Exception
{
    /// <summary>
    /// 找不到的 Release Branch 名稱
    /// </summary>
    public string BranchName { get; }

    /// <summary>
    /// 專案/Repository 名稱
    /// </summary>
    public string RepositoryName { get; }

    /// <summary>
    /// 可用的 Release Branch 清單
    /// </summary>
    public IReadOnlyList<string> AvailableBranches { get; }

    /// <summary>
    /// 建立 ReleaseBranchNotFoundException 實例
    /// </summary>
    /// <param name="branchName">找不到的 Release Branch 名稱</param>
    /// <param name="repositoryName">專案/Repository 名稱</param>
    /// <param name="availableBranches">可用的 Release Branch 清單</param>
    public ReleaseBranchNotFoundException(
        string branchName,
        string repositoryName,
        IEnumerable<string>? availableBranches = null)
        : base(BuildMessage(branchName, repositoryName, availableBranches))
    {
        BranchName = branchName;
        RepositoryName = repositoryName;
        AvailableBranches = availableBranches?.ToList().AsReadOnly()
            ?? Array.Empty<string>().AsReadOnly();
    }

    /// <summary>
    /// 建立 ReleaseBranchNotFoundException 實例（含內部例外）
    /// </summary>
    public ReleaseBranchNotFoundException(
        string branchName,
        string repositoryName,
        Exception innerException)
        : base(BuildMessage(branchName, repositoryName, null), innerException)
    {
        BranchName = branchName;
        RepositoryName = repositoryName;
        AvailableBranches = Array.Empty<string>().AsReadOnly();
    }

    private static string BuildMessage(
        string branchName,
        string repositoryName,
        IEnumerable<string>? availableBranches)
    {
        var message = $"Release branch '{branchName}' not found in repository '{repositoryName}'.";

        var branches = availableBranches?.ToList();
        if (branches != null && branches.Count > 0)
        {
            message += $" Available release branches: {string.Join(", ", branches)}";
        }

        return message;
    }
}
