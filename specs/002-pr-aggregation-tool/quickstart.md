# Quick Start Guide: ReleaseSync

**Feature**: 002-pr-aggregation-tool
**Date**: 2025-10-18
**Version**: 1.0

本指南幫助您快速設定並執行 ReleaseSync 工具,從 GitLab、BitBucket 抓取 PR/MR 變更資訊,並可選擇性地關聯 Azure DevOps Work Item。

---

## 前置需求

### 系統需求
- **.NET 8.0 SDK 或 Runtime** ([下載連結](https://dotnet.microsoft.com/download/dotnet/8.0))
- **網路連線**: 能存取 GitLab, BitBucket, Azure DevOps API 端點
- **作業系統**: Windows, Linux, macOS (跨平台支援)

### API 存取權限

在開始前,請確保您已取得以下 API 存取憑證:

#### GitLab Personal Access Token
1. 登入 GitLab: https://gitlab.com (或您的 GitLab 實例)
2. 點選右上角使用者圖示 → **Preferences** → **Access Tokens**
3. 建立新 Token:
   - **Token name**: ReleaseSync API Access
   - **Scopes**: 勾選 `api`, `read_repository`
   - **Expiration date**: 設定有效期限
4. 點選 **Create personal access token** 並**立即複製** Token (僅顯示一次)

#### BitBucket App Password (Cloud) 或 Access Token (Data Center)
**BitBucket Cloud**:
1. 登入 BitBucket: https://bitbucket.org
2. 點選右上角使用者圖示 → **Personal settings** → **App passwords**
3. 建立新 App Password:
   - **Label**: ReleaseSync API Access
   - **Permissions**: 勾選 `Repositories: Read`, `Pull requests: Read`
4. 點選 **Create** 並**立即複製** Password

**BitBucket Data Center**:
- 請使用您的 Access Token 或參考管理員取得 API 存取權限

#### Azure DevOps Personal Access Token
1. 登入 Azure DevOps: https://dev.azure.com/{organization}
2. 點選右上角使用者圖示 → **Personal access tokens**
3. 建立新 Token:
   - **Name**: ReleaseSync API Access
   - **Organization**: 選擇組織
   - **Scopes**: 選擇 **Work Items: Read**
4. 點選 **Create** 並**立即複製** Token

---

## 安裝步驟

### 方式 1: 下載發行版本 (建議)

1. 前往 [Releases](https://github.com/yourorg/ReleaseSync/releases) 頁面
2. 下載最新版本的壓縮檔:
   - Windows: `ReleaseSync-win-x64.zip`
   - Linux: `ReleaseSync-linux-x64.tar.gz`
   - macOS: `ReleaseSync-osx-x64.tar.gz`
3. 解壓縮至任意目錄

### 方式 2: 從原始碼建置

```bash
# Clone 專案
git clone https://github.com/yourorg/ReleaseSync.git
cd ReleaseSync

# 建置專案
dotnet build --configuration Release

# (選填) 發行為獨立執行檔
dotnet publish -c Release -r win-x64 --self-contained
```

---

## 組態設定

### 步驟 1: 建立組態檔

1. 複製組態範本檔案:

**Linux / macOS**:
```bash
cd /path/to/ReleaseSync
cp src/ReleaseSync.Console/appsettings.example.json src/ReleaseSync.Console/appsettings.json
cp src/ReleaseSync.Console/appsettings.secure.example.json src/ReleaseSync.Console/appsettings.secure.json
```

**Windows**:
```powershell
cd C:\path\to\ReleaseSync
copy src\ReleaseSync.Console\appsettings.example.json src\ReleaseSync.Console\appsettings.json
copy src\ReleaseSync.Console\appsettings.secure.example.json src\ReleaseSync.Console\appsettings.secure.json
```

### 步驟 2: 設定 appsettings.json

編輯 `appsettings.json`,填入您的專案資訊:

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
    ],
    "ParsingBehavior": {
      "OnParseFailure": "LogWarningAndContinue",
      "StopOnFirstMatch": true
    }
  }
}
```

**關鍵設定說明**:

- **GitLab / BitBucket Projects**: 列出需要同步的專案清單
  - `ProjectPath` (GitLab): 格式為 `group/project`
  - `WorkspaceAndRepo` (BitBucket): 格式為 `workspace/repository`
  - `TargetBranches`: 目標分支清單,空陣列表示所有分支

- **AzureDevOps WorkItemIdPatterns**: 定義如何從 Branch 名稱解析 Work Item ID
  - `Regex`: 正規表示式,需包含 capture group 擷取數字
  - `IgnoreCase`: 是否忽略大小寫
  - `CaptureGroup`: 擷取 Work Item ID 的 group 索引 (通常為 1)

### 步驟 3: 設定 appsettings.secure.json

編輯 `appsettings.secure.json`,填入您的 API Token:

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

**⚠️ 安全提醒**:
- `appsettings.secure.json` 已在 `.gitignore` 中,不會被提交至版本控制
- 請妥善保管此檔案,避免洩漏 API Token
- 建議定期輪替 Token,降低安全風險

---

## 執行工具

### 基本執行範例

#### 範例 1: 抓取 GitLab 過去 30 天的 MR 資訊

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab
```

**輸出範例**:
```
[2025-01-15 14:30:00] [INFO] 開始同步 PR/MR 資訊...
[2025-01-15 14:30:00] [INFO] 時間範圍: 2025-01-01 to 2025-01-31
[2025-01-15 14:30:00] [INFO] 啟用平台: GitLab
[2025-01-15 14:30:01] [INFO] [GitLab] 正在抓取專案: mygroup/backend-api
[2025-01-15 14:30:03] [INFO] [GitLab] 找到 12 筆 Merge Requests
[2025-01-15 14:30:03] [INFO] 同步完成: 1/1 平台成功, 共抓取 12 筆 PR/MR (0 筆關聯到 Work Item), 耗時 3.21 秒
```

#### 範例 2: 同時抓取 GitLab 與 BitBucket 並匯出 JSON

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab \
  --enable-bitbucket \
  --output-file ./output/sync-result.json
```

**輸出檔案** (`./output/sync-result.json`):
```json
{
  "SyncMetadata": {
    "SyncStartedAt": "2025-01-15T06:30:00Z",
    "SyncCompletedAt": "2025-01-15T06:30:05Z",
    "DateRange": {
      "StartDate": "2025-01-01T00:00:00Z",
      "EndDate": "2025-01-31T23:59:59Z"
    },
    "IsFullySuccessful": true,
    "TotalPullRequestCount": 23,
    "LinkedWorkItemCount": 0
  },
  "PlatformStatuses": [
    {
      "PlatformName": "GitLab",
      "IsSuccess": true,
      "PullRequestCount": 12,
      "ErrorMessage": null,
      "ElapsedMilliseconds": 3210
    },
    {
      "PlatformName": "BitBucket",
      "IsSuccess": true,
      "PullRequestCount": 11,
      "ErrorMessage": null,
      "ElapsedMilliseconds": 2156
    }
  ],
  "PullRequests": [
    {
      "Platform": "GitLab",
      "Id": "12345",
      "Number": 42,
      "Title": "Add user authentication feature",
      "Description": "Implements OAuth 2.0 authentication",
      "SourceBranch": "feature/vsts1234-auth",
      "TargetBranch": "main",
      "CreatedAt": "2025-01-10T14:30:00Z",
      "MergedAt": "2025-01-12T09:15:00Z",
      "State": "Merged",
      "AuthorUsername": "john.doe",
      "AuthorDisplayName": "John Doe",
      "RepositoryName": "mygroup/backend-api",
      "Url": "https://gitlab.com/mygroup/backend-api/-/merge_requests/42",
      "AssociatedWorkItem": null
    }
    // ... 其他 PR/MR
  ]
}
```

#### 範例 3: 啟用 Azure DevOps Work Item 整合

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab \
  --enable-azure-devops \
  --output-file ./output/full-sync.json \
  --verbose
```

**說明**:
- `--enable-azure-devops`: 啟用 Work Item 整合,會從 Branch 名稱解析 Work Item ID
- `--verbose`: 啟用詳細日誌輸出 (Debug 等級),可查看解析過程

**輸出範例** (包含 Work Item):
```
[2025-01-15 14:30:00] [INFO] 開始同步 PR/MR 資訊...
[2025-01-15 14:30:01] [DEBUG] [GitLab] 找到 MR #42: feature/vsts1234-auth
[2025-01-15 14:30:01] [DEBUG] [Parser] 成功解析 Work Item ID: 1234 from Branch: feature/vsts1234-auth using pattern: VSTS Pattern
[2025-01-15 14:30:02] [INFO] [AzureDevOps] 抓取 Work Item: 1234
[2025-01-15 14:30:02] [INFO] [AzureDevOps] Work Item #1234: Implement user authentication (User Story)
[2025-01-15 14:30:02] [INFO] [AzureDevOps] Parent Work Item #100: User Management Epic (Epic)
[2025-01-15 14:30:05] [INFO] 同步完成: 2/2 平台成功, 共抓取 12 筆 PR/MR (8 筆關聯到 Work Item), 耗時 5.12 秒
```

---

## 命令列參數完整說明

| 參數 | 別名 | 類型 | 必填 | 預設值 | 說明 |
|------|------|------|------|--------|------|
| `--start-date` | `-s` | DateTime | 是 | - | 查詢起始日期 (包含) |
| `--end-date` | `-e` | DateTime | 是 | - | 查詢結束日期 (包含) |
| `--enable-gitlab` | `--gitlab` | bool | 否 | false | 啟用 GitLab 平台 |
| `--enable-bitbucket` | `--bitbucket` | bool | 否 | false | 啟用 BitBucket 平台 |
| `--enable-azure-devops` | `--azure`, `--ado` | bool | 否 | false | 啟用 Azure DevOps Work Item 整合 |
| `--output-file` | `-o` | string | 否 | null | JSON 匯出檔案路徑 |
| `--force` | `-f` | bool | 否 | false | 強制覆蓋已存在的輸出檔案 |
| `--verbose` | `-v` | bool | 否 | false | 啟用詳細日誌輸出 |

**詳細說明請參閱**: [Command Line Parameters Contract](./contracts/command-line-parameters.md)

---

## 常見問題

### Q1: 執行時出現「缺少 appsettings.json」錯誤

**答**: 請確認 `appsettings.json` 位於執行檔同目錄下。若從原始碼執行,請確認檔案位於 `src/ReleaseSync.Console/` 目錄。

**解決方式**:
```bash
# 檢查檔案是否存在
ls src/ReleaseSync.Console/appsettings.json

# 若不存在,複製範本檔案
cp src/ReleaseSync.Console/appsettings.example.json src/ReleaseSync.Console/appsettings.json
```

### Q2: API 認證失敗 (401 Unauthorized)

**答**: 請檢查 `appsettings.secure.json` 中的 Token 是否正確,並確認 Token 權限是否足夠。

**檢查步驟**:
1. 確認 Token 未過期
2. GitLab Token 需包含 `api`, `read_repository` scope
3. BitBucket App Password 需包含 `Repositories: Read`, `Pull requests: Read` 權限
4. Azure DevOps Token 需包含 `Work Items: Read` scope

### Q3: 無法解析 Work Item ID

**答**: 請檢查 `appsettings.json` 中的 `WorkItemIdPatterns` 設定是否符合您的 Branch 命名規則。

**範例**:

若 Branch 名稱為 `feature/12345-user-auth`,Regex 應為:
```json
{
  "Regex": "feature/(\\d+)-",
  "IgnoreCase": false,
  "CaptureGroup": 1
}
```

若 Branch 名稱為 `vsts1234` (不區分大小寫),Regex 應為:
```json
{
  "Regex": "vsts(\\d+)",
  "IgnoreCase": true,
  "CaptureGroup": 1
}
```

### Q4: 部分平台失敗,如何查看詳細錯誤?

**答**: 使用 `--verbose` 參數啟用詳細日誌輸出:

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab \
  --verbose
```

錯誤訊息也會記錄在輸出 JSON 的 `PlatformStatuses[].ErrorMessage` 欄位中。

### Q5: 如何只抓取特定分支的 PR/MR?

**答**: 在 `appsettings.json` 的 `Projects` 設定中指定 `TargetBranches`:

```json
{
  "GitLab": {
    "Projects": [
      {
        "ProjectPath": "mygroup/backend-api",
        "TargetBranches": ["main", "develop"]  // 只抓取合併至 main 或 develop 的 MR
      }
    ]
  }
}
```

---

## 進階設定

### 自訂 Branch 名稱解析規則

若您的團隊有多種 Branch 命名規則,可設定多個 Regex Pattern:

```json
{
  "AzureDevOps": {
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
      },
      {
        "Name": "Bug Pattern",
        "Regex": "bug_(\\d+)",
        "IgnoreCase": true,
        "CaptureGroup": 1
      }
    ],
    "ParsingBehavior": {
      "OnParseFailure": "LogWarningAndContinue",  // 無法解析時記錄警告但繼續
      "StopOnFirstMatch": true  // 找到第一個匹配後停止
    }
  }
}
```

### 使用者對應設定

若您的團隊成員在 GitLab 與 BitBucket 使用不同帳號,可設定 User Mapping:

```json
{
  "UserMapping": [
    {
      "GitLabUserId": "john.doe",
      "BitBucketUserId": "jdoe",
      "DisplayName": "John Doe"
    },
    {
      "GitLabUserId": "jane.smith",
      "BitBucketUserId": "jsmith",
      "DisplayName": "Jane Smith"
    }
  ]
}
```

**用途**: 未來可用於跨平台的使用者統計與報表生成。

---

## 下一步

- 查看 [Data Model](./data-model.md) 了解資料結構
- 查看 [API Contracts](./contracts/) 了解詳細的設定與輸出格式
- 閱讀 [Implementation Plan](./plan.md) 了解技術架構

---

## 支援與回饋

若遇到問題或有功能建議,請:
- 提交 Issue: https://github.com/yourorg/ReleaseSync/issues
- 查閱文件: https://github.com/yourorg/ReleaseSync/wiki

---

**版本**: 1.0
**最後更新**: 2025-10-18
