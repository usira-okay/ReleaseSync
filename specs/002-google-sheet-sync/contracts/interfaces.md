# Service Interfaces: Google Sheet 同步匯出功能

**Branch**: `002-google-sheet-sync`
**Date**: 2025-11-16

## Application Layer Interfaces

### 1. IGoogleSheetSyncService

負責協調 Google Sheet 同步的核心服務介面。

```csharp
/// <summary>
/// Google Sheet 同步服務介面
/// </summary>
public interface IGoogleSheetSyncService
{
    /// <summary>
    /// 將 Repository-based 資料同步至 Google Sheet
    /// </summary>
    /// <param name="data">PR/MR 資料 (Repository-based 格式)</param>
    /// <param name="cancellationToken">取消 token</param>
    /// <returns>同步結果</returns>
    Task<GoogleSheetSyncResult> SyncAsync(
        RepositoryBasedOutputDto data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 驗證 Google Sheet 設定是否正確
    /// </summary>
    /// <returns>驗證結果 (true: 通過, false: 失敗)</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateConfigurationAsync(
        CancellationToken cancellationToken = default);
}
```

**Implementation Notes**:
- 實作類別: `GoogleSheetSyncService`
- 位置: `ReleaseSync.Application/Services/`
- 依賴: `IGoogleSheetApiClient`, `IGoogleSheetDataMapper`, `IWorkItemIdParser`, `IOptions<GoogleSheetSettings>`

---

### 2. IGoogleSheetDataMapper

負責將 PR/MR 資料轉換為 Sheet row 資料格式。

```csharp
/// <summary>
/// Google Sheet 資料對應器介面
/// </summary>
public interface IGoogleSheetDataMapper
{
    /// <summary>
    /// 將 Repository PR/MR 資料轉換為 Sheet row 資料
    /// </summary>
    /// <param name="repository">Repository 分組資料</param>
    /// <returns>Sheet row 資料清單</returns>
    IEnumerable<SheetRowData> MapToSheetRows(RepositoryGroupDto repository);

    /// <summary>
    /// 合併兩筆 SheetRowData (用於更新現有 row)
    /// </summary>
    /// <param name="existing">現有 row 資料</param>
    /// <param name="incoming">新的 PR/MR 資料</param>
    /// <returns>合併後的 row 資料</returns>
    SheetRowData MergeRowData(SheetRowData existing, SheetRowData incoming);

    /// <summary>
    /// 產生 Unique Key
    /// </summary>
    /// <param name="workItemId">Work Item ID</param>
    /// <param name="repositoryName">Repository 名稱</param>
    /// <returns>Unique Key 字串</returns>
    string GenerateUniqueKey(int workItemId, string repositoryName);
}
```

**Implementation Notes**:
- 實作類別: `GoogleSheetDataMapper`
- 位置: `ReleaseSync.Application/Mappers/`
- 依賴: `IWorkItemIdParser`, `IOptions<GoogleSheetSettings>`

---

## Infrastructure Layer Interfaces

### 3. IGoogleSheetApiClient

負責與 Google Sheets API 互動的低階客戶端。

```csharp
/// <summary>
/// Google Sheets API 客戶端介面
/// </summary>
public interface IGoogleSheetApiClient
{
    /// <summary>
    /// 驗證憑證並連線至 Google Sheet
    /// </summary>
    /// <returns>連線結果</returns>
    Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查 Spreadsheet 是否存在
    /// </summary>
    /// <param name="spreadsheetId">Spreadsheet ID</param>
    /// <returns>是否存在</returns>
    Task<bool> SpreadsheetExistsAsync(
        string spreadsheetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查工作表是否存在
    /// </summary>
    /// <param name="spreadsheetId">Spreadsheet ID</param>
    /// <param name="sheetName">工作表名稱</param>
    /// <returns>是否存在</returns>
    Task<bool> SheetExistsAsync(
        string spreadsheetId,
        string sheetName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 讀取工作表所有資料
    /// </summary>
    /// <param name="spreadsheetId">Spreadsheet ID</param>
    /// <param name="sheetName">工作表名稱</param>
    /// <returns>二維陣列格式的資料</returns>
    Task<IList<IList<object>>> ReadSheetDataAsync(
        string spreadsheetId,
        string sheetName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批次更新工作表資料
    /// </summary>
    /// <param name="spreadsheetId">Spreadsheet ID</param>
    /// <param name="operations">同步操作清單</param>
    /// <returns>更新結果</returns>
    Task<GoogleSheetSyncResult> BatchUpdateAsync(
        string spreadsheetId,
        string sheetName,
        IEnumerable<SheetSyncOperation> operations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得 Spreadsheet URL
    /// </summary>
    /// <param name="spreadsheetId">Spreadsheet ID</param>
    /// <returns>完整 URL</returns>
    string GetSpreadsheetUrl(string spreadsheetId);
}
```

**Implementation Notes**:
- 實作類別: `GoogleSheetApiClient`
- 位置: `ReleaseSync.Infrastructure/GoogleSheet/`
- 依賴: `Google.Apis.Sheets.v4`, `IOptions<GoogleSheetSettings>`, Polly retry policy
- 重要: 所有 API 呼叫必須透過 Polly retry 包裝

---

### 4. IGoogleSheetRowParser

負責解析 Google Sheet row 資料。

```csharp
/// <summary>
/// Google Sheet row 解析器介面
/// </summary>
public interface IGoogleSheetRowParser
{
    /// <summary>
    /// 從 Google Sheet 原始資料解析為 SheetRowData
    /// </summary>
    /// <param name="rowNumber">Row 編號 (1-based)</param>
    /// <param name="rowData">原始 row 資料</param>
    /// <param name="columnMapping">欄位對應設定</param>
    /// <returns>解析後的 SheetRowData</returns>
    SheetRowData ParseRow(
        int rowNumber,
        IList<object> rowData,
        GoogleSheetColumnMapping columnMapping);

    /// <summary>
    /// 將 SheetRowData 轉換為 Google Sheet 格式
    /// </summary>
    /// <param name="rowData">SheetRowData</param>
    /// <param name="columnMapping">欄位對應設定</param>
    /// <returns>Google Sheet API 格式的資料</returns>
    IList<object> ConvertToSheetFormat(
        SheetRowData rowData,
        GoogleSheetColumnMapping columnMapping);
}
```

**Implementation Notes**:
- 實作類別: `GoogleSheetRowParser`
- 位置: `ReleaseSync.Infrastructure/GoogleSheet/`
- 負責: 欄位字母 ↔ 索引轉換、換行分隔符處理、超連結格式

---

## Console Layer Interfaces

### 5. Command Options Extension

SyncCommandOptions 的擴展欄位。

```csharp
/// <summary>
/// Sync 命令選項 (擴展 Google Sheet 設定)
/// </summary>
public sealed record SyncCommandOptions
{
    // ... 現有欄位 ...

    /// <summary>
    /// 是否啟用 Google Sheet 同步
    /// </summary>
    public bool EnableGoogleSheet { get; init; }

    /// <summary>
    /// Google Sheet ID (可覆蓋 appsettings.json 設定)
    /// </summary>
    public string? GoogleSheetId { get; init; }

    /// <summary>
    /// 工作表名稱 (可覆蓋 appsettings.json 設定)
    /// </summary>
    public string? GoogleSheetName { get; init; }
}
```

---

## Exception Types

### 6. GoogleSheetSyncException

自訂例外類型，表達 Google Sheet 同步相關錯誤。

```csharp
/// <summary>
/// Google Sheet 同步例外基底類別
/// </summary>
public class GoogleSheetSyncException : Exception
{
    public GoogleSheetSyncException(string message) : base(message) { }
    public GoogleSheetSyncException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// 憑證無效例外
/// </summary>
public class GoogleSheetCredentialException : GoogleSheetSyncException
{
    public string CredentialPath { get; }

    public GoogleSheetCredentialException(string credentialPath, string message)
        : base(message)
    {
        CredentialPath = credentialPath;
    }
}

/// <summary>
/// Spreadsheet 不存在例外
/// </summary>
public class GoogleSheetNotFoundException : GoogleSheetSyncException
{
    public string SpreadsheetId { get; }

    public GoogleSheetNotFoundException(string spreadsheetId)
        : base($"Google Sheet 不存在或無法存取: {spreadsheetId}")
    {
        SpreadsheetId = spreadsheetId;
    }
}

/// <summary>
/// 工作表不存在例外
/// </summary>
public class GoogleSheetWorksheetNotFoundException : GoogleSheetSyncException
{
    public string SheetName { get; }

    public GoogleSheetWorksheetNotFoundException(string sheetName)
        : base($"工作表不存在: {sheetName}")
    {
        SheetName = sheetName;
    }
}

/// <summary>
/// 設定驗證失敗例外
/// </summary>
public class GoogleSheetConfigurationException : GoogleSheetSyncException
{
    public GoogleSheetConfigurationException(string message) : base(message) { }
}
```

---

## Dependency Injection Extensions

### 7. GoogleSheetServiceExtensions

DI 註冊擴展方法。

```csharp
/// <summary>
/// Google Sheet 服務 DI 擴展方法
/// </summary>
public static class GoogleSheetServiceExtensions
{
    /// <summary>
    /// 註冊 Google Sheet 相關服務
    /// </summary>
    public static IServiceCollection AddGoogleSheetServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 綁定設定
        services.Configure<GoogleSheetSettings>(
            configuration.GetSection("GoogleSheet"));

        // 註冊 Infrastructure 層服務
        services.AddScoped<IGoogleSheetApiClient, GoogleSheetApiClient>();
        services.AddScoped<IGoogleSheetRowParser, GoogleSheetRowParser>();

        // 註冊 Application 層服務
        services.AddScoped<IGoogleSheetDataMapper, GoogleSheetDataMapper>();
        services.AddScoped<IGoogleSheetSyncService, GoogleSheetSyncService>();

        return services;
    }
}
```

---

## Configuration Schema

### 8. appsettings.json 結構

```json
{
  "GoogleSheet": {
    "SpreadsheetId": "your-spreadsheet-id-here",
    "SheetName": "工作表1",
    "ServiceAccountCredentialPath": "path/to/service-account.json",
    "RetryCount": 3,
    "RetryWaitSeconds": 60,
    "ColumnMapping": {
      "RepositoryNameColumn": "Z",
      "FeatureColumn": "B",
      "TeamColumn": "D",
      "AuthorsColumn": "W",
      "PullRequestUrlsColumn": "X",
      "UniqueKeyColumn": "Y"
    }
  }
}
```

---

## Service Interaction Flow

```
SyncCommandHandler
    ↓ 檢查 EnableGoogleSheet
IGoogleSheetSyncService.ValidateConfigurationAsync()
    ↓ 驗證憑證與 Sheet 存在
IGoogleSheetSyncService.SyncAsync()
    ↓ 讀取現有 Sheet 資料
IGoogleSheetApiClient.ReadSheetDataAsync()
    ↓ 解析為 SheetRowData
IGoogleSheetRowParser.ParseRow()
    ↓ 轉換 PR/MR 資料
IGoogleSheetDataMapper.MapToSheetRows()
    ↓ 比對 UK 與合併資料
IGoogleSheetDataMapper.MergeRowData()
    ↓ 產生同步操作
List<SheetSyncOperation>
    ↓ 批次寫入
IGoogleSheetApiClient.BatchUpdateAsync()
    ↓ 回傳結果
GoogleSheetSyncResult
```

---

## Contract Validation Rules

### Input Validation

1. **RepositoryBasedOutputDto**:
   - 必須不為 null
   - repositories 清單可為空 (無資料時不執行同步)

2. **GoogleSheetSettings**:
   - SpreadsheetId: 非空字串
   - SheetName: 非空字串
   - ServiceAccountCredentialPath: 檔案必須存在且為有效 JSON

3. **GoogleSheetColumnMapping**:
   - 所有欄位必須符合 A-Z 或 AA-ZZ 格式
   - 欄位不可重複

### Output Guarantees

1. **GoogleSheetSyncResult**:
   - IsSuccess: 明確表示成功或失敗
   - 失敗時 ErrorMessage 不為空
   - 成功時 SpreadsheetUrl 不為空
   - ExecutionDuration 為正數

2. **SheetRowData**:
   - UniqueKey 格式: {WorkItemId}{RepositoryName}
   - Authors 和 PullRequestUrls 為 HashSet (自動去重)
   - Feature 格式: VSTS{ID} - {Title}
