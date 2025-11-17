# ReleaseSync - PR/MR 變更資訊聚合工具

從 GitLab、BitBucket 和 Azure DevOps 抓取 Pull Request / Merge Request 變更資訊,並匯出為結構化 JSON 格式。

## 功能特色

- 🔄 支援多平台: GitLab, BitBucket Cloud
- 🔗 Azure DevOps Work Item 整合
- 📊 JSON 格式匯出/匯入
- 📈 Google Sheet 同步功能
- 🛡️ 部分失敗容錯處理
- 📝 詳細的日誌記錄（Serilog）
- ⚡ 並行查詢提升效能
- 🔍 Verbose 模式支援 Debug 等級日誌
- 👥 使用者對照功能（User Mapping）
- 🏢 團隊名稱對照功能（Team Mapping）
- 🔐 支援 User Secrets 安全管理敏感資訊

## 快速開始

### 1. 安裝

```bash
git clone https://github.com/yourorg/ReleaseSync.git
cd ReleaseSync
dotnet build
```

### 2. 設定

```bash
cd src/ReleaseSync.Console
cp appsettings.example.json appsettings.json
# 編輯 appsettings.json,設定專案路徑、Work Item 解析規則等

# 使用 User Secrets 儲存敏感資訊 (推薦)
dotnet user-secrets set "GitLab:PersonalAccessToken" "<YOUR_TOKEN>"
dotnet user-secrets set "BitBucket:Email" "<YOUR_EMAIL>"
dotnet user-secrets set "BitBucket:AccessToken" "<YOUR_TOKEN>"
dotnet user-secrets set "AzureDevOps:PersonalAccessToken" "<YOUR_TOKEN>"
```

### 3. 執行

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab \
  --enable-bitbucket \
  --export \
  --output-file output.json
```

## 使用範例

### 基本同步

從 GitLab 抓取過去 30 天的 MR 資訊:

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab
```

### 匯出 JSON 格式

同時抓取 GitLab 與 BitBucket 並匯出為 JSON:

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --bitbucket \
  --export \
  -o ./output/sync-result.json
```

**注意**: 使用 `--export` 參數啟用 JSON 匯出功能,並透過 `-o` 指定輸出檔案路徑。

### Azure DevOps Work Item 整合

啟用 Work Item 整合,從 Branch 名稱解析並抓取 Work Item 資訊:

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --azdo \
  --export \
  -o ./output/full-sync.json \
  --verbose
```

### Google Sheet 同步

同步資料到 Google Sheet (需先設定服務帳號憑證):

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --bitbucket \
  --google-sheet
```

## 命令列參數

| 參數 | 別名 | 說明 |
|------|------|------|
| `--start-date` | `-s` | 查詢起始日期 (必填) |
| `--end-date` | `-e` | 查詢結束日期 (必填) |
| `--enable-gitlab` | `--gitlab` | 啟用 GitLab 平台 |
| `--enable-bitbucket` | `--bitbucket` | 啟用 BitBucket 平台 |
| `--enable-azure-devops` | `--azdo` | 啟用 Azure DevOps Work Item 整合 |
| `--enable-export` | `--export` | 啟用 JSON 匯出功能 |
| `--output-file` | `-o` | JSON 匯出檔案路徑 |
| `--force` | `-f` | 強制覆蓋已存在的輸出檔案 |
| `--verbose` | `-v` | 啟用詳細日誌輸出 (Debug 等級) |
| `--enable-google-sheet` | `--google-sheet` | 啟用 Google Sheet 同步功能 |

## 組態設定

### appsettings.json

設定 API URL、專案清單、Work Item 解析規則等:

```json
{
  "GitLab": {
    "ApiUrl": "https://gitlab.com/api/v4",
    "Projects": [
      {
        "ProjectPath": "mygroup/backend-api",
        "TargetBranches": ["main", "develop"]
      }
    ]
  },
  "BitBucket": {
    "ApiUrl": "https://api.bitbucket.org/2.0",
    "Projects": [
      {
        "WorkspaceAndRepo": "myworkspace/myrepo",
        "TargetBranches": ["main"]
      }
    ]
  },
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/myorganization",
    "ProjectName": "MyProject",
    "WorkItemIdPatterns": [
      {
        "Name": "VSTS Pattern",
        "Regex": "vsts(\\d+)",
        "IgnoreCase": true,
        "CaptureGroup": 1
      },
      {
        "Name": "Feature Pattern",
        "Regex": "feature/(\\d+)-",
        "IgnoreCase": false,
        "CaptureGroup": 1
      }
    ],
    "ParsingBehavior": {
      "OnParseFailure": "LogWarningAndContinue",
      "StopOnFirstMatch": true
    },
    "TeamMapping": [
      {
        "OriginalTeamName": "MoneyLogistic",
        "DisplayName": "金流團隊"
      },
      {
        "OriginalTeamName": "DailyResource",
        "DisplayName": "日常資源團隊"
      }
    ]
  },
  "UserMapping": [
    {
      "GitLabUserId": "john.doe",
      "BitBucketUserId": "jdoe",
      "DisplayName": "John Doe"
    }
  ]
}
```

### User Secrets 設定 (敏感資訊)

**使用 User Secrets 儲存 API Tokens (推薦)**

User Secrets 將敏感資訊儲存在使用者設定檔中 (`~/.microsoft/usersecrets/`),完全不會被提交至版本控制:

```bash
cd src/ReleaseSync.Console

# 版控平台 Tokens
dotnet user-secrets set "GitLab:PersonalAccessToken" "glpat-xxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "BitBucket:Email" "your.email@example.com"
dotnet user-secrets set "BitBucket:AccessToken" "ATBB..."
dotnet user-secrets set "AzureDevOps:PersonalAccessToken" "xxxxxxxxxxxxxxxxxxxx"

# Google Sheet 設定 (選用)
dotnet user-secrets set "GoogleSheet:SpreadsheetId" "your-spreadsheet-id"
dotnet user-secrets set "GoogleSheet:SheetName" "Sheet1"
dotnet user-secrets set "GoogleSheet:ServiceAccountCredentialPath" "/path/to/service-account.json"
```

**或者直接將敏感資訊加入 appsettings.json (不推薦)**

若您不想使用 User Secrets,也可以直接將 Token 寫入 `appsettings.json` 的對應區段,但請務必確保該檔案不會被提交至版本控制。

## 進階功能

### JSON 檔案匯入

除了從版控平台抓取資料,也支援從既有的 JSON 檔案匯入資料,方便資料重複使用或離線處理:

```bash
# 透過程式碼使用 JsonFileImporter
# 範例程式碼:
var importer = new JsonFileImporter();
var results = await importer.ImportAsync("output.json");
```

**注意**: 此功能主要供程式內部使用,目前未提供命令列介面。

### User Mapping (使用者對照)

當同一位開發者在不同平台使用不同的使用者 ID 時,可透過 User Mapping 進行對照,讓匯出的 JSON 使用統一的顯示名稱:

```json
"UserMapping": [
  {
    "GitLabUserId": "john.doe",
    "BitBucketUserId": "jdoe",
    "DisplayName": "John Doe"
  }
]
```

### Team Mapping (團隊名稱對照)

將 Azure DevOps Work Item 中的團隊名稱對照為更易讀的顯示名稱:

```json
"TeamMapping": [
  {
    "OriginalTeamName": "MoneyLogistic",
    "DisplayName": "金流團隊"
  }
]
```

### Work Item ID 解析規則

支援多種 Branch 命名模式,自動從 Branch 名稱解析 Work Item ID:

```json
"WorkItemIdPatterns": [
  {
    "Name": "VSTS Pattern",
    "Regex": "vsts(\\d+)",
    "IgnoreCase": true,
    "CaptureGroup": 1
  },
  {
    "Name": "Feature Pattern",
    "Regex": "feature/(\\d+)-",
    "IgnoreCase": false,
    "CaptureGroup": 1
  }
]
```

**解析行為設定:**
- `OnParseFailure`: 當無法解析 Work Item ID 時的處理方式（LogWarningAndContinue 或 ThrowException）
- `StopOnFirstMatch`: 找到第一個符合的規則後即停止（true 推薦）

### Google Sheet 整合

將同步結果自動上傳至 Google Sheet,方便團隊協作與資料視覺化:

**前置需求:**
1. 在 Google Cloud Console 建立專案並啟用 Google Sheets API
2. 建立服務帳號並下載 JSON 金鑰檔
3. 將服務帳號的 Email 加入目標 Google Sheet 的編輯者權限

**組態設定 (appsettings.json 或 User Secrets):**

```json
{
  "GoogleSheet": {
    "SpreadsheetId": "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms",
    "SheetName": "Sheet1",
    "ServiceAccountCredentialPath": "path/to/service-account.json",
    "RetryCount": 3,
    "RetryWaitSeconds": 60,
    "ColumnMapping": {
      "UniqueKeyColumn": "Y",
      "FeatureColumn": "B",
      "TeamColumn": "D",
      "AuthorsColumn": "W",
      "PullRequestUrlsColumn": "X",
      "RepositoryNameColumn": "Z"
    }
  }
}
```

**使用方式:**

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --bitbucket \
  --google-sheet
```

同步後,資料將自動寫入指定的 Google Sheet,包含以下欄位:
- **Unique Key** (Y 欄) - 唯一識別碼 (WorkItemId + RepositoryName)
- **Feature** (B 欄) - Work Item 描述 (格式: VSTS{ID} - {Title})
- **Team** (D 欄) - 上線團隊名稱
- **Authors** (W 欄) - RD 負責人清單 (多人以換行分隔)
- **PR/MR URLs** (X 欄) - Pull Request 連結清單 (多筆以換行分隔)
- **Repository Name** (Z 欄) - 專案名稱

**注意**: 欄位位置可透過 `appsettings.json` 的 `GoogleSheet:ColumnMapping` 自訂。

## 錯誤處理

工具提供友善的錯誤訊息,協助快速診斷問題:

### 認證失敗

```
❌ 認證失敗!
請檢查以下項目:
  1. 確認 User Secrets 或 appsettings.json 中的 Token 正確
  2. 確認 Token 未過期
  3. 確認 Token 權限足夠 (GitLab: api, read_repository)
```

### 網路連線失敗

```
❌ 網路連線失敗!
請檢查:
  1. 網路連線是否正常
  2. API URL 是否正確 (appsettings.json)
  3. 錯誤訊息: ...
```

### 組態檔遺失

```
❌ 找不到組態檔!
請確認以下檔案存在:
  - appsettings.json (可從 appsettings.example.json 複製)

敏感資訊設定方式:
  使用 User Secrets (推薦)
    dotnet user-secrets set "GitLab:PersonalAccessToken" "<YOUR_TOKEN>"
    dotnet user-secrets set "BitBucket:Email" "<YOUR_EMAIL>"
    dotnet user-secrets set "BitBucket:AccessToken" "<YOUR_TOKEN>"
```

## 效能

- 並行查詢多個專案,提升抓取效率
- 目標效能: 100 筆 PR/MR 於 30 秒內完成 (不含網路 I/O)
- 自動記錄效能指標,監控執行效率

## 專案架構

專案遵循 Clean Architecture 與 DDD Tactical Patterns:

```
src/
├── ReleaseSync.Domain/          # 核心領域模型
│   ├── Models/                  # 實體與值物件
│   ├── Services/                # 領域服務介面
│   └── Repositories/            # Repository 介面
├── ReleaseSync.Application/     # 應用層
│   ├── Services/                # 應用服務 (SyncOrchestrator, GoogleSheetSyncService)
│   ├── DTOs/                    # 資料傳輸物件
│   ├── Exporters/               # 匯出器 (JsonFileExporter)
│   └── Importers/               # 匯入器 (JsonFileImporter)
├── ReleaseSync.Infrastructure/  # 基礎設施層
│   ├── Platforms/               # 平台整合 (GitLab, BitBucket, Azure DevOps)
│   ├── GoogleSheet/             # Google Sheet API 整合
│   ├── Configuration/           # 組態模型
│   └── Parsers/                 # Work Item ID 解析器
└── ReleaseSync.Console/         # 命令列介面
    ├── Commands/                # 命令定義
    └── Handlers/                # 命令處理器
```

## 開發指南

### 前置需求

- .NET 9.0 SDK
- 存取 GitLab / BitBucket / Azure DevOps API 的權限
- (選用) Google Cloud 服務帳號 (若需使用 Google Sheet 功能)

### 建置

```bash
# 建置整個解決方案
dotnet build src/src.sln

# Release 模式建置
dotnet build src/src.sln -c Release
```

### 測試

```bash
# 執行所有測試
dotnet test src/src.sln

# 執行單元測試 (排除整合測試)
dotnet test src/src.sln --filter "FullyQualifiedName!~Integration"

# 產生測試覆蓋率報告
dotnet test src/src.sln --collect:"XPlat Code Coverage"
```

### 程式碼品質

專案遵循以下原則:

- ✅ SOLID 原則
- ✅ KISS 原則 (Keep It Simple, Stupid)
- ✅ DDD Tactical Patterns (適度應用)
- ✅ 結構化日誌記錄
- ✅ 完整的 XML 文件註解 (繁體中文)

## 安全性

- **推薦使用 User Secrets**：敏感資訊儲存在使用者設定檔中（`~/.microsoft/usersecrets/`），完全不會出現在專案目錄
- 日誌輸出不包含任何敏感資訊 (Token, Password)
- 建議定期輪替 API Token
- 支援 Azure DevOps、GitLab 和 BitBucket 的 Personal Access Token (PAT) 認證機制

## 授權

MIT License

## 支援與回饋

若遇到問題或有功能建議,請:
- 提交 Issue: https://github.com/yourorg/ReleaseSync/issues
- 查閱文件: https://github.com/yourorg/ReleaseSync/wiki

---

**版本**: 0.2.0
**最後更新**: 2025-11-17
