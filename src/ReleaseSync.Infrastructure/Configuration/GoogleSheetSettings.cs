// <copyright file="GoogleSheetSettings.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// Google Sheet 設定 (appsettings.json)。
/// 定義 Google Sheet 同步所需的組態參數。
/// </summary>
public sealed class GoogleSheetSettings
{
    /// <summary>
    /// 組態區段名稱。
    /// </summary>
    public const string SectionName = "GoogleSheet";

    /// <summary>
    /// Google Sheet ID (Spreadsheet ID)。
    /// 從 Google Sheet URL 中取得: https://docs.google.com/spreadsheets/d/{SpreadsheetId}/edit。
    /// </summary>
    public string? SpreadsheetId { get; set; }

    /// <summary>
    /// 目標工作表名稱 (Sheet Name)。
    /// 預設為 "Sheet1"。
    /// </summary>
    public string SheetName { get; set; } = "Sheet1";

    /// <summary>
    /// Service Account 憑證檔案路徑。
    /// 指向 Google Cloud Console 下載的 JSON 憑證檔案。
    /// </summary>
    public string? ServiceAccountCredentialPath { get; set; }

    /// <summary>
    /// Retry 次數。
    /// 當遇到暫時性錯誤 (如速率限制) 時的重試次數。
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Retry 等待間隔 (秒)。
    /// 每次重試之間的等待時間。
    /// </summary>
    public int RetryWaitSeconds { get; set; } = 60;

    /// <summary>
    /// 欄位對應設定。
    /// </summary>
    public GoogleSheetColumnMappingSettings ColumnMapping { get; set; } = new();
}

/// <summary>
/// 欄位對應設定 (appsettings.json)。
/// 定義 PR/MR 資料對應到 Google Sheet 的欄位位置。
/// </summary>
public sealed class GoogleSheetColumnMappingSettings
{
    /// <summary>
    /// Repository 名稱欄位 (預設: "Z")。
    /// </summary>
    public string RepositoryNameColumn { get; set; } = "Z";

    /// <summary>
    /// Feature (Work Item) 欄位 (預設: "B")。
    /// </summary>
    public string FeatureColumn { get; set; } = "B";

    /// <summary>
    /// 上線團隊欄位 (預設: "D")。
    /// </summary>
    public string TeamColumn { get; set; } = "D";

    /// <summary>
    /// RD 負責人欄位 (預設: "W")。
    /// </summary>
    public string AuthorsColumn { get; set; } = "W";

    /// <summary>
    /// PR/MR 連結欄位 (預設: "X")。
    /// </summary>
    public string PullRequestUrlsColumn { get; set; } = "X";

    /// <summary>
    /// Unique Key 欄位 (預設: "Y")。
    /// 格式: {WorkItemId}{RepositoryName}。
    /// </summary>
    public string UniqueKeyColumn { get; set; } = "Y";
}
