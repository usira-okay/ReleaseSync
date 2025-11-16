# Quickstart: Google Sheet 同步匯出功能

**Branch**: `002-google-sheet-sync`
**Date**: 2025-11-16

## 前置需求

### 1. Google Cloud Console 設定

1. 前往 [Google Cloud Console](https://console.cloud.google.com/)
2. 建立新專案或選擇現有專案
3. 啟用 **Google Sheets API**:
   - 導航至 APIs & Services > Library
   - 搜尋 "Google Sheets API"
   - 點擊 "Enable"

### 2. Service Account 建立

1. 在 Google Cloud Console 中:
   - 導航至 IAM & Admin > Service Accounts
   - 點擊 "Create Service Account"
   - 輸入名稱 (例如: `releasesync-sheet-writer`)
   - 點擊 "Create and Continue"
   - 跳過權限設定，直接 "Done"

2. 產生 JSON 憑證:
   - 點擊新建立的 Service Account
   - 切換到 "Keys" 頁籤
   - 點擊 "Add Key" > "Create new key"
   - 選擇 "JSON" 格式
   - 下載並儲存 JSON 檔案至安全位置

3. 記錄 Service Account 電子郵件:
   - 格式類似: `releasesync-sheet-writer@project-id.iam.gserviceaccount.com`

### 3. Google Sheet 授權

1. 開啟目標 Google Sheet
2. 點擊右上角 "Share" 按鈕
3. 將 Service Account 電子郵件加入，授予 "Editor" 權限
4. 記錄 Google Sheet ID:
   - URL 格式: `https://docs.google.com/spreadsheets/d/{SPREADSHEET_ID}/edit`
   - 複製 `{SPREADSHEET_ID}` 部分

---

## 安裝步驟

### 1. 安裝必要 NuGet 套件

```bash
cd src/ReleaseSync.Infrastructure
dotnet add package Google.Apis.Sheets.v4
dotnet add package Polly
dotnet add package Microsoft.Extensions.Http.Polly
```

### 2. 設定 appsettings.json

編輯 `src/ReleaseSync.Console/appsettings.json`:

```json
{
  "GoogleSheet": {
    "SpreadsheetId": "你的-Google-Sheet-ID",
    "SheetName": "工作表1",
    "ServiceAccountCredentialPath": "C:/path/to/service-account.json",
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

**注意**: 憑證檔案路徑不應包含敏感資訊在日誌中。

### 3. 設定 User Secrets (建議)

使用 User Secrets 管理敏感路徑:

```bash
cd src/ReleaseSync.Console
dotnet user-secrets set "GoogleSheet:ServiceAccountCredentialPath" "C:/secure/path/to/service-account.json"
dotnet user-secrets set "GoogleSheet:SpreadsheetId" "your-spreadsheet-id"
```

---

## 使用方式

### 基本使用 (條件一：即時同步)

```bash
# 從版控平台取得資料並同步至 Google Sheet
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --azdo \
  --google-sheet
```

### 從 JSON 檔案同步 (條件二)

```bash
# 先匯出 JSON
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --azdo \
  --export \
  -o output.json

# 再從 JSON 同步至 Google Sheet
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  -o output.json \
  --google-sheet
```

### 覆蓋組態設定

```bash
# 使用命令列參數覆蓋 appsettings.json
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --google-sheet \
  --google-sheet-id "alternate-sheet-id" \
  --google-sheet-name "其他工作表"
```

### 同時匯出 JSON 與 Google Sheet

```bash
# 兩者同時執行
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --azdo \
  --export \
  -o output.json \
  --google-sheet
```

---

## Google Sheet 工作表設定

確保目標工作表包含以下欄位標題 (第一行):

| 欄位 | 建議標題 | 說明 |
|------|---------|------|
| B | Feature | VSTS{ID} - {Title}，含超連結 |
| D | 上線團隊 | Work Item 所屬團隊 |
| W | RD 負責人 | PR 作者，多人換行分隔 |
| X | PR/MR 連結 | PR URL，多筆換行分隔 |
| Y | UK | Unique Key: {WorkItemId}{RepositoryName} |
| Z | Repository | Repository 名稱 |

**重要**: 欄位位置可透過 `ColumnMapping` 設定自訂。

---

## 預期輸出

### Console 輸出範例

```
[2025-11-16 10:30:45] 開始 Google Sheet 同步...
[2025-11-16 10:30:46] 驗證憑證成功
[2025-11-16 10:30:47] 連接至 Google Sheet: your-spreadsheet-id
[2025-11-16 10:30:48] 讀取現有資料: 150 rows
[2025-11-16 10:30:49] 處理 PR/MR 資料: 25 repositories, 87 pull requests
[2025-11-16 10:30:50] 比對 Unique Key...
[2025-11-16 10:30:51] 計畫操作: 12 updates, 8 inserts
[2025-11-16 10:30:55] 批次寫入完成
[2025-11-16 10:30:55] 同步成功完成
                      - 更新: 12 rows
                      - 新增: 8 rows
                      - 處理 PR/MR: 87
                      - 執行時間: 10.2 秒
                      - Google Sheet: https://docs.google.com/spreadsheets/d/your-id/edit
```

### 錯誤處理範例

**憑證不存在**:
```
[ERROR] Google Sheet 同步失敗: 找不到 Service Account 憑證檔案
        路徑: C:/invalid/path/credentials.json
        解決方案: 請確認憑證檔案路徑正確，並具有讀取權限
```

**Sheet 不存在**:
```
[ERROR] Google Sheet 同步失敗: Google Sheet 不存在或無法存取
        ID: invalid-spreadsheet-id
        解決方案: 請確認 Google Sheet ID 正確，並確保 Service Account 已被授予存取權限
```

**工作表不存在**:
```
[ERROR] Google Sheet 同步失敗: 工作表不存在
        名稱: 不存在的工作表
        解決方案: 請在 Google Sheet 中建立此工作表，或更正設定中的工作表名稱
```

---

## 測試驗證

### 1. 驗證組態設定

```bash
# 使用 verbose 模式檢查設定載入
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --google-sheet \
  -v
```

### 2. 檢查 Google Sheet 連線

執行同步命令，觀察：
- 憑證驗證是否成功
- 能否讀取現有 Sheet 資料
- 能否正確識別欄位位置

### 3. 驗證資料格式

同步後檢查 Google Sheet：
- Feature 欄位是否包含超連結
- RD 負責人是否正確換行分隔
- UK 格式是否正確

---

## 常見問題排解

### Q1: 出現 403 Forbidden 錯誤

**原因**: Service Account 沒有 Google Sheet 存取權限

**解決方案**:
1. 開啟 Google Sheet
2. 點擊 Share
3. 加入 Service Account email 並授予 Editor 權限

### Q2: 出現 Rate Limit 錯誤

**原因**: 超過 Google Sheets API 速率限制

**解決方案**:
- 系統會自動 retry (預設 3 次，每次等待 60 秒)
- 可增加 `RetryWaitSeconds` 設定
- 確保不要頻繁執行同步

### Q3: 欄位資料寫入位置錯誤

**原因**: ColumnMapping 設定與實際工作表不符

**解決方案**:
- 檢查 appsettings.json 中的 ColumnMapping
- 確認欄位字母與工作表一致
- 使用 verbose 模式查看載入的設定

### Q4: 日期時間格式問題

**原因**: 時區轉換差異

**解決方案**:
- 系統使用 UTC 時間
- 確保查詢日期範圍正確

---

## 進階設定

### 自訂欄位對應

如果工作表結構不同，可調整 ColumnMapping:

```json
{
  "GoogleSheet": {
    "ColumnMapping": {
      "RepositoryNameColumn": "A",
      "FeatureColumn": "C",
      "TeamColumn": "E",
      "AuthorsColumn": "F",
      "PullRequestUrlsColumn": "G",
      "UniqueKeyColumn": "H"
    }
  }
}
```

### 調整 Retry 策略

```json
{
  "GoogleSheet": {
    "RetryCount": 5,
    "RetryWaitSeconds": 120
  }
}
```

---

## 相關文件

- [功能規格](./spec.md)
- [研究報告](./research.md)
- [資料模型](./data-model.md)
- [服務介面](./contracts/interfaces.md)
- [專案憲章](../../.specify/memory/constitution.md)
