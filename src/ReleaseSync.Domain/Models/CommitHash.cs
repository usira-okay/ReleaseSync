using System.Text.RegularExpressions;

namespace ReleaseSync.Domain.Models;

/// <summary>
/// Git Commit Hash 值物件
/// 支援完整 hash (40 字元) 與短 hash (7 字元)
/// </summary>
public sealed record CommitHash
{
    private static readonly Regex FullHashPattern = new(@"^[0-9a-fA-F]{40}$", RegexOptions.Compiled);
    private static readonly Regex ShortHashPattern = new(@"^[0-9a-fA-F]{7,40}$", RegexOptions.Compiled);

    /// <summary>
    /// 完整的 Commit Hash (小寫)
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 短 Hash (前 7 字元)
    /// </summary>
    public string ShortHash => Value.Length >= 7 ? Value[..7] : Value;

    /// <summary>
    /// 建立 CommitHash 實例
    /// </summary>
    /// <param name="value">Commit Hash 值</param>
    /// <exception cref="ArgumentException">Hash 格式無效</exception>
    public CommitHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Commit hash cannot be empty.", nameof(value));

        if (!ShortHashPattern.IsMatch(value))
            throw new ArgumentException(
                $"Invalid commit hash format: '{value}'. Expected 7-40 hexadecimal characters.",
                nameof(value));

        // 統一轉換為小寫
        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// 檢查是否為完整 hash (40 字元)
    /// </summary>
    public bool IsFullHash() => Value.Length == 40;

    public override string ToString() => Value;

    /// <summary>
    /// 嘗試解析 Commit Hash
    /// </summary>
    /// <param name="value">要解析的字串</param>
    /// <param name="result">解析結果</param>
    /// <returns>解析成功回傳 true,否則回傳 false</returns>
    public static bool TryParse(string value, out CommitHash? result)
    {
        try
        {
            result = new CommitHash(value);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
