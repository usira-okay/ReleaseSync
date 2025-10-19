# Implementation Plan: PR/MR 變更資訊聚合工具

**Branch**: `002-pr-aggregation-tool` | **Date**: 2025-10-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-pr-aggregation-tool/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

建立一個 .NET Console 應用程式,用於從多個版控平台 (GitLab, BitBucket) 抓取指定時間範圍內的 PR/MR 變更資訊,並可選擇性地從 Branch 名稱解析 Azure DevOps Work Item ID 以抓取對應的工作項目資訊。所有資料將整理為結構化 JSON 格式匯出,支援部分失敗容錯處理與清楚的錯誤訊息。此階段專注於結構設計與類別架構規劃,暫不實作完整的資料抓取邏輯。

## Technical Context

**Language/Version**: C# 12 / .NET 8.0
**Primary Dependencies**:
  - System.CommandLine (命令列參數處理)
  - Microsoft.Extensions.Configuration (組態管理,包含 appsettings.json 與 secure.json)
  - Microsoft.Extensions.DependencyInjection (依賴注入容器)
  - Microsoft.Extensions.Logging (結構化日誌)
  - System.Text.Json (JSON 序列化/反序列化)
  - NEEDS CLARIFICATION: GitLab API v4 用戶端函式庫 (官方或第三方)
  - NEEDS CLARIFICATION: BitBucket Cloud/Server API 用戶端函式庫
  - NEEDS CLARIFICATION: Azure DevOps REST API 用戶端函式庫

**Storage**:
  - 組態檔案: appsettings.json (公開設定), secure.json (敏感資訊,不納入版控)
  - 輸出檔案: JSON 格式匯出 (檔案系統)

**Testing**: xUnit (單元測試與整合測試), FluentAssertions (可讀性斷言), NSubstitute (模擬物件)
**Target Platform**: 跨平台 Console 應用程式 (Windows, Linux, macOS)
**Project Type**: Single Console Application
**Performance Goals**:
  - 單一平台抓取 100 筆 PR/MR 資料應在 30 秒內完成 (不含網路傳輸時間)
  - 支援處理超過 100 筆 PR/MR 資料集而不發生錯誤

**Constraints**:
  - NEEDS CLARIFICATION: Branch 名稱的 Work Item ID 解析 Regex 格式細節
  - 部分平台失敗時仍須繼續處理其他平台
  - 錯誤訊息需清楚指引使用者處理方式 (90% 以上不需查閱文件即可理解)

**Scale/Scope**:
  - 預計支援同時抓取 3 個平台 (GitLab, BitBucket, Azure DevOps)
  - 每個平台支援多個專案/儲存庫設定
  - 預計單次執行處理數百筆 PR/MR 資料

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Test-First Development (Principle I)
- **Status**: PASS
- **Justification**: 將於實作階段採用 TDD 流程,先撰寫失敗測試後再實作功能
- **Action**: 所有公開 API 與業務邏輯方法必須先撰寫單元測試

### ✅ Code Quality Standards (Principle II)
- **Status**: PASS
- **Justification**: 專案將啟用 .editorconfig、StyleCop Analyzers 與 TreatWarningsAsErrors
- **Action**:
  - 設定 .editorconfig 定義 C# 程式碼風格
  - 設定專案檔啟用 Roslyn 分析器與警告視為錯誤
  - 遵循 KISS 原則,避免過早抽象化

### ✅ Test Coverage & Types (Principle III)
- **Status**: PASS
- **Plan**:
  - **Contract Tests**: 驗證各平台 API 回應結構是否符合預期 Schema
  - **Integration Tests**: 測試 DI 容器組態、組態檔讀取、完整工作流程
  - **Unit Tests**: 測試 Domain 邏輯 (Work Item ID 解析、資料轉換、驗證規則)

### ✅ Performance Requirements (Principle IV)
- **Status**: PASS
- **Constraints**: 單一平台 100 筆 PR/MR 抓取應於 30 秒內完成 (不含網路 I/O)
- **Actions**:
  - 使用 async/await 處理所有 I/O 操作
  - 避免在迴圈中使用 LINQ 處理大量資料集
  - 必要時使用 BenchmarkDotNet 進行微基準測試

### ✅ Observability & Debugging (Principle V)
- **Status**: PASS
- **Plan**:
  - 使用 ILogger<T> 進行結構化日誌記錄
  - 所有錯誤必須包含上下文資訊 (平台名稱、PR/MR ID、錯誤原因)
  - 錯誤訊息提供使用者可操作的指引
  - 不記錄敏感資訊 (Access Token, Personal Access Token)

### ✅ Domain-Driven Design (Principle VI)
- **Status**: PASS (適度應用)
- **Bounded Contexts**:
  - **Version Control Context**: 處理 PR/MR 資料抓取與轉換
  - **Work Item Context**: 處理 Azure DevOps Work Item 資料抓取
  - **Integration Context**: 協調多平台資料聚合與匯出
- **Tactical Patterns** (遵循 KISS,僅在必要時使用):
  - **Value Objects**: DateRange (時間範圍), BranchName (分支名稱), WorkItemId
  - **Entities**: PullRequestInfo, WorkItemInfo (具有唯一識別碼)
  - **Domain Services**: WorkItemIdParser (從 Branch 名稱解析 Work Item ID)
  - **Repositories**: IPullRequestRepository, IWorkItemRepository (抽象資料存取)
- **Anti-Corruption Layer**: 各平台 API 回應轉換為內部 Domain Model,避免外部模型污染

### ✅ Design Patterns & SOLID (Principle VII)
- **Status**: PASS
- **Patterns** (適度應用,遵循 KISS):
  - **Strategy Pattern**: 不同平台的 PR/MR 抓取策略 (IPlatformService)
  - **Repository Pattern**: 抽象各平台資料存取
  - **Dependency Injection**: 所有相依性透過建構子注入
  - **Factory Pattern**: 根據組態建立不同平台服務實例
- **SOLID Principles**:
  - **SRP**: 每個類別單一職責 (例如: GitLabService 只負責 GitLab API 互動)
  - **OCP**: 新增平台時無須修改既有程式碼,透過介面擴展
  - **LSP**: 所有 IPlatformService 實作可互相替換
  - **ISP**: 介面保持小而專注 (例如: IWorkItemParser 與 IWorkItemFetcher 分離)
  - **DIP**: 依賴抽象介面而非具體實作

### ✅ CQRS (Principle VIII)
- **Status**: PASS (簡化版本,適用於 Console 應用程式)
- **Commands**: 無狀態變更操作 (此工具為唯讀資料抓取,無 Command 需求)
- **Queries**:
  - FetchPullRequestsQuery (抓取 PR/MR 資料)
  - FetchWorkItemQuery (抓取 Work Item 資料)
- **Action**: 使用 MediatR 實作 Query Handler 模式 (可選,視複雜度決定是否引入)

### ✅ Program.cs Organization (Principle IX)
- **Status**: PASS
- **Requirements**:
  - Program.cs 僅包含服務註冊與 Host 建構
  - 複雜組態邏輯抽取至 Extension Methods (例如: AddGitLabServices, AddBitBucketServices)
  - 不在 Program.cs 中撰寫業務邏輯或資料處理邏輯

### ✅ XML Documentation (Principle X)
- **Status**: PASS
- **Requirements**:
  - 所有 public 類別、方法、屬性必須包含 XML 文件註解
  - 使用繁體中文說明業務領域概念
  - 啟用 GenerateDocumentationFile 產生文件檔案

### ✅ Inline Comments (Principle XI)
- **Status**: PASS
- **Guidelines**:
  - 使用繁體中文註解說明業務規則與領域邏輯
  - 技術實作細節可使用英文
  - 避免冗餘註解,優先透過清楚的命名表達意圖

### ✅ Performance Standards (Principle XII)
- **Status**: PASS
- **C# Specific**:
  - 所有 I/O 操作使用 async/await (API 呼叫、檔案寫入)
  - 避免在 async 方法中使用 .Result 或 .Wait()
  - 使用 ConfigureAwait(false) 於函式庫程式碼
  - 必要時使用 Span<T> 或 Memory<T> 優化緩衝區操作

### ✅ Documentation Language Standards (Principle XIII)
- **Status**: PASS
- **Compliance**:
  - spec.md, plan.md 使用繁體中文撰寫
  - 程式碼 XML 文件註解優先使用繁體中文
  - 程式碼識別碼 (類別名稱、方法名稱、變數名稱) 使用英文遵循 C# 慣例
  - README 與使用者文件使用繁體中文

### Summary
**所有憲章檢查項目均通過,無需例外申請。此功能遵循 KISS 原則,僅在必要時應用 DDD 與設計模式,避免過度工程化。**

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
src/
├── ReleaseSync.Console/          # Console 應用程式進入點
│   ├── Program.cs                # 應用程式進入點與 DI 容器設定
│   ├── Commands/                 # System.CommandLine 命令定義
│   │   └── SyncCommand.cs        # 主要同步命令與參數定義
│   ├── Handlers/                 # 命令處理器 (協調 Application Layer)
│   │   └── SyncCommandHandler.cs
│   ├── appsettings.json          # 公開組態設定
│   └── appsettings.secure.json   # 敏感資訊 (不納入版控,範本另建)
│
├── ReleaseSync.Domain/           # 領域層 (Domain Layer)
│   ├── Models/                   # 領域實體與值物件
│   │   ├── PullRequestInfo.cs    # PR/MR 實體
│   │   ├── WorkItemInfo.cs       # Work Item 實體
│   │   ├── DateRange.cs          # 時間範圍值物件
│   │   ├── BranchName.cs         # 分支名稱值物件
│   │   └── WorkItemId.cs         # Work Item ID 值物件
│   ├── Services/                 # 領域服務
│   │   └── IWorkItemIdParser.cs  # Work Item ID 解析服務介面
│   └── Repositories/             # Repository 介面 (實作在 Infrastructure)
│       ├── IPullRequestRepository.cs
│       └── IWorkItemRepository.cs
│
├── ReleaseSync.Application/      # 應用層 (Application Layer)
│   ├── Services/                 # 應用服務 (協調領域邏輯與基礎設施)
│   │   ├── ISyncOrchestrator.cs  # 同步協調器介面
│   │   └── SyncOrchestrator.cs   # 同步協調器實作
│   ├── DTOs/                     # 資料傳輸物件
│   │   ├── SyncRequest.cs        # 同步請求 DTO
│   │   └── SyncResult.cs         # 同步結果 DTO
│   └── Exporters/                # 資料匯出器
│       ├── IResultExporter.cs    # 匯出器介面
│       └── JsonFileExporter.cs   # JSON 檔案匯出實作
│
├── ReleaseSync.Infrastructure/   # 基礎設施層 (Infrastructure Layer)
│   ├── Configuration/            # 組態模型
│   │   ├── GitLabSettings.cs     # GitLab 組態
│   │   ├── BitBucketSettings.cs  # BitBucket 組態
│   │   ├── AzureDevOpsSettings.cs # Azure DevOps 組態
│   │   └── UserMappingSettings.cs # 使用者對應組態
│   ├── Platforms/                # 各平台服務實作 (Anti-Corruption Layer)
│   │   ├── GitLab/
│   │   │   ├── GitLabService.cs  # GitLab API 互動服務
│   │   │   ├── GitLabApiClient.cs # GitLab API 用戶端封裝
│   │   │   └── GitLabPullRequestRepository.cs # GitLab Repository 實作
│   │   ├── BitBucket/
│   │   │   ├── BitBucketService.cs
│   │   │   ├── BitBucketApiClient.cs
│   │   │   └── BitBucketPullRequestRepository.cs
│   │   └── AzureDevOps/
│   │       ├── AzureDevOpsService.cs
│   │       ├── AzureDevOpsApiClient.cs
│   │       └── AzureDevOpsWorkItemRepository.cs
│   ├── Parsers/                  # 解析器實作
│   │   └── RegexWorkItemIdParser.cs # Regex 解析 Work Item ID
│   └── DependencyInjection/      # DI 擴充方法
│       ├── GitLabServiceExtensions.cs
│       ├── BitBucketServiceExtensions.cs
│       └── AzureDevOpsServiceExtensions.cs
│
tests/
├── ReleaseSync.Domain.UnitTests/       # 領域層單元測試
│   ├── Models/
│   │   ├── PullRequestInfoTests.cs
│   │   └── WorkItemInfoTests.cs
│   └── Services/
│       └── WorkItemIdParserTests.cs
│
├── ReleaseSync.Application.UnitTests/  # 應用層單元測試
│   └── Services/
│       └── SyncOrchestratorTests.cs
│
├── ReleaseSync.Infrastructure.UnitTests/ # 基礎設施層單元測試
│   ├── Platforms/
│   │   ├── GitLabServiceTests.cs
│   │   └── BitBucketServiceTests.cs
│   └── Parsers/
│       └── RegexWorkItemIdParserTests.cs
│
└── ReleaseSync.Integration.Tests/      # 整合測試
    ├── Configuration/
    │   └── ConfigurationLoadingTests.cs # 組態載入測試
    ├── Platforms/
    │   └── PlatformApiContractTests.cs  # API Contract 測試
    └── EndToEnd/
        └── SyncWorkflowTests.cs         # 端到端工作流程測試
```

**Structure Decision**: 採用 **Clean Architecture** 分層架構 (Single Console Application):
- **Domain Layer**: 核心業務邏輯與領域模型,無外部相依性
- **Application Layer**: 協調領域邏輯與基礎設施,定義應用服務介面
- **Infrastructure Layer**: 實作外部系統整合 (API 用戶端、Repository 實作、組態管理)
- **Console Layer**: 使用者介面層,處理命令列參數與呼叫應用層

此結構遵循 **Dependency Inversion Principle**,Domain 與 Application 層不依賴 Infrastructure,所有相依性透過介面抽象化。測試結構對應各層級,確保清楚的測試邊界。

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

**無違反憲章的複雜度問題,此區塊留空。**

---

## Phase 0: Research & Design Decisions

### 需要釐清的技術問題 (NEEDS CLARIFICATION)

以下項目需要進行研究以解決技術背景中的未確認事項:

1. **GitLab API v4 用戶端函式庫選擇**
   - **問題**: 應使用官方 SDK 或第三方函式庫 (例如 NGitLab, GitLabApiClient)?
   - **研究目標**: 評估函式庫成熟度、API 涵蓋範圍、社群支援、授權條款

2. **BitBucket API 用戶端函式庫選擇**
   - **問題**: BitBucket Cloud 與 BitBucket Server API 不同,需確認目標平台與對應函式庫
   - **研究目標**: 確認使用者需求 (Cloud/Server/Both)、評估可用函式庫或使用 HttpClient 直接呼叫

3. **Azure DevOps REST API 用戶端函式庫選擇**
   - **問題**: 應使用官方 `Microsoft.TeamFoundationServer.Client` 或 `Microsoft.VisualStudio.Services.Client`?
   - **研究目標**: 評估官方 SDK 的 .NET 8 相容性、Work Item API 涵蓋範圍

4. **Branch 名稱解析 Regex 格式**
   - **問題**: 使用者提供範例 `vsts(\d+)` (不區分大小寫),但需確認是否有其他格式變體
   - **研究目標**: 向使用者確認 Branch 命名慣例,設計可設定化的 Regex 模式

5. **appsettings.secure.json 的安全載入機制**
   - **問題**: 如何確保 secure.json 不納入版控但能正確載入?
   - **研究目標**: 研究 .NET Configuration Provider 的最佳實務 (optional: true, 範本檔案管理)

6. **MediatR 是否必要?**
   - **問題**: Console 應用程式是否需要引入 MediatR 實作 CQRS Query Handler?
   - **研究目標**: 評估直接呼叫 Application Service vs. MediatR 的複雜度權衡 (遵循 KISS 原則)

---

## Phase 1: Design Artifacts (待產生)

Phase 1 將產生以下設計文件:

### 1. data-model.md
- 定義所有 Domain 實體與值物件的詳細結構
- 包含驗證規則、不變條件、狀態轉換

### 2. contracts/ 目錄
- 定義 Console 命令參數結構
- 定義 appsettings.json 與 appsettings.secure.json 的 JSON Schema
- 定義各平台 API 回應的 Contract 測試規格

### 3. quickstart.md
- 提供快速開始指南,說明如何設定與執行工具
- 包含組態檔範例與命令列參數說明

---

## 總結

此 Implementation Plan 定義了 PR/MR 變更資訊聚合工具的技術背景、憲章合規性檢查、專案結構設計與研究需求。

**下一步驟**:
1. **Phase 0**: 產生 `research.md`,解決所有 NEEDS CLARIFICATION 項目
2. **Phase 1**: 基於研究結果產生 `data-model.md`, `contracts/`, `quickstart.md`
3. **Phase 1**: 更新 agent context (CLAUDE.md)
4. **Phase 2**: 產生 `tasks.md` (由 `/speckit.tasks` 命令執行,非此命令範圍)

**憲章檢查結論**: ✅ 所有檢查項目通過,可進入 Phase 0 研究階段。

---

## Phase 1 後憲章檢查 (Post-Design Review)

**檢查時間**: 2025-10-18
**檢查範圍**: Phase 0 (research.md) 與 Phase 1 (data-model.md, contracts/, quickstart.md) 設計產出

### ✅ Domain-Driven Design (Principle VI) - Re-check

**檢查結果**: **PASS**

**驗證項目**:
- ✅ **Bounded Contexts 清楚定義**: Version Control Context, Work Item Context, Integration Context 邊界明確
- ✅ **Value Objects 正確使用**: DateRange, BranchName, WorkItemId, PlatformSyncStatus 皆為不可變物件,具有驗證邏輯
- ✅ **Entities 識別清楚**: PullRequestInfo 與 WorkItemInfo 具有唯一識別碼,封裝業務邏輯
- ✅ **Aggregate Root 適當**: SyncResult 作為聚合根,控制所有 PullRequestInfo 與 PlatformSyncStatus 的存取
- ✅ **Repository Pattern**: IPullRequestRepository 與 IWorkItemRepository 抽象資料存取,遵循 DIP
- ✅ **Domain Services**: IWorkItemIdParser 處理跨實體的業務邏輯 (Branch 名稱解析)
- ✅ **Anti-Corruption Layer**: 各平台 API 回應將轉換為內部 Domain Model,避免外部模型污染

**符合 KISS 原則**: 僅在必要時使用 DDD Tactical Patterns,未過度設計。

---

### ✅ SOLID Principles (Principle VII) - Re-check

**檢查結果**: **PASS**

**驗證項目**:
- ✅ **Single Responsibility**: 每個類別職責單一 (例如: DateRange 僅處理時間範圍驗證與查詢)
- ✅ **Open/Closed**: IPullRequestRepository 支援新增平台實作而無需修改介面
- ✅ **Liskov Substitution**: 所有 IPullRequestRepository 實作可互相替換
- ✅ **Interface Segregation**: IWorkItemIdParser 與 IWorkItemRepository 分離,介面保持小而專注
- ✅ **Dependency Inversion**: Domain Layer 不依賴 Infrastructure,所有相依性透過介面抽象

---

### ✅ API Contracts 清楚定義

**檢查結果**: **PASS**

**產出**:
- ✅ `command-line-parameters.md`: 詳細定義所有命令列參數,包含驗證規則與使用範例
- ✅ `appsettings-schema.json`: appsettings.json 的完整 JSON Schema,支援 IDE 自動完成與驗證
- ✅ `appsettings-secure-schema.json`: 敏感資訊組態 Schema,清楚定義必要欄位
- ✅ `appsettings.example.json`: 提供完整範例,降低使用者設定門檻
- ✅ `output-json-schema.json`: 輸出 JSON 格式 Schema,確保資料交換標準化

**文件品質**: 所有 Contracts 皆使用繁體中文說明,符合 Principle XIII (Documentation Language Standards)

---

### ✅ Documentation Language Standards (Principle XIII) - Re-check

**檢查結果**: **PASS**

**驗證項目**:
- ✅ `plan.md`: 使用繁體中文撰寫
- ✅ `research.md`: 使用繁體中文撰寫
- ✅ `data-model.md`: 使用繁體中文撰寫,程式碼註解使用繁體中文說明業務邏輯
- ✅ `quickstart.md`: 使用繁體中文撰寫,提供清楚的設定與執行指引
- ✅ `contracts/*.md`: 使用繁體中文說明,JSON Schema 的 description 欄位使用繁體中文

**程式碼識別碼**: 使用英文遵循 C# 慣例 (例如: `PullRequestInfo`, `SyncResult`)

---

### ✅ KISS Principle 遵循度

**檢查結果**: **PASS**

**驗證項目**:
- ✅ **不使用 MediatR**: 避免不必要的抽象層,直接呼叫 Application Service (research.md 決策)
- ✅ **Value Objects 使用適度**: 僅在需要驗證邏輯或不可變性時使用,未過度抽象
- ✅ **Repository Pattern 必要性**: 抽象多平台資料存取,合理應用
- ✅ **Aggregate Root 簡化**: SyncResult 僅管理必要的聚合邊界,未過度複雜化
- ✅ **組態設計簡潔**: appsettings.json 結構清楚,使用者易於理解與設定

---

### 總結

**Phase 1 設計階段憲章檢查結果**: ✅ **PASS**

**關鍵成果**:
1. ✅ 所有設計產出符合憲章原則
2. ✅ DDD Tactical Patterns 適度應用,未過度工程化
3. ✅ SOLID 原則正確遵循
4. ✅ API Contracts 完整定義,支援使用者快速上手
5. ✅ 文件語言標準符合繁體中文要求
6. ✅ KISS 原則貫徹,避免不必要複雜度

**無違反憲章的項目,可進入 Phase 2 (tasks.md 產生階段)。**