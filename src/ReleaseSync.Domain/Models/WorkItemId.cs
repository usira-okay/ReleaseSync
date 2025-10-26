namespace ReleaseSync.Domain.Models;

/// <summary>
/// Work Item 識別碼值物件
/// </summary>
/// <param name="Value">Work Item ID (正整數或 0 作為佔位符)</param>
public record WorkItemId(int Value)
{
    /// <summary>
    /// Work Item ID (正整數或 0 作為佔位符)
    /// </summary>
    public int Value { get; init; } = Value;

    /// <summary>
    /// 是否為佔位符 (Work Item ID 為 0)
    /// </summary>
    public bool IsPlaceholder => Value == 0;

    /// <summary>
    /// 驗證 Work Item ID 是否有效
    /// 允許 0 作為佔位符,用於沒有關聯 Work Item 的情況
    /// </summary>
    public void Validate()
    {
        // 允許 0 作為佔位符,不再限制 Value <= 0
        if (Value < 0)
        {
            throw new ArgumentException($"Work Item ID 不可為負數: {Value}");
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
    /// 允許 0 作為佔位符
    /// </summary>
    public static bool TryParse(string value, out WorkItemId? workItemId)
    {
        workItemId = null;
        if (!int.TryParse(value, out int id) || id < 0)
        {
            return false;
        }

        workItemId = new WorkItemId(id);
        return true;
    }

    public override string ToString() => Value.ToString();
}
