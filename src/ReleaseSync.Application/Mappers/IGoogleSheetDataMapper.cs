// <copyright file="IGoogleSheetDataMapper.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Models;

namespace ReleaseSync.Application.Mappers;

/// <summary>
/// Google Sheet 資料對應器介面。
/// 負責將 RepositoryBasedOutputDto 轉換為 SheetRowData。
/// </summary>
public interface IGoogleSheetDataMapper
{
    /// <summary>
    /// 將 RepositoryBasedOutputDto 轉換為 SheetRowData 清單。
    /// 每個 (WorkItemId, RepositoryName) 組合對應一個 SheetRowData。
    /// </summary>
    /// <param name="repositoryData">Repository 基礎資料。</param>
    /// <returns>轉換後的 SheetRowData 清單。</returns>
    IReadOnlyList<SheetRowData> MapToSheetRows(RepositoryBasedOutputDto repositoryData);

    /// <summary>
    /// 產生 Unique Key。
    /// 格式: {WorkItemId}{RepositoryName}。
    /// </summary>
    /// <param name="workItemId">Work Item ID。</param>
    /// <param name="repositoryName">Repository 名稱。</param>
    /// <returns>Unique Key 字串。</returns>
    string GenerateUniqueKey(int workItemId, string repositoryName, string platform = "");
}
