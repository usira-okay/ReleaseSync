# ReleaseSync - PR/MR 變更資訊聚合工具

從 GitLab、BitBucket 和 Azure DevOps 抓取 Pull Request / Merge Request 變更資訊,並匯出為結構化 JSON 格式。

## 功能特色

- 🔄 支援多平台: GitLab, BitBucket Cloud
- 🔗 Azure DevOps Work Item 整合
- 📊 JSON 格式匯出
- 🛡️ 部分失敗容錯處理
- 📝 詳細的日誌記錄
- ⚡ 並行查詢提升效能
- 🔍 Verbose 模式支援 Debug 等級日誌

## 快速開始

詳細說明請參考: [Quick Start Guide](specs/002-pr-aggregation-tool/quickstart.md)

### 1. 安裝

```bash
git clone https://github.com/yourorg/ReleaseSync.git
cd ReleaseSync
dotnet build
```

### 2. 設定

```bash
cd src/ReleaseSync.Console
cp appsettings.json.example appsettings.json
cp appsettings.secure.json.example appsettings.secure.json
# 編輯 appsettings.secure.json,填入 API Tokens
```

### 3. 執行

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab \
  --enable-bitbucket \
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
  -o ./output/sync-result.json
```

### Azure DevOps Work Item 整合

啟用 Work Item 整合,從 Branch 名稱解析並抓取 Work Item 資訊:

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --azdo \
  -o ./output/full-sync.json \
  --verbose
```

## 命令列參數

| 參數 | 別名 | 說明 |
|------|------|------|
| `--start-date` | `-s` | 查詢起始日期 (必填) |
| `--end-date` | `-e` | 查詢結束日期 (必填) |
| `--enable-gitlab` | `--gitlab` | 啟用 GitLab 平台 |
| `--enable-bitbucket` | `--bitbucket` | 啟用 BitBucket 平台 |
| `--enable-azure-devops` | `--azdo` | 啟用 Azure DevOps Work Item 整合 |
| `--output-file` | `-o` | JSON 匯出檔案路徑 |
| `--force` | `-f` | 強制覆蓋已存在的輸出檔案 |
| `--verbose` | `-v` | 啟用詳細日誌輸出 (Debug 等級) |

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
      }
    ]
  }
}
```

### appsettings.secure.json

設定 API Token (不會被提交至版本控制):

```json
{
  "GitLab": {
    "PersonalAccessToken": "glpat-xxxxxxxxxxxxxxxxxxxx"
  },
  "BitBucket": {
    "Email": "your.email@example.com",
    "AccessToken": "ATBB..."
  },
  "AzureDevOps": {
    "PersonalAccessToken": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

## 錯誤處理

工具提供友善的錯誤訊息,協助快速診斷問題:

### 認證失敗

```
❌ 認證失敗!
請檢查以下項目:
  1. 確認 appsettings.secure.json 中的 Token 正確
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
  - appsettings.json
  - appsettings.secure.json (可從 appsettings.secure.example.json 複製)
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
│   ├── Services/                # 應用服務 (SyncOrchestrator)
│   ├── DTOs/                    # 資料傳輸物件
│   └── Exporters/               # 匯出器 (JSON)
├── ReleaseSync.Infrastructure/  # 基礎設施層
│   ├── Platforms/               # 平台整合 (GitLab, BitBucket, Azure DevOps)
│   ├── Configuration/           # 組態模型
│   └── Parsers/                 # Work Item ID 解析器
└── ReleaseSync.Console/         # 命令列介面
    ├── Commands/                # 命令定義
    └── Handlers/                # 命令處理器
```

## 文件

- [Quick Start Guide](specs/002-pr-aggregation-tool/quickstart.md) - 快速開始指南
- [Implementation Plan](specs/002-pr-aggregation-tool/plan.md) - 技術架構與設計決策
- [Data Model](specs/002-pr-aggregation-tool/data-model.md) - 資料模型詳細說明
- [Tasks](specs/002-pr-aggregation-tool/tasks.md) - 任務清單與執行順序
- [Research](specs/002-pr-aggregation-tool/research.md) - 技術調查與決策記錄

## 開發指南

### 前置需求

- .NET 8.0 SDK
- 存取 GitLab / BitBucket / Azure DevOps API 的權限

### 建置

```bash
dotnet build
```

### 測試

```bash
dotnet test
```

### 程式碼品質

專案遵循以下原則:

- ✅ SOLID 原則
- ✅ KISS 原則 (Keep It Simple, Stupid)
- ✅ DDD Tactical Patterns (適度應用)
- ✅ 結構化日誌記錄
- ✅ 完整的 XML 文件註解 (繁體中文)

## 安全性

- API Token 儲存於 `appsettings.secure.json`,已在 `.gitignore` 中排除
- 日誌輸出不包含任何敏感資訊 (Token, Password)
- 建議定期輪替 API Token

## 授權

MIT License

## 支援與回饋

若遇到問題或有功能建議,請:
- 提交 Issue: https://github.com/yourorg/ReleaseSync/issues
- 查閱文件: https://github.com/yourorg/ReleaseSync/wiki

---

**版本**: 1.0.0
**最後更新**: 2025-10-18
