# Data Model: Console 工具基礎架構

**Feature**: ReleaseSync Console 工具基礎架構
**Date**: 2025-10-18
**Status**: Foundation Phase - No Domain Models Required

---

## 概述

本階段 (001-console-tool-foundation) 專注於建立 Console 應用程式的基礎架構,**不涉及實際的領域模型或實體定義**。根據使用者需求與 spec.md:

> 請先定義好 program.cs 的規範與基本架構,先不需要實作任何取得資料的邏輯。
> 還不需要處理任何的 model 或 entity,不是本次重點。

因此,本文件記錄的是:
1. 本階段無需定義的領域模型
2. 未來階段需要設計的領域模型規劃
3. 基礎設定物件 (Configuration Objects)

---

## 本階段: 基礎設定物件

### ApplicationSettings (appsettings.json)

**用途**: 儲存應用程式的一般設定,非敏感資料

```csharp
/// <summary>
/// 應用程式設定
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// 日誌設定區段
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Repository 設定區段 (保留空結構,未來階段使用)
    /// </summary>
    public RepositorySettings Repository { get; set; } = new();
}

/// <summary>
/// 日誌相關設定
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// 最低日誌等級 (預設: Information)
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// 是否輸出到檔案
    /// </summary>
    public bool EnableFileLogging { get; set; } = false;

    /// <summary>
    /// 日誌檔案路徑 (預設: logs/)
    /// </summary>
    public string LogFilePath { get; set; } = "logs/";
}

/// <summary>
/// Repository 相關設定 (保留空結構,未來階段使用)
/// </summary>
public class RepositorySettings
{
    // 本階段保留空結構
    // 未來階段將包含:
    // - 預設平台 (GitHub/GitLab/Azure DevOps)
    // - API Timeout 設定
    // - Retry Policy 設定
    // - Rate Limiting 設定
}
```

**對應的 appsettings.json**:

```json
{
  "Logging": {
    "MinimumLevel": "Information",
    "EnableFileLogging": false,
    "LogFilePath": "logs/"
  },
  "Repository": {
    // 保留空結構,未來階段使用
  }
}
```

### SecureSettings (secure.json)

**用途**: 儲存敏感資料,應加入 `.gitignore`,不提交至版控系統

```csharp
/// <summary>
/// 敏感資料設定 (不應提交至版控)
/// </summary>
public class SecureSettings
{
    /// <summary>
    /// GitHub 個人存取權杖 (保留空結構,未來階段使用)
    /// </summary>
    public string? GitHubToken { get; set; }

    /// <summary>
    /// GitLab 個人存取權杖 (保留空結構,未來階段使用)
    /// </summary>
    public string? GitLabToken { get; set; }

    /// <summary>
    /// Azure DevOps 個人存取權杖 (保留空結構,未來階段使用)
    /// </summary>
    public string? AzureDevOpsToken { get; set; }
}
```

**對應的 secure.json** (範例,實際內容不提交):

```json
{
  "GitHubToken": "",
  "GitLabToken": "",
  "AzureDevOpsToken": ""
}
```

**secure.json.example** (提交至版控作為範本):

```json
{
  "GitHubToken": "YOUR_GITHUB_TOKEN_HERE",
  "GitLabToken": "YOUR_GITLAB_TOKEN_HERE",
  "AzureDevOpsToken": "YOUR_AZURE_DEVOPS_TOKEN_HERE"
}
```

---

## 未來階段: 領域模型規劃

以下領域模型將在未來階段設計與實作,本階段不處理:

### 1. Pull Request / Merge Request Entity

**用途**: 表示從版控平台抓取的 PR/MR 資訊

**預期屬性** (未來設計):
- `Id`: 識別碼 (Guid)
- `PlatformType`: 平台類型 (GitHub/GitLab/Azure DevOps)
- `Number`: PR/MR 編號
- `Title`: 標題
- `Description`: 描述
- `State`: 狀態 (Open/Closed/Merged)
- `Author`: 作者資訊
- `CreatedAt`: 建立時間
- `UpdatedAt`: 更新時間
- `MergedAt`: 合併時間
- `SourceBranch`: 來源分支
- `TargetBranch`: 目標分支
- `Commits`: 提交歷史集合
- `ChangedFiles`: 變更檔案集合

**DDD 模式考量**:
- **Entity** (有唯一識別性,狀態會隨時間變化)
- 考慮作為 **Aggregate Root** (PR/MR 包含 Commits 與 ChangedFiles)

### 2. Commit Entity

**用途**: 表示 PR/MR 中的提交記錄

**預期屬性**:
- `Id`: 識別碼
- `Sha`: Commit SHA
- `Message`: 提交訊息
- `Author`: 作者
- `CommittedAt`: 提交時間

**DDD 模式考量**:
- **Entity** (屬於 Pull Request Aggregate 的一部分)

### 3. ChangedFile Value Object

**用途**: 表示 PR/MR 中的檔案變更

**預期屬性**:
- `FilePath`: 檔案路徑
- `ChangeType`: 變更類型 (Added/Modified/Deleted)
- `AddedLines`: 新增行數
- `DeletedLines`: 刪除行數

**DDD 模式考量**:
- **Value Object** (無唯一識別性,由屬性值定義相等性)

### 4. Platform Configuration Value Object

**用途**: 表示平台相關設定 (API endpoint, token 等)

**預期屬性**:
- `PlatformType`: 平台類型
- `ApiBaseUrl`: API 基底網址
- `Token`: 存取權杖 (加密儲存)
- `Timeout`: 請求逾時時間

**DDD 模式考量**:
- **Value Object** (不可變,由屬性值定義相等性)

### 5. Repository Information Value Object

**用途**: 表示 Repository 資訊

**預期屬性**:
- `Owner`: 擁有者
- `Name`: Repository 名稱
- `FullName`: 完整名稱 (owner/repo)

**DDD 模式考量**:
- **Value Object** (不可變)

---

## 領域服務規劃 (未來階段)

### IPullRequestFetchingService

**職責**: 從版控平台抓取 PR/MR 資訊

**方法簽章**:
```csharp
Task<PullRequest> FetchPullRequestAsync(
    PlatformType platform,
    RepositoryInformation repository,
    int pullRequestNumber,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<PullRequest>> FetchPullRequestsAsync(
    PlatformType platform,
    RepositoryInformation repository,
    PullRequestFilter filter,
    CancellationToken cancellationToken = default);
```

### ISyncService

**職責**: 同步 PR/MR 資訊到目標系統 (本地檔案、資料庫等)

**方法簽章**:
```csharp
Task SyncPullRequestAsync(
    PullRequest pullRequest,
    SyncOptions options,
    CancellationToken cancellationToken = default);
```

---

## 資料驗證規則 (未來階段)

### Pull Request Validation

- **Number**: 必須 > 0
- **Title**: 不得為空,長度 1-255 字元
- **State**: 必須是有效的 PullRequestState 列舉值
- **SourceBranch**: 不得為空
- **TargetBranch**: 不得為空
- **CreatedAt**: 不得晚於 UpdatedAt
- **MergedAt**: 若非 null,則 State 必須為 Merged

### Repository Information Validation

- **Owner**: 不得為空,不得包含特殊字元
- **Name**: 不得為空,不得包含特殊字元
- **FullName**: 必須符合格式 `{Owner}/{Name}`

---

## 狀態轉換 (未來階段)

### Pull Request State Machine

```
Open → [Merge Event] → Merged
Open → [Close Event] → Closed
Closed → [Reopen Event] → Open
```

**業務規則**:
- 只有 Open 狀態的 PR 可以被合併
- Merged 狀態的 PR 不可再變更狀態
- Closed 狀態的 PR 可以被重新開啟 (取決於平台支援)

---

## 資料持久化策略 (未來階段)

### Repository Pattern

將實作以下 Repository 介面:

```csharp
public interface IPullRequestRepository
{
    Task<PullRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PullRequest?> GetByNumberAsync(string owner, string repo, int number, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PullRequest>> GetByFilterAsync(PullRequestFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);
    Task UpdateAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 儲存選項考量

1. **本地 JSON 檔案**: 適合簡單場景,易於查看與偵錯
2. **SQLite**: 適合單機應用程式,支援 SQL 查詢
3. **PostgreSQL/SQL Server**: 適合多使用者環境,需要複雜查詢
4. **NoSQL (MongoDB)**: 適合非結構化資料,彈性 schema

**本專案初期建議**: 本地 JSON 檔案,未來視需求遷移至 SQLite 或關聯式資料庫

---

## Bounded Context 規劃 (未來階段)

根據 DDD 原則,ReleaseSync 可能包含以下 Bounded Contexts:

### 1. Version Control Integration Context

**職責**: 與版控平台整合,抓取 PR/MR 資訊

**Entities**:
- PullRequest
- Commit
- ChangedFile

**Services**:
- IPullRequestFetchingService
- IGitHubApiClient
- IGitLabApiClient

### 2. Data Sync Context

**職責**: 同步資料到目標系統

**Entities**:
- SyncJob
- SyncHistory

**Services**:
- ISyncService
- IExportService

### 3. Configuration Context

**職責**: 管理應用程式設定與平台認證

**Value Objects**:
- PlatformConfiguration
- RepositoryInformation

**Services**:
- IConfigurationService
- ISecretManager

---

## 總結

### 本階段 (001-console-tool-foundation)

- ✅ 定義基礎設定物件 (ApplicationSettings, SecureSettings)
- ✅ 規劃設定檔結構 (appsettings.json, secure.json)
- ✅ 記錄未來領域模型設計方向
- ❌ **不實作** 任何領域模型或實體

### 下一階段需要

- 設計完整的領域模型 (PullRequest, Commit, ChangedFile 等)
- 定義 Repository 介面與實作
- 實作資料驗證與業務規則
- 決定資料持久化策略
- 實作與版控平台的整合邏輯

---

**Document Version**: 1.0
**Last Updated**: 2025-10-18
**Next Review**: 下一個功能階段開始時
