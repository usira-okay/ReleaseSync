namespace ReleaseSync.Domain.Models;

/// <summary>
/// 時間範圍值物件
/// </summary>
/// <param name="StartDate">起始日期 (包含)</param>
/// <param name="EndDate">結束日期 (包含)</param>
public record DateRange(DateTime StartDate, DateTime EndDate)
{
    /// <summary>
    /// 起始日期 (包含)
    /// </summary>
    public DateTime StartDate { get; init; } = StartDate;

    /// <summary>
    /// 結束日期 (包含)
    /// </summary>
    public DateTime EndDate { get; init; } = EndDate;

    /// <summary>
    /// 驗證時間範圍是否有效
    /// </summary>
    public void Validate()
    {
        if (StartDate > EndDate)
        {
            throw new ArgumentException(
                $"起始日期 ({StartDate:yyyy-MM-dd}) 不能晚於結束日期 ({EndDate:yyyy-MM-dd})"
            );
        }
    }

    /// <summary>
    /// 檢查指定日期是否在此範圍內
    /// </summary>
    public bool Contains(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    /// <summary>
    /// 建立「最近 N 天」的時間範圍
    /// </summary>
    public static DateRange LastDays(int days)
    {
        DateTime endDate = DateTime.UtcNow;
        DateTime startDate = endDate.AddDays(-days);
        return new DateRange(startDate, endDate);
    }
}
