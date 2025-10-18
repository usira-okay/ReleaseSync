# Tasks: PR/MR 變更資訊聚合工具

**Input**: Design documents from `/specs/002-pr-aggregation-tool/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md

**Tests**: 本專案遵循 TDD 原則,所有公開 API 與業務邏輯必須先撰寫測試。

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: 建立專案基礎結構與設定檔

- [X] T001 建立 .NET Solution 與專案結構 (ReleaseSync.sln, Domain/Application/Infrastructure/Console 專案)
- [X] T002 [P] 設定 .editorconfig 與 StyleCop Analyzers 於 src/ 根目錄
- [X] T003 [P] 建立 .gitignore,排除 appsettings.secure.json 與 bin/obj 目錄
- [X] T004 [P] 複製 appsettings.example.json 與 appsettings.secure.example.json 至 src/ReleaseSync.Console/
- [X] T005 建立測試專案結構 (Domain.UnitTests, Application.UnitTests, Infrastructure.UnitTests, Integration.Tests)
- [X] T006 [P] 安裝 NuGet 套件至 Infrastructure 專案: NGitLab 9.3.0, Microsoft.TeamFoundationServer.Client 19.225.1
- [X] T007 [P] 安裝 NuGet 套件至 Console 專案: System.CommandLine, Microsoft.Extensions.Configuration.Json, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging
- [X] T008 [P] 安裝測試相關 NuGet 套件至測試專案: xUnit, FluentAssertions, NSubstitute

**Checkpoint**: 專案結構已建立,可開始實作

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 核心 Domain 模型與基礎設施,所有 User Stories 皆依賴此階段完成

**⚠️ CRITICAL**: 此階段必須完成後,才能開始實作任何 User Story

### Domain Layer - Value Objects (可平行實作)

- [X] T009 [P] 建立 DateRange 值物件與單元測試 in src/ReleaseSync.Domain/Models/DateRange.cs + tests/ReleaseSync.Domain.UnitTests/Models/DateRangeTests.cs
- [X] T010 [P] 建立 BranchName 值物件與單元測試 in src/ReleaseSync.Domain/Models/BranchName.cs + tests/ReleaseSync.Domain.UnitTests/Models/BranchNameTests.cs
- [X] T011 [P] 建立 WorkItemId 值物件與單元測試 in src/ReleaseSync.Domain/Models/WorkItemId.cs + tests/ReleaseSync.Domain.UnitTests/Models/WorkItemIdTests.cs
- [X] T012 [P] 建立 PlatformSyncStatus 值物件 in src/ReleaseSync.Domain/Models/PlatformSyncStatus.cs

### Domain Layer - Entities (依賴 Value Objects)

- [X] T013 建立 PullRequestInfo 實體與單元測試 in src/ReleaseSync.Domain/Models/PullRequestInfo.cs + tests/ReleaseSync.Domain.UnitTests/Models/PullRequestInfoTests.cs
- [X] T014 建立 WorkItemInfo 實體與單元測試 in src/ReleaseSync.Domain/Models/WorkItemInfo.cs + tests/ReleaseSync.Domain.UnitTests/Models/WorkItemInfoTests.cs

### Domain Layer - Aggregate Root & Interfaces

- [X] T015 建立 SyncResult 聚合根與單元測試 in src/ReleaseSync.Domain/Models/SyncResult.cs + tests/ReleaseSync.Domain.UnitTests/Models/SyncResultTests.cs
- [X] T016 [P] 定義 IWorkItemIdParser 介面 in src/ReleaseSync.Domain/Services/IWorkItemIdParser.cs
- [X] T017 [P] 定義 IPullRequestRepository 介面 in src/ReleaseSync.Domain/Repositories/IPullRequestRepository.cs
- [X] T018 [P] 定義 IWorkItemRepository 介面 in src/ReleaseSync.Domain/Repositories/IWorkItemRepository.cs

### Infrastructure Layer - Configuration Models (可平行實作)

- [X] T019 [P] 建立 GitLabSettings 組態模型 in src/ReleaseSync.Infrastructure/Configuration/GitLabSettings.cs
- [X] T020 [P] 建立 BitBucketSettings 組態模型 in src/ReleaseSync.Infrastructure/Configuration/BitBucketSettings.cs
- [X] T021 [P] 建立 AzureDevOpsSettings 組態模型 in src/ReleaseSync.Infrastructure/Configuration/AzureDevOpsSettings.cs
- [X] T022 [P] 建立 UserMappingSettings 組態模型 in src/ReleaseSync.Infrastructure/Configuration/UserMappingSettings.cs

### Application Layer - DTOs & Interfaces

- [X] T023 [P] 建立 SyncRequest DTO in src/ReleaseSync.Application/DTOs/SyncRequest.cs
- [X] T024 [P] 建立 SyncResult DTO in src/ReleaseSync.Application/DTOs/SyncResultDto.cs
- [X] T025 [P] 定義 ISyncOrchestrator 介面 in src/ReleaseSync.Application/Services/ISyncOrchestrator.cs
- [X] T026 [P] 定義 IResultExporter 介面 in src/ReleaseSync.Application/Exporters/IResultExporter.cs

**Checkpoint**: 所有基礎 Domain 模型、Repository 介面、Application 介面已完成,User Stories 可開始平行實作

---

## Phase 3: User Story 1 - 從單一平台抓取 PR/MR 資訊 (Priority: P1) 🎯 MVP

**Goal**: 能夠從 GitLab 或 BitBucket 抓取指定時間範圍內的 PR/MR 資訊

**Independent Test**: 執行工具並指定單一平台(例如 GitLab)和時間範圍,驗證是否能成功抓取並顯示 PR/MR 清單資訊

### Tests for User Story 1 (TDD - 先寫測試)

- [X] T027 [P] [US1] Contract Test: 驗證 GitLab API 回應結構 in tests/ReleaseSync.Integration.Tests/Platforms/GitLabApiContractTests.cs
- [X] T028 [P] [US1] Contract Test: 驗證 BitBucket API 回應結構 in tests/ReleaseSync.Integration.Tests/Platforms/BitBucketApiContractTests.cs
- [X] T029 [P] [US1] Unit Test: GitLabService 單元測試 in tests/ReleaseSync.Infrastructure.UnitTests/Platforms/GitLabServiceTests.cs
- [X] T030 [P] [US1] Unit Test: BitBucketService 單元測試 in tests/ReleaseSync.Infrastructure.UnitTests/Platforms/BitBucketServiceTests.cs

### Implementation for User Story 1

#### Infrastructure - GitLab Platform

- [X] T031 [P] [US1] 實作 GitLabApiClient 封裝 NGitLab in src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabApiClient.cs
- [X] T032 [US1] 實作 GitLabPullRequestRepository (依賴 T031) in src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabPullRequestRepository.cs
- [X] T033 [US1] 實作 GitLabService 協調 Repository in src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabService.cs
- [X] T034 [US1] 建立 GitLabServiceExtensions DI 註冊 in src/ReleaseSync.Infrastructure/DependencyInjection/GitLabServiceExtensions.cs

#### Infrastructure - BitBucket Platform

- [X] T035 [P] [US1] 實作 BitBucketApiClient 使用 HttpClient in src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketApiClient.cs
- [X] T036 [US1] 實作 BitBucketPullRequestRepository (依賴 T035) in src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketPullRequestRepository.cs
- [X] T037 [US1] 實作 BitBucketService 協調 Repository in src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketService.cs
- [X] T038 [US1] 建立 BitBucketServiceExtensions DI 註冊 in src/ReleaseSync.Infrastructure/DependencyInjection/BitBucketServiceExtensions.cs

#### Application - Orchestration

- [X] T039 [US1] 實作 SyncOrchestrator 協調多平台抓取 in src/ReleaseSync.Application/Services/SyncOrchestrator.cs
- [ ] T040 [P] [US1] Unit Test: SyncOrchestrator 單元測試 in tests/ReleaseSync.Application.UnitTests/Services/SyncOrchestratorTests.cs

#### Console - Command Line Interface

- [X] T041 [US1] 實作 SyncCommand 定義命令列參數 in src/ReleaseSync.Console/Commands/SyncCommand.cs
- [X] T042 [US1] 實作 SyncCommandHandler 處理命令執行 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs
- [X] T043 [US1] 設定 Program.cs DI 容器與命令列解析 in src/ReleaseSync.Console/Program.cs

#### Integration Tests

- [ ] T044 [US1] Integration Test: 完整 GitLab 工作流程測試 in tests/ReleaseSync.Integration.Tests/EndToEnd/GitLabSyncWorkflowTests.cs
- [ ] T045 [US1] Integration Test: 完整 BitBucket 工作流程測試 in tests/ReleaseSync.Integration.Tests/EndToEnd/BitBucketSyncWorkflowTests.cs
- [ ] T046 [US1] Integration Test: 組態檔載入測試 in tests/ReleaseSync.Integration.Tests/Configuration/ConfigurationLoadingTests.cs

**Checkpoint**: User Story 1 完成 - 可從 GitLab/BitBucket 抓取 PR/MR 並在 Console 顯示

---

## Phase 4: User Story 2 - 將 PR/MR 資訊匯出為 JSON 檔案 (Priority: P2)

**Goal**: 能夠將抓取到的 PR/MR 資訊匯出為結構化 JSON 檔案

**Independent Test**: 執行工具後,驗證是否能選擇匯出選項並成功產生包含所有 PR/MR 資訊的 JSON 檔案

### Tests for User Story 2 (TDD - 先寫測試)

- [ ] T047 [P] [US2] Unit Test: JsonFileExporter 單元測試 in tests/ReleaseSync.Application.UnitTests/Exporters/JsonFileExporterTests.cs
- [ ] T048 [P] [US2] Integration Test: JSON 輸出格式驗證測試 in tests/ReleaseSync.Integration.Tests/Exporters/JsonExportValidationTests.cs

### Implementation for User Story 2

- [X] T049 [P] [US2] 實作 JsonFileExporter 使用 System.Text.Json in src/ReleaseSync.Application/Exporters/JsonFileExporter.cs
- [X] T050 [US2] 更新 SyncCommandHandler 加入 --output-file 與 --force 參數處理 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs
- [X] T051 [US2] 更新 SyncCommand 加入匯出相關參數定義 in src/ReleaseSync.Console/Commands/SyncCommand.cs
- [X] T052 [US2] 實作檔案已存在確認邏輯與錯誤處理 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs

#### Integration Tests

- [ ] T053 [US2] Integration Test: 端到端 JSON 匯出工作流程測試 in tests/ReleaseSync.Integration.Tests/EndToEnd/JsonExportWorkflowTests.cs

**Checkpoint**: User Story 2 完成 - 可將 PR/MR 資訊匯出為 JSON 檔案

---

## Phase 5: User Story 3 - 從 Branch 名稱解析 Azure DevOps Work Item (Priority: P3)

**Goal**: 能夠從 PR/MR 的 Branch 名稱中解析出 Azure DevOps Work Item ID,並抓取對應的 Work Item 資訊

**Independent Test**: 使用包含 Work Item ID 的 Branch 名稱(例如 feature/12345-new-feature),驗證工具是否能成功解析 ID 並抓取 Azure DevOps 資訊

### Tests for User Story 3 (TDD - 先寫測試)

- [ ] T054 [P] [US3] Unit Test: RegexWorkItemIdParser 單元測試 in tests/ReleaseSync.Infrastructure.UnitTests/Parsers/RegexWorkItemIdParserTests.cs
- [ ] T055 [P] [US3] Unit Test: AzureDevOpsService 單元測試 in tests/ReleaseSync.Infrastructure.UnitTests/Platforms/AzureDevOpsServiceTests.cs
- [ ] T056 [P] [US3] Contract Test: Azure DevOps Work Item API 回應結構驗證 in tests/ReleaseSync.Integration.Tests/Platforms/AzureDevOpsApiContractTests.cs

### Implementation for User Story 3

#### Infrastructure - Work Item Parser

- [X] T057 [P] [US3] 實作 RegexWorkItemIdParser 解析 Branch 名稱 in src/ReleaseSync.Infrastructure/Parsers/RegexWorkItemIdParser.cs

#### Infrastructure - Azure DevOps Platform

- [X] T058 [P] [US3] 實作 AzureDevOpsApiClient 封裝 Microsoft.TeamFoundationServer.Client in src/ReleaseSync.Infrastructure/Platforms/AzureDevOps/AzureDevOpsApiClient.cs
- [X] T059 [US3] 實作 AzureDevOpsWorkItemRepository (依賴 T058) in src/ReleaseSync.Infrastructure/Platforms/AzureDevOps/AzureDevOpsWorkItemRepository.cs
- [X] T060 [US3] 實作 AzureDevOpsService 協調 Repository in src/ReleaseSync.Infrastructure/Platforms/AzureDevOps/AzureDevOpsService.cs
- [X] T061 [US3] 建立 AzureDevOpsServiceExtensions DI 註冊 in src/ReleaseSync.Infrastructure/DependencyInjection/AzureDevOpsServiceExtensions.cs

#### Application - Integration Logic

- [X] T062 [US3] 更新 SyncOrchestrator 加入 Work Item 解析與關聯邏輯 in src/ReleaseSync.Application/Services/SyncOrchestrator.cs
- [X] T063 [US3] 更新 SyncCommandHandler 加入 --enable-azure-devops 參數處理 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs
- [X] T064 [US3] 更新 SyncCommand 加入 Azure DevOps 參數定義 in src/ReleaseSync.Console/Commands/SyncCommand.cs

#### Integration Tests

- [ ] T065 [US3] Integration Test: 完整 Work Item 解析工作流程測試 in tests/ReleaseSync.Integration.Tests/EndToEnd/WorkItemIntegrationWorkflowTests.cs
- [ ] T066 [US3] Integration Test: Branch 名稱無法解析時的錯誤處理測試 in tests/ReleaseSync.Integration.Tests/EndToEnd/WorkItemParsingFailureTests.cs

**Checkpoint**: User Story 3 完成 - 可從 Branch 名稱解析 Work Item ID 並抓取 Azure DevOps 資訊

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 改善程式碼品質、效能與可維護性

- [X] T067 [P] 實作 ILogger 結構化日誌記錄於所有服務層 (GitLabService, BitBucketService, AzureDevOpsService)
- [X] T068 [P] 實作錯誤處理與使用者友善錯誤訊息於 SyncCommandHandler
- [X] T069 [P] 加入 --verbose 參數支援 Debug 等級日誌輸出 in src/ReleaseSync.Console/Commands/SyncCommand.cs
- [X] T070 驗證所有 public 類別與方法皆包含 XML 文件註解 (繁體中文)
- [X] T071 執行 quickstart.md 驗證流程,確保文件與實作一致
- [X] T072 [P] 效能測試: 驗證單一平台 100 筆 PR/MR 抓取於 30 秒內完成 (不含網路 I/O)
- [X] T073 程式碼審查: 確認所有類別遵循 SOLID 原則與 KISS 原則
- [X] T074 [P] 安全性審查: 確認不記錄敏感資訊 (Access Token, PAT)
- [X] T075 建立 README.md 提供專案概述與快速開始連結

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - 可立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - **阻塞所有 User Stories**
- **User Stories (Phase 3-5)**: 全部依賴 Foundational 完成
  - User Stories 可平行實作 (若有多人團隊)
  - 或依優先順序循序實作 (P1 → P2 → P3)
- **Polish (Phase 6)**: 依賴所有 User Stories 完成

### User Story Dependencies

- **User Story 1 (P1)**: 可於 Foundational (Phase 2) 完成後開始 - 無其他 User Story 依賴
- **User Story 2 (P2)**: 可於 Foundational (Phase 2) 完成後開始 - 可與 US1 平行實作,但需整合 US1 的 SyncOrchestrator
- **User Story 3 (P3)**: 可於 Foundational (Phase 2) 完成後開始 - 需整合 US1 的 SyncOrchestrator

### Within Each User Story

- Tests (TDD) MUST 先寫並確認失敗,再實作功能
- Value Objects → Entities → Aggregate Root
- Repository 介面 → Repository 實作
- Service 實作 → DI Extensions
- Application Orchestrator → Command Handler
- 核心實作 → Integration Tests

### Parallel Opportunities

#### Phase 1 (Setup)
```
T002 (editorconfig) || T003 (gitignore) || T004 (appsettings) || T006 (NuGet) || T007 (NuGet) || T008 (NuGet)
```

#### Phase 2 (Foundational)
```
# Value Objects (可全部平行)
T009 (DateRange) || T010 (BranchName) || T011 (WorkItemId) || T012 (PlatformSyncStatus)

# Configuration Models (可全部平行)
T019 (GitLabSettings) || T020 (BitBucketSettings) || T021 (AzureDevOpsSettings) || T022 (UserMappingSettings)

# Application Interfaces (可全部平行)
T023 (SyncRequest) || T024 (SyncResult) || T025 (ISyncOrchestrator) || T026 (IResultExporter)

# Domain Interfaces (可全部平行)
T016 (IWorkItemIdParser) || T017 (IPullRequestRepository) || T018 (IWorkItemRepository)
```

#### Phase 3 (User Story 1)
```
# Contract Tests (可全部平行)
T027 (GitLab Contract Test) || T028 (BitBucket Contract Test)

# Unit Tests (可全部平行)
T029 (GitLabService Test) || T030 (BitBucketService Test) || T040 (SyncOrchestrator Test)

# GitLab Platform (可與 BitBucket 平行)
T031 (GitLabApiClient) → T032 (GitLabPullRequestRepository) → T033 (GitLabService) → T034 (DI Extensions)

# BitBucket Platform (可與 GitLab 平行)
T035 (BitBucketApiClient) → T036 (BitBucketPullRequestRepository) → T037 (BitBucketService) → T038 (DI Extensions)
```

#### Phase 4 (User Story 2)
```
# Tests (可平行)
T047 (JsonFileExporter Test) || T048 (JSON Validation Test)

# Implementation (可平行)
T049 (JsonFileExporter) || T050 (Update Handler) || T051 (Update Command)
```

#### Phase 5 (User Story 3)
```
# Tests (可全部平行)
T054 (Parser Test) || T055 (AzureDevOps Test) || T056 (Contract Test)

# Implementation (可平行)
T057 (RegexWorkItemIdParser) || T058 (AzureDevOpsApiClient) → T059 (Repository) → T060 (Service) → T061 (DI Extensions)
```

#### Phase 6 (Polish)
```
# Cross-cutting concerns (可全部平行)
T067 (Logging) || T068 (Error Handling) || T069 (Verbose) || T070 (XML Docs) || T072 (Performance) || T073 (Code Review) || T074 (Security) || T075 (README)
```

---

## Parallel Example: User Story 1

### Launch all contract tests together:
```bash
Task: "[US1] Contract Test: 驗證 GitLab API 回應結構 in tests/.../GitLabApiContractTests.cs"
Task: "[US1] Contract Test: 驗證 BitBucket API 回應結構 in tests/.../BitBucketApiContractTests.cs"
```

### Launch GitLab and BitBucket platform implementation in parallel:
```bash
# GitLab Team
Task: "[US1] 實作 GitLabApiClient in .../GitLab/GitLabApiClient.cs"
# BitBucket Team
Task: "[US1] 實作 BitBucketApiClient in .../BitBucket/BitBucketApiClient.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (**CRITICAL** - 阻塞所有 User Stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: 測試 User Story 1 是否獨立運作
5. Deploy/Demo MVP

### Incremental Delivery

1. Setup + Foundational → 基礎完成 ✅
2. Add User Story 1 → 測試獨立運作 → Deploy/Demo (MVP!) 🎯
3. Add User Story 2 → 測試獨立運作 → Deploy/Demo
4. Add User Story 3 → 測試獨立運作 → Deploy/Demo
5. 每個 User Story 增加價值而不破壞既有功能

### Parallel Team Strategy

若有多位開發者:

1. 團隊共同完成 Setup + Foundational
2. Foundational 完成後:
   - Developer A: User Story 1 (GitLab + BitBucket Platform)
   - Developer B: User Story 2 (JSON Export)
   - Developer C: User Story 3 (Azure DevOps Integration)
3. 各 User Story 獨立完成與整合

---

## Notes

- **[P] 標記**: 表示可平行執行的任務 (不同檔案,無依賴)
- **[Story] 標記**: 標示任務屬於哪個 User Story (US1, US2, US3)
- **TDD 原則**: 所有測試必須先寫並確認失敗,再實作功能
- **獨立測試**: 每個 User Story 應能獨立完成與測試
- **檔案路徑**: 所有任務皆包含明確的檔案路徑
- **Checkpoints**: 每個 User Story 完成後皆有驗證檢查點
- **遵循憲章**: 所有實作皆遵循 SOLID 原則、KISS 原則、DDD Tactical Patterns (適度應用)
- **繁體中文註解**: XML 文件註解與業務邏輯註解使用繁體中文
- **避免**: 模糊任務、同檔案衝突、破壞獨立性的跨 User Story 依賴

---

**Total Tasks**: 75
**MVP Scope**: Phase 1 + Phase 2 + Phase 3 (User Story 1) = T001-T046 (46 tasks)
**Estimated MVP Completion**: 基礎架構完成後,User Story 1 約需 2-3 週 (視團隊規模)
