# Research: Google Sheet 同步匯出功能

**Branch**: `002-google-sheet-sync`
**Date**: 2025-11-16
**Status**: Complete

## 技術決策

### 1. Google Sheets API 客戶端選擇

**Decision**: 使用 Google.Apis.Sheets.v4 官方 NuGet 套件

**Rationale**:
- 官方維護，與 Google API 同步更新
- 支援 Service Account 驗證，符合需求
- 提供強型別 API 存取
- 良好的文件與社群支援
- 支援 .NET 8.0

**Alternatives considered**:
- 直接呼叫 REST API：需要自行處理驗證與序列化，增加實作複雜度
- 第三方套件 (如 Sheetsu)：功能受限，無法支援所有需求操作
- 自建 HTTP 客戶端：需要大量實作工作，違反 KISS 原則

### 2. 驗證機制

**Decision**: 僅支援 Service Account JSON 憑證

**Rationale**:
- Service Account 適合自動化腳本與伺服器端應用
- 不需要使用者介入的 OAuth 授權流程
- 憑證檔案可透過組態指定路徑
- 符合無人值守執行的需求

**Alternatives considered**:
- OAuth 2.0 使用者授權：需要瀏覽器互動，不適合 CLI 工具
- API Key：功能受限，無法寫入私有 Sheet

### 3. 資料同步策略

**Decision**: 批次讀取現有資料 → 記憶體中處理 → 批次寫入更新

**Rationale**:
- 降低 API 呼叫次數，符合速率限制
- 使用者指定使用 Polly retry 與批次處理
- 減少網路往返，提升效能
- 允許複雜的邏輯處理（UK 比對、row 插入）

**Implementation approach**:
1. 批次讀取目標工作表所有資料 (一次 API 呼叫)
2. 在記憶體中建立 UK → Row 的索引
3. 迭代 PR/MR 資料，更新記憶體中的資料結構
4. 識別需要插入的新 rows
5. 批次寫入所有變更 (一次或少數幾次 API 呼叫)

### 4. Retry 策略

**Decision**: 使用 Polly 實作 Retry Policy

**Rationale**:
- Polly 為 .NET 標準的 resilience 套件
- 使用者明確要求 Polly retry
- 支援等待間隔配置
- 可處理 Google API 速率限制錯誤 (429 Too Many Requests)

**Configuration**:
- Retry 次數: 3
- 等待間隔: 1 分鐘
- 觸發條件: Google API Rate Limit 錯誤

### 5. 欄位對應策略

**Decision**: 透過組態檔設定欄位對應，使用 A-Z 字母表示欄位

**Rationale**:
- 使用者明確要求可配置的欄位對應
- 預設值已定義 (Z, B, D, W, X, Y)
- 字母表示法符合 Google Sheet 欄位命名習慣
- 彈性支援不同工作表結構

**Column mappings** (預設):
- Repository Name: Z
- Feature (Work Item): B
- 上線團隊: D
- RD 負責人: W
- PR/MR 連結: X
- UK (Unique Key): Y

### 6. 架構分層

**Decision**: 遵循現有 Clean Architecture 分層

**Rationale**:
- 符合專案憲章要求
- 與現有 Exporter 模式一致
- 保持程式碼一致性與可維護性
- 支援測試驅動開發

**Layer responsibilities**:
- **Application Layer**: GoogleSheetExporter (IResultExporter 實作)
- **Infrastructure Layer**: GoogleSheetApiClient (API 存取)、GoogleSheetSettings (組態)
- **Console Layer**: SyncCommand 參數處理、DI 註冊

### 7. Work Item ID 解析邏輯

**Decision**: 重用現有的 IWorkItemIdParser 實作

**Rationale**:
- 現有 RegexWorkItemIdParser 已實作完整的解析邏輯
- 支援從 PR Title 和 Source Branch 解析
- 符合 DRY 原則
- 避免重複實作

**Flow**:
1. 若 pullRequest.workItem 不為 null → 使用 workItem.workItemId
2. 若為 null → 使用 IWorkItemIdParser 解析 pullRequestTitle
3. 若解析失敗 → 使用 IWorkItemIdParser 解析 sourceBranch

### 8. 資料來源

**Decision**: 支援兩種資料來源模式

**Rationale**:
- 使用者明確要求兩種條件

**條件一** (即時同步):
- 啟用任一版控平台 (GitLab, BitBucket)
- 允許同步 Azure DevOps 資料
- googleSheetSync 為 true

**條件二** (從 JSON 檔案):
- 有設定 outputFile 且可讀取
- googleSheetSync 為 true

### 9. 錯誤處理策略

**Decision**: 部分失敗容錯，配合清楚的錯誤訊息

**Rationale**:
- 符合功能規格要求
- Google Sheet 失敗不應影響 JSON 匯出
- 提供可操作的錯誤訊息

**Error scenarios**:
- 憑證不存在/無效: 提示設定正確路徑
- Sheet 不存在: 提示 Sheet ID 無效
- 工作表不存在: 提示建立工作表
- API 速率限制: 透過 Polly retry 處理
- 權限不足: 提示授予 Service Account 權限

## 技術堆疊

- **Google Sheets API**: Google.Apis.Sheets.v4 (v1.68.x)
- **Retry Policy**: Polly (v8.x)
- **Configuration**: Microsoft.Extensions.Options
- **Logging**: Microsoft.Extensions.Logging + Serilog
- **Serialization**: System.Text.Json

## 相依性

### 新增 NuGet 套件
- Google.Apis.Sheets.v4
- Polly (若尚未使用)
- Microsoft.Extensions.Http.Polly

### 現有套件重用
- Microsoft.Extensions.Options
- Microsoft.Extensions.Logging
- System.Text.Json

## 風險與緩解

### 風險 1: Google API 速率限制
**緩解**: 使用 Polly retry + 批次處理策略

### 風險 2: 大量資料同步效能
**緩解**: 批次讀取與寫入，記憶體中處理

### 風險 3: 欄位對應錯誤
**緩解**: 組態驗證 + 清楚的錯誤訊息

### 風險 4: 並行存取衝突
**緩解**: 單次執行 (CLI 工具本身為單次執行)

## 參考資料

- [Google Sheets API v4 Documentation](https://developers.google.com/sheets/api/guides/concepts)
- [Google.Apis.Sheets.v4 NuGet](https://www.nuget.org/packages/Google.Apis.Sheets.v4)
- [Service Account Authentication](https://developers.google.com/identity/protocols/oauth2/service-account)
- [Polly Documentation](https://github.com/App-vNext/Polly)
