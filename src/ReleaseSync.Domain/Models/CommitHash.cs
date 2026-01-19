using System.Text.RegularExpressions;

namespace ReleaseSync.Domain.Models;

/// <summary>
/// Git Commit Hash 值物件
/// </summary>
/// <param name="Value">Commit Hash (40 字元完整 hash 或 7+ 字元短 hash)</param>
public partial record CommitHash(string Value)
{
    private static readonly Regex HexPattern = GenerateHexPattern();

    /// <summary>
    /// Commit Hash 值 (統一轉為小寫)
    /// </summary>
    public string Value { get; init; } = ValidateAndNormalize(Value);

    /// <summary>
    /// 取得短 Hash (前 7 個字元)
    /// </summary>
    public string ShortHash => Value.Length >= 7 ? Value[..7] : Value;

    /// <summary>
    /// 驗證並正規化 Commit Hash
    /// </summary>
    private static string ValidateAndNormalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Commit Hash 不能為空白");
        }

        // 統一轉為小寫
        string normalized = value.Trim().ToLowerInvariant();

        // 驗證長度 (完整 hash 為 40 字元,短 hash 通常為 7-40 字元)
        if (normalized.Length < 7 || normalized.Length > 40)
        {
            throw new ArgumentException(
                $"無效的 Commit Hash 長度: {normalized.Length}。" +
                "Commit Hash 長度應在 7-40 字元之間"
            );
        }

        // 驗證是否為十六進位字元
        if (!HexPattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                $"無效的 Commit Hash 格式: {value}。" +
                "Commit Hash 必須僅包含十六進位字元 (0-9, a-f)"
            );
        }

        return normalized;
    }

    /// <summary>
    /// 產生十六進位字元的正則表達式 Pattern
    /// </summary>
    [GeneratedRegex("^[0-9a-f]+$", RegexOptions.Compiled)]
    private static partial Regex GenerateHexPattern();

    public override string ToString() => Value;
}
