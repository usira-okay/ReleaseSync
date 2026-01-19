using System.Globalization;
using System.Text.RegularExpressions;

namespace ReleaseSync.Domain.Models;

/// <summary>
/// Release Branch 名稱值物件
/// </summary>
/// <param name="Value">Branch 名稱 (格式: release/yyyyMMdd)</param>
public partial record ReleaseBranchName(string Value)
{
    private static readonly Regex ReleaseBranchPattern = GenerateReleaseBranchPattern();

    /// <summary>
    /// Branch 名稱
    /// </summary>
    public string Value { get; init; } = ValidateFormat(Value);

    /// <summary>
    /// 取得 Release 日期
    /// </summary>
    public DateTime GetDate()
    {
        var match = ReleaseBranchPattern.Match(Value);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"無法從分支名稱解析日期: {Value}"
            );
        }

        string dateString = match.Groups[1].Value;
        return DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 比較此 Release Branch 是否比另一個版本更新
    /// </summary>
    /// <param name="other">要比較的另一個 Release Branch</param>
    /// <returns>如果此版本較新則返回 true</returns>
    public bool IsNewerThan(ReleaseBranchName other)
    {
        return GetDate() > other.GetDate();
    }

    /// <summary>
    /// 驗證 Release Branch 名稱格式
    /// </summary>
    private static string ValidateFormat(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Release Branch 名稱不能為空白");
        }

        if (!ReleaseBranchPattern.IsMatch(value))
        {
            throw new ArgumentException(
                $"無效的 Release Branch 名稱格式: {value}。" +
                "正確格式應為 release/yyyyMMdd (例如: release/20260120)"
            );
        }

        return value;
    }

    /// <summary>
    /// 產生 Release Branch 名稱的正則表達式 Pattern
    /// </summary>
    [GeneratedRegex(@"^release/(\d{8})$", RegexOptions.Compiled)]
    private static partial Regex GenerateReleaseBranchPattern();

    public override string ToString() => Value;
}
