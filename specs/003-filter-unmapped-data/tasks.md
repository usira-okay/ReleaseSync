# Tasks: 資料過濾機制 - UserMapping 與 Team Mapping

**Feature**: 003-filter-unmapped-data
**Input**: Design documents from `/specs/003-filter-unmapped-data/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: 本專案遵循 Test-First Development (Constitution Principle I),所有測試任務都已包含並必須在實作前完成。

**Organization**: 任務按使用者故事分組,確保每個故事都可以獨立實作和測試。

## Format: `[ID] [P?] [Story] Description`
- **[P]**: 可平行執行 (不同檔案,無依賴關係)
- **[Story]**: 任務所屬的使用者故事 (例如: US1, US2, US3)
- 所有描述都包含確切的檔案路徑

## Path Conventions
本專案使用 Clean Architecture:
- Domain 層: `src/ReleaseSync.Domain/`
- Application 層: `src/ReleaseSync.Application/`
- Infrastructure 層: `src/ReleaseSync.Infrastructure/`
- Console 層: `src/ReleaseSync.Console/`
- 測試: `src/tests/`

---

## Phase 1: Setup (共用基礎設施)

**Purpose**: 專案初始化和基本結構設定

- [ ] T001 確認專案結構符合 plan.md 定義的 Clean Architecture 架構
- [ ] T002 驗證 .editorconfig 和 Roslyn analyzers 設定已啟用
- [ ] T003 [P] 檢視現有 UserMappingService 實作作為參考範本

---

## Phase 2: Foundational (阻塞性前置需求)

**Purpose**: 所有使用者故事開始前必須完成的核心基礎設施

**⚠️ CRITICAL**: 任何使用者故事都不能在此階段完成前開始

- [X] T004 [P] 定義 ITeamMappingService 介面在 `src/ReleaseSync.Domain/Services/ITeamMappingService.cs`
- [X] T005 [P] 擴展 IUserMappingService 介面加入 HasMapping() 和 IsFilteringEnabled() 方法在 `src/ReleaseSync.Application/Services/IUserMappingService.cs`
- [X] T006 [P] 建立 TeamMappingSettings 配置模型在 `src/ReleaseSync.Infrastructure/Configuration/TeamMappingSettings.cs`
- [X] T007 擴展 AzureDevOpsSettings 加入 TeamMapping 屬性在 `src/ReleaseSync.Infrastructure/Configuration/AzureDevOpsSettings.cs`
- [X] T008 擴展 WorkItemInfo 加入 Team 屬性在 `src/ReleaseSync.Domain/Models/WorkItemInfo.cs`

**Checkpoint**: 基礎設施就緒 - 使用者故事實作現在可以平行開始

---

## Phase 3: User Story 1 - 過濾未對應作者的 PR/MR (Priority: P1) 🎯 MVP

**Goal**: 實作 PR/MR 作者過濾功能,根據 UserMapping 只收錄已定義的團隊成員的 PR/MR

**Independent Test**: 設定 UserMapping 包含 3 個使用者,抓取包含 5 個不同作者的 PR/MR 清單,系統應只保留 UserMapping 中定義的 3 個使用者的 PR/MR,其餘 2 筆應被過濾掉。

### Tests for User Story 1 (Test-First Development)

**NOTE: 這些測試必須先寫,並確保在實作前失敗**

- [X] T009 [P] [US1] 建立 UserMappingService HasMapping 方法的單元測試在 `src/tests/ReleaseSync.Infrastructure.UnitTests/Services/UserMappingServiceTests.cs`
  - 測試已對應使用者返回 true
  - 測試未對應使用者返回 false
  - 測試空 UserMapping 返回 true (向後相容)
  - 測試大小寫不敏感比對

- [ ] T010 [P] [US1] 建立 GitLab PR 過濾邏輯的單元測試在 `src/tests/ReleaseSync.Infrastructure.UnitTests/Platforms/GitLabPullRequestRepositoryTests.cs`
  - 測試過濾掉未對應作者的 PR
  - 測試保留已對應作者的 PR
  - 測試空 UserMapping 不過濾任何 PR
  - 測試日誌記錄過濾統計

- [ ] T011 [P] [US1] 建立 BitBucket PR 過濾邏輯的單元測試在 `src/tests/ReleaseSync.Infrastructure.UnitTests/Platforms/BitBucketPullRequestRepositoryTests.cs`
  - 測試過濾掉未對應作者的 PR
  - 測試保留已對應作者的 PR
  - 測試空 UserMapping 不過濾任何 PR

- [ ] T012 [P] [US1] 建立完整過濾流程的整合測試在 `src/tests/ReleaseSync.Integration.Tests/EndToEnd/UserMappingFilteringWorkflowTests.cs`
  - 測試完整的 GitLab PR 過濾流程 (配置載入 → 抓取 → 過濾 → 輸出)
  - 測試完整的 BitBucket PR 過濾流程
  - 測試多平台對應 (同一人在不同平台)
  - 測試向後相容性 (空 UserMapping)

### Implementation for User Story 1

- [X] T013 [US1] 實作 UserMappingService.HasMapping() 方法在 `src/ReleaseSync.Infrastructure/Services/UserMappingService.cs`
  - 使用 HashSet 快速查找 (O(1))
  - 使用 StringComparer.OrdinalIgnoreCase
  - 空 Mapping 返回 true (向後相容)
  - 加入 XML 文件註解 (繁體中文)

- [X] T014 [US1] 實作 UserMappingService.IsFilteringEnabled() 方法在 `src/ReleaseSync.Infrastructure/Services/UserMappingService.cs`

- [X] T015 [US1] 在 GitLabPullRequestRepository 加入過濾邏輯在 `src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabPullRequestRepository.cs`
  - 注入 IUserMappingService
  - 在 GetPullRequestsAsync 中呼叫 HasMapping 過濾
  - 記錄過濾統計到日誌 (使用 ILogger)
  - 加入結構化日誌參數

- [X] T016 [US1] 在 BitBucketPullRequestRepository 加入過濾邏輯在 `src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketPullRequestRepository.cs`
  - 注入 IUserMappingService
  - 在 GetPullRequestsAsync 中呼叫 HasMapping 過濾
  - 記錄過濾統計到日誌

- [ ] T017 [US1] 執行所有 User Story 1 測試並確保通過

**Checkpoint**: 此時 User Story 1 應該完全功能正常且可獨立測試

---

## Phase 4: User Story 2 - 新增 Azure DevOps Team Mapping 配置 (Priority: P2)

**Goal**: 建立 TeamMapping 配置結構,讓系統管理員可以定義團隊對應關係

**Independent Test**: 在 appsettings.json 中新增 AzureDevOps.TeamMapping 區段,定義 3 組團隊對應,系統啟動時應能成功載入配置而不報錯。

### Tests for User Story 2 (Test-First Development)

- [ ] T018 [P] [US2] 建立 TeamMapping 配置載入的單元測試在 `src/tests/ReleaseSync.Infrastructure.UnitTests/Configuration/TeamMappingSettingsTests.cs`
  - 測試成功解析 TeamMapping JSON
  - 測試空 TeamMapping 不報錯
  - 測試重複 OriginalTeamName 記錄警告

- [ ] T019 [P] [US2] 建立配置契約測試在 `src/tests/ReleaseSync.Integration.Tests/Configuration/TeamMappingContractTests.cs`
  - 驗證 appsettings.example.json 符合 JSON Schema
  - 驗證配置可正確載入到 AzureDevOpsSettings

### Implementation for User Story 2

- [X] T020 [US2] 實作 TeamMappingService 在 `src/ReleaseSync.Infrastructure/Services/TeamMappingService.cs`
  - 實作 HasMapping(string? originalTeamName) 方法
  - 實作 GetDisplayName(string? originalTeamName) 方法
  - 實作 IsFilteringEnabled() 方法
  - 使用 HashSet 和 Dictionary 快速查找
  - 使用 StringComparer.OrdinalIgnoreCase
  - 空 Mapping 時 HasMapping 返回 true
  - 加入 XML 文件註解 (繁體中文)

- [X] T021 [US2] 建立 TeamMappingServiceExtensions DI 註冊在 `src/ReleaseSync.Infrastructure/DependencyInjection/TeamMappingServiceExtensions.cs`
  - 註冊 ITeamMappingService 為 Scoped
  - 從 AzureDevOpsSettings.TeamMapping 載入配置

- [X] T022 [US2] 更新 appsettings.example.json 加入 TeamMapping 範例在 `src/ReleaseSync.Console/appsettings.example.json`
  - 加入三組團隊對應範例
  - 使用使用者提供的團隊名稱

- [X] T023 [P] [US2] 建立 TeamMappingService 的單元測試在 `src/tests/ReleaseSync.Infrastructure.UnitTests/Services/TeamMappingServiceTests.cs`
  - 測試 HasMapping 已對應團隊返回 true
  - 測試 HasMapping 未對應團隊返回 false
  - 測試 HasMapping 空 Mapping 返回 true
  - 測試 GetDisplayName 返回正確的顯示名稱
  - 測試 GetDisplayName 無對應時返回原始名稱
  - 測試大小寫不敏感比對

- [ ] T024 [US2] 執行所有 User Story 2 測試並確保通過

**Checkpoint**: 此時 User Stories 1 和 2 應該都能獨立運作

---

## Phase 5: User Story 3 - 過濾未對應團隊的 Work Item (Priority: P3)

**Goal**: 實作 Work Item 團隊過濾功能,根據 TeamMapping 只收錄已定義團隊的 Work Item

**Independent Test**: 設定 TeamMapping 包含 2 個團隊,抓取包含 4 個不同團隊的 Work Item,系統應只保留 TeamMapping 中定義的 2 個團隊的 Work Item,其餘應被過濾掉。

### Tests for User Story 3 (Test-First Development)

- [ ] T025 [P] [US3] 建立 WorkItemInfo Team 屬性的單元測試在 `src/tests/ReleaseSync.Domain.UnitTests/Models/WorkItemInfoTests.cs`
  - 測試 Team 屬性可正確設定和讀取
  - 測試 Team 為 null 不影響 Validate() 方法

- [ ] T026 [P] [US3] 建立 AzureDevOps Work Item 過濾邏輯的單元測試在 `src/tests/ReleaseSync.Infrastructure.UnitTests/Platforms/AzureDevOpsWorkItemRepositoryTests.cs`
  - 測試從 Area Path 提取團隊名稱
  - 測試過濾掉未對應團隊的 Work Item
  - 測試保留已對應團隊的 Work Item
  - 測試空 TeamMapping 不過濾任何 Work Item
  - 測試 Team 屬性使用 DisplayName

- [ ] T027 [P] [US3] 建立 Work Item 過濾完整流程的整合測試在 `src/tests/ReleaseSync.Integration.Tests/EndToEnd/WorkItemFilteringWorkflowTests.cs`
  - 測試完整的 Work Item 過濾流程
  - 測試 Work Item 被過濾時 PR/MR 仍保留 (FR-012)
  - 測試 Work Item 關聯顯示為空
  - 測試日誌記錄警告訊息
  - 測試向後相容性 (空 TeamMapping)

### Implementation for User Story 3

- [X] T028 [US3] 在 AzureDevOpsApiClient 中加入從 Area Path 提取團隊名稱的邏輯在 `src/ReleaseSync.Infrastructure/Platforms/AzureDevOps/AzureDevOpsWorkItemRepository.cs`
  - 從 System.AreaPath 欄位提取團隊名稱
  - 處理不同的 Area Path 格式
  - 加入錯誤處理 (Area Path 不存在或格式異常)

- [X] T029 [US3] 在 AzureDevOpsWorkItemRepository 加入過濾邏輯在 `src/ReleaseSync.Infrastructure/Platforms/AzureDevOps/AzureDevOpsWorkItemRepository.cs`
  - 注入 ITeamMappingService
  - 在 GetWorkItemAsync 中提取團隊名稱
  - 呼叫 HasMapping 判斷是否過濾
  - 使用 GetDisplayName 設定 WorkItemInfo.Team 屬性
  - 記錄過濾統計到日誌
  - 處理 Work Item 被過濾時的邏輯 (返回 null 或不加入清單)

- [ ] T030 [US3] 確保 Work Item 被過濾時 PR/MR 仍保留在 `src/ReleaseSync.Application/Services/SyncOrchestrator.cs`
  - 檢視現有關聯邏輯
  - 確認 Work Item 清單為空時 PR/MR 不被移除
  - 加入日誌警告當 Work Item 全部被過濾

- [ ] T031 [US3] 執行所有 User Story 3 測試並確保通過

**Checkpoint**: 所有使用者故事現在應該都能獨立運作

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 影響多個使用者故事的改進和完善

- [ ] T032 [P] 更新 quickstart.md 加入實際配置範例和使用說明 (已完成)
- [ ] T033 [P] 建立 contracts/team-mapping-schema.json JSON Schema 文件 (已完成)
- [ ] T034 執行完整的端到端測試驗證所有三個使用者故事協同運作
- [ ] T035 [P] 效能測試驗證過濾邏輯影響 <5%
  - 測試 100 筆 UserMapping 和 100 筆 PR/MR
  - 測試 20 筆 TeamMapping 和 50 筆 Work Item
  - 記錄執行時間並與未啟用過濾時比較

- [X] T036 [P] 檢查所有新增的公開 API 是否包含 XML 文件註解 (繁體中文)
- [X] T037 程式碼審查確認符合 KISS 原則和 SOLID 原則
- [ ] T038 執行所有測試套件確保無回歸 (需要 .NET 8.0 runtime)
- [ ] T039 更新 CLAUDE.md 文件 (已完成)
- [ ] T040 準備 Pull Request 說明文件

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無依賴 - 可立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - 阻塞所有使用者故事
- **User Stories (Phase 3-5)**: 所有都依賴 Foundational 階段完成
  - 使用者故事可以平行進行 (如果有足夠人力)
  - 或依優先順序依序進行 (P1 → P2 → P3)
- **Polish (Phase 6)**: 依賴所有期望的使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始 - 不依賴其他故事
- **User Story 2 (P2)**: Foundational 完成後可開始 - 不依賴其他故事,但為 US3 的前置
- **User Story 3 (P3)**: Foundational 完成後可開始,理想上 US2 完成後再開始 (需要 TeamMapping 配置結構)

### Within Each User Story

- 測試必須先寫並在實作前失敗 (Test-First Development)
- 介面和配置模型優先
- 服務實作其次
- Repository 過濾邏輯最後
- 故事完成後再進入下一個優先級

### Parallel Opportunities

- Phase 1 所有任務標記 [P] 可平行執行
- Phase 2 所有任務標記 [P] 可平行執行
- Foundational 階段完成後,所有使用者故事可平行開始 (如有團隊容量)
- 每個使用者故事內標記 [P] 的測試可平行執行
- 每個使用者故事內標記 [P] 的實作任務可平行執行 (不同檔案)
- 不同使用者故事可由不同團隊成員平行工作

---

## Parallel Example: User Story 1

```bash
# 平行啟動 User Story 1 的所有測試:
Task: "建立 UserMappingService HasMapping 方法的單元測試"
Task: "建立 GitLab PR 過濾邏輯的單元測試"
Task: "建立 BitBucket PR 過濾邏輯的單元測試"
Task: "建立完整過濾流程的整合測試"

# 平行啟動 User Story 1 的 Repository 過濾邏輯 (測試通過後):
Task: "在 GitLabPullRequestRepository 加入過濾邏輯"
Task: "在 BitBucketPullRequestRepository 加入過濾邏輯"
```

---

## Parallel Example: User Story 2

```bash
# 平行啟動 User Story 2 的所有測試和實作:
Task: "建立 TeamMapping 配置載入的單元測試"
Task: "建立配置契約測試"
Task: "建立 TeamMappingService 的單元測試"

# 實作任務 (測試通過後):
Task: "實作 TeamMappingService"  # 核心邏輯
Task: "更新 appsettings.example.json"  # 獨立任務,可平行
```

---

## Parallel Example: User Story 3

```bash
# 平行啟動 User Story 3 的所有測試:
Task: "建立 WorkItemInfo Team 屬性的單元測試"
Task: "建立 AzureDevOps Work Item 過濾邏輯的單元測試"
Task: "建立 Work Item 過濾完整流程的整合測試"

# 實作任務依序進行 (有依賴):
Task: "在 AzureDevOpsApiClient 中加入從 Area Path 提取團隊名稱的邏輯"  # 先
Task: "在 AzureDevOpsWorkItemRepository 加入過濾邏輯"  # 依賴前者
Task: "確保 Work Item 被過濾時 PR/MR 仍保留"  # 最後
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational (關鍵 - 阻塞所有故事)
3. 完成 Phase 3: User Story 1
4. **停止並驗證**: 獨立測試 User Story 1
5. 如果就緒可部署/展示

### Incremental Delivery

1. 完成 Setup + Foundational → 基礎就緒
2. 加入 User Story 1 → 獨立測試 → 部署/展示 (MVP!)
3. 加入 User Story 2 → 獨立測試 → 部署/展示
4. 加入 User Story 3 → 獨立測試 → 部署/展示
5. 每個故事都增加價值且不破壞先前的故事

### Parallel Team Strategy

多位開發者情況:

1. 團隊一起完成 Setup + Foundational
2. Foundational 完成後:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: 準備 User Story 3 (等待 US2 完成)
3. 故事獨立完成並整合

---

## Task Count Summary

- **Phase 1 (Setup)**: 3 tasks
- **Phase 2 (Foundational)**: 5 tasks
- **Phase 3 (User Story 1 - P1)**: 9 tasks (4 tests + 5 implementation)
- **Phase 4 (User Story 2 - P2)**: 7 tasks (2 tests + 5 implementation)
- **Phase 5 (User Story 3 - P3)**: 7 tasks (3 tests + 4 implementation)
- **Phase 6 (Polish)**: 9 tasks
- **Total**: 40 tasks

**Parallel Opportunities Identified**:
- 8 tasks in Setup + Foundational can run in parallel
- 12 test tasks across all user stories can run in parallel (within their story)
- 3 user stories can be developed in parallel after Foundational phase

**MVP Scope** (User Story 1 only):
- Setup (3) + Foundational (5) + User Story 1 (9) = **17 tasks for MVP**

---

## Notes

- [P] 任務 = 不同檔案,無依賴關係
- [Story] 標籤將任務對應到特定使用者故事以便追蹤
- 每個使用者故事都應該可獨立完成和測試
- 先驗證測試失敗再開始實作 (Test-First Development)
- 每個任務或邏輯群組完成後提交
- 在任何檢查點停止以獨立驗證故事
- 避免: 模糊任務、相同檔案衝突、破壞獨立性的跨故事依賴

---

## Constitution Compliance Checkpoints

依據專案章程,在實作過程中持續驗證:

✅ **Test-First Development**: 所有測試任務 (T009-T012, T018-T019, T023, T025-T027) 都在實作前完成
✅ **XML Documentation**: T036 確保所有公開 API 包含繁體中文文件註解
✅ **KISS & SOLID**: T037 程式碼審查確認遵循原則
✅ **Performance**: T035 驗證效能影響 <5%
✅ **Backward Compatibility**: 測試任務包含向後相容性驗證 (空 Mapping 情況)
