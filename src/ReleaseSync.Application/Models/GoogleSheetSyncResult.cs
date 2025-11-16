// <copyright file="GoogleSheetSyncResult.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Application.Models;

/// <summary>
/// Google Sheet 同步結果值物件。
/// 表示同步操作的完成摘要。
/// </summary>
public sealed record GoogleSheetSyncResult
{
    /// <summary>
    /// 是否成功完成同步。
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 更新的 row 數量。
    /// </summary>
    public int UpdatedRowCount { get; init; }

    /// <summary>
    /// 新增的 row 數量。
    /// </summary>
    public int InsertedRowCount { get; init; }

    /// <summary>
    /// 處理的 PR/MR 總數。
    /// </summary>
    public int ProcessedPullRequestCount { get; init; }

    /// <summary>
    /// Google Sheet URL (方便使用者直接開啟)。
    /// </summary>
    public string? SpreadsheetUrl { get; init; }

    /// <summary>
    /// 錯誤訊息 (若失敗時提供)。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 執行時間。
    /// </summary>
    public TimeSpan ExecutionDuration { get; init; }

    /// <summary>
    /// 建立成功的同步結果。
    /// </summary>
    /// <param name="updatedRowCount">更新的 row 數量。</param>
    /// <param name="insertedRowCount">新增的 row 數量。</param>
    /// <param name="processedPullRequestCount">處理的 PR/MR 數量。</param>
    /// <param name="spreadsheetUrl">Google Sheet URL。</param>
    /// <param name="executionDuration">執行時間。</param>
    /// <returns>成功的同步結果。</returns>
    public static GoogleSheetSyncResult Success(
        int updatedRowCount,
        int insertedRowCount,
        int processedPullRequestCount,
        string? spreadsheetUrl,
        TimeSpan executionDuration)
    {
        return new GoogleSheetSyncResult
        {
            IsSuccess = true,
            UpdatedRowCount = updatedRowCount,
            InsertedRowCount = insertedRowCount,
            ProcessedPullRequestCount = processedPullRequestCount,
            SpreadsheetUrl = spreadsheetUrl,
            ExecutionDuration = executionDuration,
        };
    }

    /// <summary>
    /// 建立失敗的同步結果。
    /// </summary>
    /// <param name="errorMessage">錯誤訊息。</param>
    /// <param name="executionDuration">執行時間。</param>
    /// <returns>失敗的同步結果。</returns>
    public static GoogleSheetSyncResult Failure(string errorMessage, TimeSpan executionDuration)
    {
        return new GoogleSheetSyncResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExecutionDuration = executionDuration,
        };
    }
}
