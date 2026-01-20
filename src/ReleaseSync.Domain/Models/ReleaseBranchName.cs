using System.Text.RegularExpressions;

namespace ReleaseSync.Domain.Models;

/// <summary>
/// 代表 Release Branch 名稱的值物件
/// 封裝命名格式驗證與日期解析邏輯
/// </summary>
public sealed partial record ReleaseBranchName : IComparable<ReleaseBranchName>
{
    /// <summary>
    /// Release Branch 名稱格式的正則表達式
    /// 格式: release/yyyyMMdd
    /// </summary>
    [GeneratedRegex(@"^release/(\d{8})$", RegexOptions.Compiled)]
    private static partial Regex ReleaseBranchPattern();

    /// <summary>
    /// Release Branch 的完整名稱
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 從 Release Branch 名稱解析出的日期
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// 取得短名稱（移除 refs/heads/ 前綴）
    /// </summary>
    public string ShortName => Value.StartsWith("refs/heads/")
        ? Value["refs/heads/".Length..]
        : Value;

    /// <summary>
    /// 私有建構函式，強制使用工廠方法建立實例
    /// </summary>
    /// <param name="value">Release Branch 名稱</param>
    /// <param name="date">解析出的日期</param>
    private ReleaseBranchName(string value, DateOnly date)
    {
        Value = value;
        Date = date;
    }

    /// <summary>
    /// 從字串解析 ReleaseBranchName
    /// </summary>
    /// <param name="value">Release Branch 名稱字串</param>
    /// <returns>ReleaseBranchName 實例</returns>
    /// <exception cref="ArgumentException">當 value 為 null 或空白時擲出</exception>
    /// <exception cref="FormatException">當格式不符合 release/yyyyMMdd 或日期無效時擲出</exception>
    public static ReleaseBranchName Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Release branch name cannot be null or empty", nameof(value));
        }

        var match = ReleaseBranchPattern().Match(value);
        if (!match.Success)
        {
            throw new FormatException(
                $"Invalid release branch format. Expected 'release/yyyyMMdd', got '{value}'");
        }

        var dateString = match.Groups[1].Value;
        if (!TryParseDate(dateString, out var date))
        {
            throw new FormatException(
                $"Invalid date in release branch name. '{dateString}' is not a valid date");
        }

        return new ReleaseBranchName(value, date);
    }

    /// <summary>
    /// 嘗試從字串解析 ReleaseBranchName
    /// </summary>
    /// <param name="value">Release Branch 名稱字串</param>
    /// <param name="result">解析成功時的 ReleaseBranchName 實例，失敗時為 null</param>
    /// <returns>解析是否成功</returns>
    public static bool TryParse(string? value, out ReleaseBranchName? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = ReleaseBranchPattern().Match(value);
        if (!match.Success)
        {
            return false;
        }

        var dateString = match.Groups[1].Value;
        if (!TryParseDate(dateString, out var date))
        {
            return false;
        }

        result = new ReleaseBranchName(value, date);
        return true;
    }

    /// <summary>
    /// 從日期建立 ReleaseBranchName
    /// </summary>
    /// <param name="date">日期</param>
    /// <returns>ReleaseBranchName 實例</returns>
    public static ReleaseBranchName FromDate(DateOnly date)
    {
        var value = $"release/{date:yyyyMMdd}";
        return new ReleaseBranchName(value, date);
    }

    /// <summary>
    /// 比較兩個 ReleaseBranchName 的日期順序
    /// </summary>
    /// <param name="other">要比較的另一個 ReleaseBranchName</param>
    /// <returns>比較結果：負數表示小於，0 表示相等，正數表示大於</returns>
    public int CompareTo(ReleaseBranchName? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Date.CompareTo(other.Date);
    }

    /// <summary>
    /// 小於運算子
    /// </summary>
    public static bool operator <(ReleaseBranchName left, ReleaseBranchName right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// 大於運算子
    /// </summary>
    public static bool operator >(ReleaseBranchName left, ReleaseBranchName right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// 小於等於運算子
    /// </summary>
    public static bool operator <=(ReleaseBranchName left, ReleaseBranchName right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// 大於等於運算子
    /// </summary>
    public static bool operator >=(ReleaseBranchName left, ReleaseBranchName right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// 隱式轉換為字串
    /// </summary>
    /// <param name="branchName">ReleaseBranchName 實例</param>
    public static implicit operator string(ReleaseBranchName branchName) => branchName.Value;

    /// <summary>
    /// 嘗試將 yyyyMMdd 格式的字串解析為 DateOnly
    /// </summary>
    /// <param name="dateString">日期字串</param>
    /// <param name="date">解析結果</param>
    /// <returns>解析是否成功</returns>
    private static bool TryParseDate(string dateString, out DateOnly date)
    {
        date = default;

        if (dateString.Length != 8)
        {
            return false;
        }

        if (!int.TryParse(dateString[..4], out var year) ||
            !int.TryParse(dateString[4..6], out var month) ||
            !int.TryParse(dateString[6..8], out var day))
        {
            return false;
        }

        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 取得字串表示
    /// </summary>
    public override string ToString() => Value;
}
