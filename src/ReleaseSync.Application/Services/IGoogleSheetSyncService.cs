// <copyright file="IGoogleSheetSyncService.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Models;

namespace ReleaseSync.Application.Services;

/// <summary>
/// Google Sheet 同步服務介面。
/// 負責協調 Google Sheet 的資料同步流程。
/// </summary>
public interface IGoogleSheetSyncService
{
    /// <summary>
    /// 驗證 Google Sheet 組態設定是否完整有效。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>非同步任務。</returns>
    Task ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步資料至 Google Sheet。
    /// </summary>
    /// <param name="repositoryData">要同步的 Repository 基礎資料。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>同步結果。</returns>
    Task<GoogleSheetSyncResult> SyncAsync(RepositoryBasedOutputDto repositoryData, CancellationToken cancellationToken = default);
}
