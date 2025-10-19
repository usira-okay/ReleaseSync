namespace ReleaseSync.Domain.Models;

/// <summary>
/// 分支名稱值物件
/// </summary>
/// <param name="Value">分支名稱</param>
public record BranchName(string Value)
{
    /// <summary>
    /// 分支名稱
    /// </summary>
    public string Value { get; init; } = Value ?? throw new ArgumentNullException(nameof(Value));

    /// <summary>
    /// 驗證分支名稱是否有效
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            throw new ArgumentException("分支名稱不能為空白");
        }

        // Git 分支命名規則驗證 (簡化版)
        if (Value.Contains("..") || Value.StartsWith('/') || Value.EndsWith('/'))
        {
            throw new ArgumentException($"無效的分支名稱格式: {Value}");
        }
    }

    /// <summary>
    /// 取得短名稱 (移除 refs/heads/ 前綴)
    /// </summary>
    public string ShortName =>
        Value.StartsWith("refs/heads/")
            ? Value.Substring("refs/heads/".Length)
            : Value;

    public override string ToString() => Value;
}
