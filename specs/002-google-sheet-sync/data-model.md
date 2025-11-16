# Data Model: Google Sheet 同步匯出功能

**Branch**: `002-google-sheet-sync`
**Date**: 2025-11-16

## Entity & Value Object 定義

### 1. GoogleSheetColumnMapping (Value Object)

表示 Google Sheet 欄位對應設定的值物件。

```csharp
/// <summary>
/// Google Sheet 欄位對應設定值物件
/// </summary>
public sealed record GoogleSheetColumnMapping
{
    /// <summary>
    /// Repository 名稱欄位 (預設: "Z")
    /// </summary>
    public string RepositoryNameColumn { get; init; } = "Z";

    /// <summary>
    /// Feature (Work Item) 欄位 (預設: "B")
    /// </summary>
    public string FeatureColumn { get; init; } = "B";

    /// <summary>
    /// 上線團隊欄位 (預設: "D")
    /// </summary>
    public string TeamColumn { get; init; } = "D";

    /// <summary>
    /// RD 負責人欄位 (預設: "W")
    /// </summary>
    public string AuthorsColumn { get; init; } = "W";

    /// <summary>
    /// PR/MR 連結欄位 (預設: "X")
    /// </summary>
    public string PullRequestUrlsColumn { get; init; } = "X";

    /// <summary>
    /// Unique Key 欄位 (預設: "Y")
    /// </summary>
    public string UniqueKeyColumn { get; init; } = "Y";
}
```

**Validation Rules**:
- 所有欄位值必須是 A-Z 或 AA-ZZ 格式
- 欄位不可重複 (避免寫入衝突)

---

### 2. GoogleSheetConfiguration (Value Object)

表示 Google Sheet 同步所需的完整組態。

```csharp
/// <summary>
/// Google Sheet 同步組態值物件
/// </summary>
public sealed record GoogleSheetConfiguration
{
    /// <summary>
    /// Google Sheet ID (Spreadsheet ID)
    /// </summary>
    public required string SpreadsheetId { get; init; }

    /// <summary>
    /// 目標工作表名稱 (Sheet Name)
    /// </summary>
    public required string SheetName { get; init; }

    /// <summary>
    /// Service Account 憑證檔案路徑
    /// </summary>
    public required string ServiceAccountCredentialPath { get; init; }

    /// <summary>
    /// 欄位對應設定
    /// </summary>
    public GoogleSheetColumnMapping ColumnMapping { get; init; } = new();
}
```

**Validation Rules**:
- SpreadsheetId 不可為空
- SheetName 不可為空
- ServiceAccountCredentialPath 必須指向有效的 JSON 檔案
- ColumnMapping 必須通過欄位驗證

---

### 3. SheetRowData (Value Object)

表示 Google Sheet 中單一 row 的資料。

```csharp
/// <summary>
/// Google Sheet row 資料值物件
/// </summary>
public sealed record SheetRowData
{
    /// <summary>
    /// Row 編號 (1-based index)
    /// </summary>
    public int RowNumber { get; init; }

    /// <summary>
    /// Unique Key (WorkItemId + RepositoryName)
    /// </summary>
    public string UniqueKey { get; init; } = string.Empty;

    /// <summary>
    /// Repository 名稱
    /// </summary>
    public string RepositoryName { get; init; } = string.Empty;

    /// <summary>
    /// Feature 描述 (VSTS{ID} - {Title})
    /// </summary>
    public string Feature { get; init; } = string.Empty;

    /// <summary>
    /// Feature 超連結 URL
    /// </summary>
    public string? FeatureUrl { get; init; }

    /// <summary>
    /// 上線團隊
    /// </summary>
    public string Team { get; init; } = string.Empty;

    /// <summary>
    /// RD 負責人清單 (換行分隔)
    /// </summary>
    public HashSet<string> Authors { get; init; } = new();

    /// <summary>
    /// PR/MR 連結清單 (換行分隔)
    /// </summary>
    public HashSet<string> PullRequestUrls { get; init; } = new();
}
```

**Validation Rules**:
- RowNumber 必須大於 0
- UniqueKey 不可為空 (組合格式: {WorkItemId}{RepositoryName})
- RepositoryName 不可為空

---

### 4. SheetSyncOperation (Value Object)

表示需要執行的同步操作。

```csharp
/// <summary>
/// Google Sheet 同步操作類型
/// </summary>
public enum SheetOperationType
{
    /// <summary>
    /// 更新現有 row
    /// </summary>
    Update,

    /// <summary>
    /// 插入新 row
    /// </summary>
    Insert
}

/// <summary>
/// Google Sheet 同步操作值物件
/// </summary>
public sealed record SheetSyncOperation
{
    /// <summary>
    /// 操作類型
    /// </summary>
    public SheetOperationType OperationType { get; init; }

    /// <summary>
    /// 目標 Row 編號 (Update: 現有 row; Insert: 插入位置)
    /// </summary>
    public int TargetRowNumber { get; init; }

    /// <summary>
    /// Row 資料
    /// </summary>
    public required SheetRowData RowData { get; init; }
}
```

---

### 5. GoogleSheetSyncResult (Value Object)

表示同步操作的結果摘要。

```csharp
/// <summary>
/// Google Sheet 同步結果值物件
/// </summary>
public sealed record GoogleSheetSyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 更新的 row 數量
    /// </summary>
    public int UpdatedRowCount { get; init; }

    /// <summary>
    /// 新增的 row 數量
    /// </summary>
    public int InsertedRowCount { get; init; }

    /// <summary>
    /// 處理的 PR/MR 總數
    /// </summary>
    public int ProcessedPullRequestCount { get; init; }

    /// <summary>
    /// Google Sheet URL
    /// </summary>
    public string? SpreadsheetUrl { get; init; }

    /// <summary>
    /// 錯誤訊息 (若失敗)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 執行時間
    /// </summary>
    public TimeSpan ExecutionDuration { get; init; }
}
```

---

## Infrastructure Layer Models

### 6. GoogleSheetSettings (Configuration Model)

appsettings.json 中的組態模型。

```csharp
/// <summary>
/// Google Sheet 設定 (appsettings.json)
/// </summary>
public sealed class GoogleSheetSettings
{
    /// <summary>
    /// Google Sheet ID (Spreadsheet ID)
    /// </summary>
    public string? SpreadsheetId { get; set; }

    /// <summary>
    /// 目標工作表名稱 (Sheet Name)
    /// </summary>
    public string SheetName { get; set; } = "Sheet1";

    /// <summary>
    /// Service Account 憑證檔案路徑
    /// </summary>
    public string? ServiceAccountCredentialPath { get; set; }

    /// <summary>
    /// Retry 次數
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Retry 等待間隔 (秒)
    /// </summary>
    public int RetryWaitSeconds { get; set; } = 60;

    /// <summary>
    /// 欄位對應設定
    /// </summary>
    public GoogleSheetColumnMappingSettings ColumnMapping { get; set; } = new();
}

/// <summary>
/// 欄位對應設定 (appsettings.json)
/// </summary>
public sealed class GoogleSheetColumnMappingSettings
{
    public string RepositoryNameColumn { get; set; } = "Z";
    public string FeatureColumn { get; set; } = "B";
    public string TeamColumn { get; set; } = "D";
    public string AuthorsColumn { get; set; } = "W";
    public string PullRequestUrlsColumn { get; set; } = "X";
    public string UniqueKeyColumn { get; set; } = "Y";
}
```

---

## Entity Relationships

```
GoogleSheetConfiguration (Root)
├── SpreadsheetId (string)
├── SheetName (string)
├── ServiceAccountCredentialPath (string)
└── GoogleSheetColumnMapping (Value Object)
    ├── RepositoryNameColumn
    ├── FeatureColumn
    ├── TeamColumn
    ├── AuthorsColumn
    ├── PullRequestUrlsColumn
    └── UniqueKeyColumn

SheetSyncOperation (Operation Entity)
├── OperationType (enum)
├── TargetRowNumber (int)
└── SheetRowData (Value Object)
    ├── RowNumber
    ├── UniqueKey (WorkItemId + RepositoryName)
    ├── RepositoryName
    ├── Feature (VSTS{ID} - {Title})
    ├── FeatureUrl
    ├── Team
    ├── Authors (HashSet<string>)
    └── PullRequestUrls (HashSet<string>)

GoogleSheetSyncResult (Result Entity)
├── IsSuccess
├── UpdatedRowCount
├── InsertedRowCount
├── ProcessedPullRequestCount
├── SpreadsheetUrl
├── ErrorMessage
└── ExecutionDuration
```

---

## Data Flow

```
RepositoryBasedOutputDto (Input)
    ↓
GoogleSheetDataMapper (轉換)
    ↓
List<SheetRowData> (記憶體中的 Sheet 資料)
    ↓
GoogleSheetSyncEngine (比對 UK)
    ↓
List<SheetSyncOperation> (同步操作清單)
    ↓
GoogleSheetApiClient (批次寫入)
    ↓
GoogleSheetSyncResult (結果)
```

---

## State Transitions

### SheetRowData 狀態轉換

1. **New**: 從 PullRequest 資料建立
2. **Matched**: 與現有 Sheet row 配對 (UK 相同)
3. **Merged**: Authors 與 PullRequestUrls 合併
4. **Pending Write**: 等待寫入 Sheet

### GoogleSheetSyncResult 狀態

1. **Success**: 所有操作完成
2. **PartialSuccess**: 部分操作失敗 (若支援)
3. **Failure**: 整體操作失敗

---

## Unique Key (UK) 格式

UK 用於識別 Google Sheet 中的唯一 row：

```
Format: {WorkItemId}{RepositoryName}
Example: 543646nine1.nine1payment.clientadminapi
```

**組成規則**:
- WorkItemId: 數字 (從 workItem.workItemId 或解析 PR title/branch)
- RepositoryName: 完整 repository 名稱

**重要**: UK 必須能夠唯一識別一個 Work Item 在特定 Repository 的變更。同一個 Work Item 可能跨多個 Repository 有變更，因此需要結合 Repository 名稱。
