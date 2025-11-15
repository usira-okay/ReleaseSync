# Implementation Plan: Repository-Based Export Format

**Branch**: `001-repository-based-export` | **Date**: 2025-11-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-repository-based-export/spec.md`

## Summary

將現有的 Work Item-centric JSON 匯出格式改為 Repository-based 格式,以 Repository 為主體進行 Pull Request 分組,方便後續同步到 Google Sheets。此變更將完全替換現有的 `WorkItemCentricOutputDto`,並調整 `JsonFileExporter` 以支援新的資料結構。

**技術方法**: 建立新的 `RepositoryBasedOutputDto` 類別,實作 Repository 分組邏輯,並移除舊的 Work Item-centric DTO。

## Technical Context

**Language/Version**: C# / .NET 9.0 (Console), .NET 8.0 (Libraries)
**Primary Dependencies**: System.Text.Json (序列化), Microsoft.Extensions.Logging (日誌)
**Storage**: 檔案系統 (JSON 檔案輸出)
**Testing**: xUnit (單元測試), FluentAssertions (斷言), NSubstitute (Mock)
**Target Platform**: Cross-platform (.NET 9.0 Console Application)
**Project Type**: Console Application (Clean Architecture + DDD)
**Performance Goals**: 處理 100 repositories × 20 PRs (2000 筆資料) 的匯出在 5 秒內完成
**Constraints**:
  - 必須保留現有 JSON 序列化設定 (WriteIndented, CamelCase, UnsafeRelaxedJsonEscaping)
  - Repository 名稱必須從完整路徑 (如 `owner/repo`) 提取最後部分 (如 `repo`)
  - Work Item 為 null 時必須明確表示為 JSON `null`
**Scale/Scope**:
  - 預期單次匯出最多 100 repositories
  - 每個 repository 平均 20 個 PRs
  - 支援 3 個平台 (GitLab, BitBucket, Azure DevOps)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

此功能設計必須符合以下憲章原則 (詳見 `.specify/memory/constitution.md`):

- **✅ DDD 戰術模式**:
  - **Entity**: 無需新增 Entity (重用現有 `PullRequestInfo`, `WorkItemInfo`)
  - **Value Object**: 無需新增 Value Object (重用現有 `BranchName`, `WorkItemId`)
  - **Repository Pattern**: 無需變更 Repository 介面 (變更僅限於 DTO 層)
  - **Domain Layer**: 領域模型不受影響,變更僅在 Application Layer (DTO)

- **✅ CQRS 模式**:
  - 此功能屬於**查詢 (Query)** 範疇,負責資料讀取與格式轉換
  - 不涉及狀態變更,符合 CQRS 查詢端設計
  - 資料轉換邏輯位於 DTO 層,不影響領域模型

- **✅ SOLID 原則**:
  - **SRP**: `RepositoryBasedOutputDto` 單一職責為 Repository 分組與資料轉換
  - **OCP**: `IResultExporter` 介面保持不變,現有 `JsonFileExporter` 僅需調整輸入型別
  - **DIP**: `JsonFileExporter` 依賴抽象介面 `IResultExporter`,不影響相依性方向

- **✅ TDD 強制執行**:
  - **先寫測試**:
    1. 建立 `RepositoryBasedOutputDtoTests` (測試分組邏輯、Repository 名稱提取、Work Item null 處理)
    2. 更新 `JsonFileExporterTests` (測試新 DTO 的序列化)
  - **覆蓋率目標**: 核心轉換邏輯達到 90% 以上覆蓋率
  - **整合測試**: 不需要新增整合測試 (僅變更 DTO 結構,不影響外部整合)

- **✅ KISS 原則**:
  - **最簡方案**: 使用 LINQ `GroupBy` 進行 Repository 分組,無需額外框架
  - **避免過度設計**: 不引入額外的抽象層或設計模式,直接在 DTO 實作轉換邏輯
  - **YAGNI**: 不實作統計資訊計算 (如 totalPullRequests, uniqueAuthors) - 使用者未明確要求

- **✅ 例外處理**:
  - **策略**: 不新增 try-catch,沿用現有 `JsonFileExporter` 的例外處理機制
  - **失敗快速**: Repository 名稱提取使用簡單字串分割,若格式異常則保留原值 (defensive programming)

- **✅ 繁體中文**:
  - 所有 XML 註解使用繁體中文
  - 所有 inline comment 使用繁體中文
  - 測試方法名稱使用英文 (符合 C# 慣例),但測試描述使用繁體中文

- **✅ 註解規範**:
  - 所有公開類別 (`RepositoryBasedOutputDto`, `RepositoryGroupDto`, `RepositoryPullRequestDto`, `PullRequestWorkItemDto`) 必須包含完整 XML `<summary>` 註解
  - 核心方法 (`FromSyncResult`, `ExtractRepositoryName`) 必須包含 `<param>` 與 `<returns>` 說明
  - Repository 名稱提取邏輯必須包含 inline comment 說明分割規則

- **✅ 重用優先**:
  - **重用**: 現有 `JsonFileExporter` 的序列化邏輯與檔案寫入功能
  - **重用**: 現有 `SyncResultDto` 提供的 Pull Request 資料
  - **重用**: 現有 `PullRequestDto`, `WorkItemDto` 的資料結構
  - **移除**: `WorkItemCentricOutputDto` 與相關類別 (`WorkItemWithPullRequestsDto`, `SimplifiedPullRequestDto`)

- **✅ Program.cs 最小化**:
  - 無需變更 `Program.cs`,僅調整 Application Layer 的 DTO

- **✅ 分層架構**:
  - **變更範圍**: 僅限 Application Layer (`ReleaseSync.Application/DTOs/`)
  - **Domain Layer**: 不受影響
  - **Infrastructure Layer**: 不受影響
  - **Presentation Layer**: 不受影響 (CLI 仍透過 `IResultExporter` 介面使用匯出功能)

**複雜度警告**: 無違反憲章原則的設計決策。

## Project Structure

### Documentation (this feature)

```text
specs/001-repository-based-export/
├── plan.md              # This file
├── research.md          # Phase 0 output (研究結果 - 無需額外研究)
├── data-model.md        # Phase 1 output (資料模型定義)
├── quickstart.md        # Phase 1 output (快速上手指南)
└── contracts/           # Phase 1 output (JSON Schema 契約定義)
    └── repository-based-output-schema.json
```

### Source Code (repository root)

```text
src/
├── ReleaseSync.Domain/              # 領域層 (不受影響)
│   └── Models/
│       ├── PullRequestInfo.cs       # 現有,不變更
│       └── WorkItemInfo.cs          # 現有,不變更
│
├── ReleaseSync.Application/         # 應用層 (主要變更範圍)
│   ├── DTOs/
│   │   ├── SyncResultDto.cs                    # 現有,不變更
│   │   ├── WorkItemCentricOutputDto.cs         # 🗑️ 將移除
│   │   └── RepositoryBasedOutputDto.cs         # ✨ 新增
│   │       ├── RepositoryBasedOutputDto        # 頂層 DTO
│   │       ├── RepositoryGroupDto              # Repository 分組 DTO
│   │       ├── RepositoryPullRequestDto        # PR DTO (簡化版)
│   │       └── PullRequestWorkItemDto          # Work Item DTO (可為 null)
│   │
│   ├── Exporters/
│   │   ├── IResultExporter.cs                  # 現有介面,調整泛型參數
│   │   └── JsonFileExporter.cs                 # 現有,調整輸入型別
│   │
│   └── Services/
│       └── SyncOrchestrator.cs                 # 現有,調整匯出呼叫
│
├── ReleaseSync.Infrastructure/      # 基礎設施層 (不受影響)
│
├── ReleaseSync.Console/             # 呈現層 (不受影響)
│   └── Handlers/
│       └── SyncCommandHandler.cs               # 現有,不變更 (透過介面使用)
│
└── tests/
    ├── ReleaseSync.Application.UnitTests/
    │   ├── DTOs/
    │   │   ├── WorkItemCentricOutputDtoTests.cs    # 🗑️ 將移除
    │   │   └── RepositoryBasedOutputDtoTests.cs    # ✨ 新增
    │   │       ├── FromSyncResult_EmptyData_ReturnsEmptyRepositories
    │   │       ├── FromSyncResult_SingleRepository_GroupsCorrectly
    │   │       ├── FromSyncResult_MultipleRepositories_GroupsByNameAndPlatform
    │   │       ├── ExtractRepositoryName_WithSlash_ReturnsLastPart
    │   │       ├── ExtractRepositoryName_WithoutSlash_ReturnsOriginal
    │   │       └── FromSyncResult_WorkItemNull_SetsWorkItemToNull
    │   │
    │   └── Exporters/
    │       └── JsonFileExporterTests.cs            # 現有,更新測試案例
    │           ├── ExportAsync_RepositoryBasedDto_SerializesCorrectly
    │           └── ExportAsync_RepositoryBasedDto_HandlesNullWorkItem
    │
    └── ReleaseSync.Integration.Tests/
        └── (無需新增整合測試)
```

**Structure Decision**: 採用現有的 Clean Architecture 四層結構 (Domain → Application → Infrastructure → Presentation)。此功能僅影響 Application Layer 的 DTO 定義與轉換邏輯,符合單一職責原則。

## Complexity Tracking

> 無違反憲章的複雜度問題

## Phase 0: Outline & Research

### Research Tasks

此功能為資料格式轉換,使用現有技術堆疊,無需額外研究。以下為已知技術決策:

1. **Repository 分組邏輯**:
   - **決策**: 使用 LINQ `GroupBy` 依據 `(RepositoryName, Platform)` 組合鍵進行分組
   - **理由**: 符合 KISS 原則,避免引入額外框架,LINQ 效能足夠應付預期規模
   - **替代方案**: Dictionary 手動分組 - 複雜度較高,無明顯效能優勢

2. **Repository 名稱提取規則**:
   - **決策**: 使用 `string.Split('/')` 並取最後部分 (`parts[^1]`)
   - **理由**: 簡單直觀,符合使用者需求 (`owner/repo` → `repo`)
   - **邊界情況**: 若無 `/` 則保留原始名稱 (defensive programming)

3. **Work Item null 處理**:
   - **決策**: 當 `AssociatedWorkItem` 為 null 時,DTO 屬性設為 `null` (而非空物件)
   - **理由**: 符合使用者明確要求「當無法抓到 azure devops 資料時 workItem 請給 null」
   - **JSON 輸出**: `System.Text.Json` 預設將 null 序列化為 JSON `null`

4. **JSON Schema 定義**:
   - **決策**: 提供 JSON Schema 文件作為契約定義
   - **理由**: 方便使用者理解資料結構,協助 Google Sheets 整合開發
   - **工具**: 手動撰寫 JSON Schema (Draft 2020-12),無需額外工具

### Output

`research.md` 內容將記錄上述決策與理由 (Phase 0 完成後產生)。

## Phase 1: Design & Contracts

### 1.1 Data Model Design

**Output**: `data-model.md`

#### Entity: RepositoryBasedOutputDto

**Purpose**: 頂層輸出 DTO,以 Repository 為主體組織 Pull Request 資料

**Fields**:
- `StartDate` (DateTime, required): 查詢開始日期
- `EndDate` (DateTime, required): 查詢結束日期
- `Repositories` (List<RepositoryGroupDto>, required): Repository 分組清單

**Business Rules**:
- `StartDate` 必須早於或等於 `EndDate`
- `Repositories` 不得為 null (但可為空陣列)

**Relationships**:
- 包含多個 `RepositoryGroupDto` (1:N)

---

#### Entity: RepositoryGroupDto

**Purpose**: 代表單一 Repository 及其關聯的 Pull Requests

**Fields**:
- `RepositoryName` (string, required): Repository 簡短名稱 (已提取最後部分)
- `Platform` (string, required): 平台名稱 (GitLab / BitBucket / AzureDevOps)
- `PullRequests` (List<RepositoryPullRequestDto>, required): PR 清單

**Business Rules**:
- `RepositoryName` 不得為空字串
- `Platform` 必須為有效平台名稱
- `PullRequests` 不得為 null (但可為空陣列)

**Relationships**:
- 包含多個 `RepositoryPullRequestDto` (1:N)

---

#### Entity: RepositoryPullRequestDto

**Purpose**: 簡化的 Pull Request DTO,包含 Work Item 關聯

**Fields**:
- `WorkItem` (PullRequestWorkItemDto?, nullable): 關聯的 Work Item (可為 null)
- `PullRequestTitle` (string, required): PR 標題
- `SourceBranch` (string, required): 來源分支
- `TargetBranch` (string, required): 目標分支
- `MergedAt` (DateTime?, nullable): 合併時間 (UTC)
- `AuthorUserId` (string?, nullable): 作者 ID
- `AuthorDisplayName` (string?, nullable): 作者顯示名稱
- `PullRequestUrl` (string?, nullable): PR URL

**Business Rules**:
- `PullRequestTitle` 不得為空字串
- `SourceBranch` 與 `TargetBranch` 不得為空字串
- 當無關聯 Work Item 時,`WorkItem` 必須為 `null`

**Relationships**:
- 可選地關聯 `PullRequestWorkItemDto` (0..1:1)

---

#### Entity: PullRequestWorkItemDto

**Purpose**: Work Item 基本資訊 (用於 PR 關聯)

**Fields**:
- `WorkItemId` (int, required): Work Item ID
- `WorkItemTitle` (string, required): Work Item 標題
- `WorkItemTeam` (string?, nullable): 所屬團隊
- `WorkItemType` (string, required): Work Item 類型
- `WorkItemUrl` (string?, nullable): Work Item URL

**Business Rules**:
- `WorkItemId` 必須為正整數
- `WorkItemTitle` 不得為空字串
- `WorkItemType` 不得為空字串

---

### 1.2 Contracts

**Output**: `contracts/repository-based-output-schema.json`

JSON Schema 定義將包含:
- 頂層結構 (`startDate`, `endDate`, `repositories`)
- Repository 物件結構
- Pull Request 物件結構
- Work Item 物件結構 (可為 null)
- 必填欄位標註
- 型別定義 (string, number, null, array)

範例結構 (依據使用者提供的格式):

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["startDate", "endDate", "repositories"],
  "properties": {
    "startDate": {
      "type": "string",
      "format": "date-time"
    },
    "endDate": {
      "type": "string",
      "format": "date-time"
    },
    "repositories": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/RepositoryGroup"
      }
    }
  },
  "$defs": {
    "RepositoryGroup": {
      "type": "object",
      "required": ["repositoryName", "platform", "pullRequests"],
      "properties": {
        "repositoryName": { "type": "string" },
        "platform": { "type": "string" },
        "pullRequests": {
          "type": "array",
          "items": { "$ref": "#/$defs/PullRequest" }
        }
      }
    },
    "PullRequest": {
      "type": "object",
      "required": ["pullRequestTitle", "sourceBranch", "targetBranch"],
      "properties": {
        "workItem": {
          "oneOf": [
            { "$ref": "#/$defs/WorkItem" },
            { "type": "null" }
          ]
        },
        "pullRequestTitle": { "type": "string" },
        "sourceBranch": { "type": "string" },
        "targetBranch": { "type": "string" },
        "mergedAt": { "type": "string", "format": "date-time" },
        "authorUserId": { "type": ["string", "null"] },
        "authorDisplayName": { "type": ["string", "null"] },
        "pullRequestUrl": { "type": ["string", "null"] }
      }
    },
    "WorkItem": {
      "type": "object",
      "required": ["workItemId", "workItemTitle", "workItemType"],
      "properties": {
        "workItemId": { "type": "integer" },
        "workItemTitle": { "type": "string" },
        "workItemTeam": { "type": ["string", "null"] },
        "workItemType": { "type": "string" },
        "workItemUrl": { "type": ["string", "null"] }
      }
    }
  }
}
```

### 1.3 Quickstart Guide

**Output**: `quickstart.md`

快速上手指南將包含:
1. **功能概述**: 說明 Repository-based 匯出格式的目的與優勢
2. **使用範例**: 如何執行 sync 命令並產生新格式 JSON
3. **輸出範例**: 完整的 JSON 範例檔案
4. **Google Sheets 整合**: 如何匯入 JSON 到 Google Sheets 的基本步驟
5. **欄位說明**: 各欄位的意義與資料來源

### 1.4 Agent Context Update

執行以下命令更新 agent 特定檔案:

```bash
.specify/scripts/bash/update-agent-context.sh claude
```

此命令將在 `.specify/memory/` 目錄更新 Claude 特定的 context 檔案,記錄此功能使用的技術與決策。

## Phase 2: Task Generation

**注意**: Phase 2 (Task Generation) 由 `/speckit.tasks` 命令執行,**不在** `/speckit.plan` 範圍內。

`/speckit.plan` 命令在完成 Phase 1 後結束,輸出以下文件:
- ✅ `plan.md` (本檔案)
- ✅ `research.md` (研究結果)
- ✅ `data-model.md` (資料模型)
- ✅ `quickstart.md` (快速上手)
- ✅ `contracts/repository-based-output-schema.json` (JSON Schema)

後續請執行 `/speckit.tasks` 產生具體實作任務清單。

## Summary of Changes

### 新增檔案
- `src/ReleaseSync.Application/DTOs/RepositoryBasedOutputDto.cs`
- `src/tests/ReleaseSync.Application.UnitTests/DTOs/RepositoryBasedOutputDtoTests.cs`
- `specs/001-repository-based-export/research.md`
- `specs/001-repository-based-export/data-model.md`
- `specs/001-repository-based-export/quickstart.md`
- `specs/001-repository-based-export/contracts/repository-based-output-schema.json`

### 修改檔案
- `src/ReleaseSync.Application/Exporters/IResultExporter.cs` (調整泛型參數)
- `src/ReleaseSync.Application/Exporters/JsonFileExporter.cs` (調整輸入型別)
- `src/ReleaseSync.Application/Services/SyncOrchestrator.cs` (使用新 DTO)
- `src/tests/ReleaseSync.Application.UnitTests/Exporters/JsonFileExporterTests.cs` (更新測試)

### 移除檔案
- `src/ReleaseSync.Application/DTOs/WorkItemCentricOutputDto.cs`
- `src/tests/ReleaseSync.Application.UnitTests/DTOs/WorkItemCentricOutputDtoTests.cs` (若存在)

### 影響範圍
- **Domain Layer**: 無影響
- **Application Layer**: DTO 定義變更,匯出邏輯調整
- **Infrastructure Layer**: 無影響
- **Presentation Layer**: 無影響 (透過介面使用)

### 破壞性變更
- ⚠️ **JSON 輸出格式完全變更**: 現有依賴 Work Item-centric 格式的下游系統需要調整
- ⚠️ **無向後相容性**: 舊格式完全移除,無法同時支援兩種格式

### 遷移建議
- 若需要向後相容,建議使用者在更新前備份舊版本產生的 JSON 檔案
- 提供遷移腳本或工具將舊格式轉換為新格式 (Out of Scope,可作為後續功能)
