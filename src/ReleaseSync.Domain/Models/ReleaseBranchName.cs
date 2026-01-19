using System.Globalization;
using System.Text.RegularExpressions;

namespace ReleaseSync.Domain.Models;

/// <summary>
/// Release Branch 名稱值物件
/// 格式: release/yyyyMMdd
/// </summary>
public sealed record ReleaseBranchName
{
    private static readonly Regex BranchNamePattern = new(@"^release/(\d{8})$", RegexOptions.Compiled);

    /// <summary>
    /// Release Branch 名稱
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Release 日期
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// 建立 ReleaseBranchName 實例
    /// </summary>
    /// <param name="value">Release Branch 名稱</param>
    /// <exception cref="ArgumentException">格式不符合 release/yyyyMMdd</exception>
    public ReleaseBranchName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Release branch name cannot be empty.", nameof(value));

        var match = BranchNamePattern.Match(value);
        if (!match.Success)
            throw new ArgumentException(
                $"Invalid release branch name format: '{value}'. Expected format: release/yyyyMMdd",
                nameof(value));

        var dateString = match.Groups[1].Value;
        if (!DateOnly.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new ArgumentException(
                $"Invalid date in release branch name: '{value}'",
                nameof(value));

        Value = value;
        Date = date;
    }

    /// <summary>
    /// 比較此 Release Branch 是否比另一個版本更新
    /// </summary>
    /// <param name="other">要比較的另一個 Release Branch</param>
    /// <returns>若此版本較新回傳 true,否則回傳 false</returns>
    public bool IsNewerThan(ReleaseBranchName other)
    {
        return Date > other.Date;
    }

    /// <summary>
    /// 取得日期部分 (yyyyMMdd 格式)
    /// </summary>
    public string GetDateString() => Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    public override string ToString() => Value;

    /// <summary>
    /// 嘗試解析 Release Branch 名稱
    /// </summary>
    /// <param name="value">要解析的字串</param>
    /// <param name="result">解析結果</param>
    /// <returns>解析成功回傳 true,否則回傳 false</returns>
    public static bool TryParse(string value, out ReleaseBranchName? result)
    {
        try
        {
            result = new ReleaseBranchName(value);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
