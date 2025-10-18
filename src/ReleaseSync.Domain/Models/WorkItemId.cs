namespace ReleaseSync.Domain.Models;

/// <summary>
/// Work Item 識別碼值物件
/// </summary>
/// <param name="Value">Work Item ID (正整數)</param>
public record WorkItemId(int Value)
{
    /// <summary>
    /// Work Item ID (正整數)
    /// </summary>
    public int Value { get; init; } = Value;

    /// <summary>
    /// 驗證 Work Item ID 是否有效
    /// </summary>
    public void Validate()
    {
        if (Value <= 0)
        {
            throw new ArgumentException($"Work Item ID 必須為正整數: {Value}");
        }
    }

    /// <summary>
    /// 從字串解析 Work Item ID
    /// </summary>
    public static WorkItemId Parse(string value)
    {
        if (!int.TryParse(value, out int id))
        {
            throw new FormatException($"無法將 '{value}' 解析為 Work Item ID");
        }

        return new WorkItemId(id);
    }

    /// <summary>
    /// 嘗試從字串解析 Work Item ID
    /// </summary>
    public static bool TryParse(string value, out WorkItemId? workItemId)
    {
        workItemId = null;
        if (!int.TryParse(value, out int id) || id <= 0)
        {
            return false;
        }

        workItemId = new WorkItemId(id);
        return true;
    }

    public override string ToString() => Value.ToString();
}
