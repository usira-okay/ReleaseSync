// <copyright file="SheetSyncOperation.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Application.Models;

/// <summary>
/// Google Sheet 同步操作類型。
/// </summary>
public enum SheetOperationType
{
    /// <summary>
    /// 更新現有 row。
    /// </summary>
    Update,

    /// <summary>
    /// 插入新 row。
    /// </summary>
    Insert,
}

/// <summary>
/// Google Sheet 同步操作值物件。
/// 表示需要執行的單一同步操作。
/// </summary>
public sealed record SheetSyncOperation
{
    /// <summary>
    /// 操作類型 (Update 或 Insert)。
    /// </summary>
    public SheetOperationType OperationType { get; init; }

    /// <summary>
    /// 目標 Row 編號。
    /// Update: 現有 row 編號; Insert: 插入位置。
    /// </summary>
    public int TargetRowNumber { get; init; }

    /// <summary>
    /// Row 資料。
    /// </summary>
    public required SheetRowData RowData { get; init; }
}
