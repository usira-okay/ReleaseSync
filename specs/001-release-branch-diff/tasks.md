# Tasks: Release Branch 差異比對功能

**Input**: Design documents from `/specs/001-release-branch-diff/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/configuration-schema.json

**Tests**: 根據專案憲章 TDD 強制執行原則，所有核心功能必須先寫測試再實作。

**Organization**: 任務按 User Story 分組，以便獨立實作與測試每個 Story。

## Format: `[ID] [P?] [Story?] Description [Build: ✅/❌] [Tests: ✅/❌]`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- **[Build]**: 完成後程式碼是否可成功建置 (✅ 可建置 / ❌ 不可建置 / ⚠ 部分建置)
- **[Tests]**: 完成後單元測試是否全部通過 (✅ 通過 / ❌ 不通過 / ⚠ 部分通過 / N/A 無測試)

**重要**: 根據專案憲章要求，每個階段性任務完成後必須標註建置與測試狀態。

---

## Phase 1: Setup (專案結構與基礎)

**Purpose**: 專案初始化與測試專案設定

- [X] T001 建立測試專案結構 src/tests/ReleaseSync.Domain.UnitTests/Models/ [Build: ✅] [Tests: N/A]
- [X] T002 [P] 建立測試專案結構 src/tests/ReleaseSync.Infrastructure.UnitTests/Configuration/ [Build: ✅] [Tests: N/A]

---

## Phase 2: Foundational (Domain Layer 值物件與列舉)

**Purpose**: 核心 Domain 模型，所有 User Story 都依賴這些基礎元件

**⚠️ CRITICAL**: 所有 User Story 都需要這些元件完成後才能開始

### Tests for Foundational (TDD: Red Phase)

> **NOTE: 根據憲章 TDD 原則，先寫測試並確保測試失敗**

- [X] T003 [P] 撰寫 FetchMode 列舉單元測試 src/tests/ReleaseSync.Domain.UnitTests/Models/FetchModeTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T004 [P] 撰寫 ReleaseBranchName 值物件單元測試（格式驗證、日期解析、比較運算）src/tests/ReleaseSync.Domain.UnitTests/Models/ReleaseBranchNameTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for Foundational (TDD: Green Phase)

- [X] T005 [P] 實作 FetchMode 列舉 src/ReleaseSync.Domain/Models/FetchMode.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T006 [P] 實作 ReleaseBranchName 值物件（含 Parse、TryParse、FromDate、比較運算子）src/ReleaseSync.Domain/Models/ReleaseBranchName.cs [Build: ✅] [Tests: ✅ (Green)]

**Checkpoint**: Domain Layer 基礎元件完成，所有測試通過

---

## Phase 3: User Story 4 - 簡化 TargetBranch 配置 (Priority: P2) 🎯 先行實作

**Goal**: 將 TargetBranches 陣列改為 TargetBranch 單一值，確保向後相容

**Independent Test**: 修改 appsettings.json 中的 TargetBranch 為單一字串值，驗證系統能正確讀取並使用該設定

**NOTE**: 雖然此 Story 優先順序為 P2，但因為是配置結構變更的基礎，且其他 Story 都依賴新配置結構，故先行實作。

### Tests for User Story 4 (TDD: Red Phase)

- [X] T007 [P] [US4] 撰寫 SyncOptionsSettings 單元測試 src/tests/ReleaseSync.Infrastructure.UnitTests/Configuration/SyncOptionsSettingsTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T008 [P] [US4] 撰寫 GitLabProjectSettings 單元測試（TargetBranch 單一值、向後相容 TargetBranches）src/tests/ReleaseSync.Infrastructure.UnitTests/Configuration/GitLabProjectSettingsTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T009 [P] [US4] 撰寫 BitBucketProjectSettings 單元測試（TargetBranch 單一值、向後相容 TargetBranches）src/tests/ReleaseSync.Infrastructure.UnitTests/Configuration/BitBucketProjectSettingsTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 4 (TDD: Green Phase)

- [X] T010 [P] [US4] 建立 SyncOptionsSettings 配置類別 src/ReleaseSync.Infrastructure/Configuration/SyncOptionsSettings.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T011 [P] [US4] 修改 GitLabProjectSettings（TargetBranches → TargetBranch + 新增 FetchMode、ReleaseBranch、StartDate、EndDate 覆寫屬性）src/ReleaseSync.Infrastructure/Configuration/GitLabSettings.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T012 [P] [US4] 修改 BitBucketProjectSettings（TargetBranches → TargetBranch + 新增 FetchMode、ReleaseBranch、StartDate、EndDate 覆寫屬性）src/ReleaseSync.Infrastructure/Configuration/BitBucketSettings.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T013 [US4] 更新 GitLabServiceExtensions 註冊 SyncOptionsSettings src/ReleaseSync.Infrastructure/DependencyInjection/GitLabServiceExtensions.cs [Build: ✅] [Tests: ✅]
- [X] T014 [US4] 更新 BitBucketServiceExtensions 註冊 SyncOptionsSettings src/ReleaseSync.Infrastructure/DependencyInjection/BitBucketServiceExtensions.cs [Build: ✅] [Tests: ✅]
- [X] T015 [US4] 修改 GitLabService 使用 TargetBranch 單一值 src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabService.cs [Build: ✅] [Tests: ✅]
- [X] T016 [US4] 修改 BitBucketService 使用 TargetBranch 單一值 src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketService.cs [Build: ✅] [Tests: ✅]
- [X] T017 [US4] 更新 appsettings.json 配置結構（SyncOptions 節點、TargetBranch 單一值）src/ReleaseSync.Console/appsettings.json [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 4 完成，配置結構已更新，向後相容測試通過

---

## Phase 4: User Story 2 - 使用時間範圍抓取 PR 資訊 (Priority: P1) 🎯 MVP

**Goal**: 保留使用時間範圍抓取 PR 的方式，確保向後相容性

**Independent Test**: 設定 `FetchMode=DateRange` 並指定 StartDate/EndDate，驗證系統正確回傳該時間範圍內的 PR 資訊

**NOTE**: 此功能為現有功能的延續與強化，確保 FetchMode=DateRange 正常運作。

### Tests for User Story 2 (TDD: Red Phase)

- [X] T018 [P] [US2] 撰寫 EffectiveProjectConfig 配置覆寫解析單元測試（DateRange 模式）src/tests/ReleaseSync.Infrastructure.UnitTests/Configuration/EffectiveProjectConfigTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 2 (TDD: Green Phase)

- [X] T019 [P] [US2] 建立 EffectiveProjectConfig 記錄類別（配置覆寫解析邏輯）src/ReleaseSync.Infrastructure/Configuration/EffectiveProjectConfig.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T020 [US2] 修改 SyncRequest DTO 新增 FetchMode 屬性 src/ReleaseSync.Application/DTOs/SyncRequest.cs [Build: ✅] [Tests: ✅]
- [X] T021 [US2] 修改 BasePlatformService 支援 EffectiveProjectConfig 解析 src/ReleaseSync.Infrastructure/Platforms/BasePlatformService.cs [Build: ✅] [Tests: ✅]
- [X] T022 [US2] 修改 SyncOrchestrator 使用 FetchMode 與配置覆寫邏輯 src/ReleaseSync.Application/Services/SyncOrchestrator.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 2 完成，DateRange 模式（向後相容）正常運作

---

## Phase 5: User Story 1 - 使用 Release Branch 比對取得待發布變更 (Priority: P1) 🎯 核心功能

**Goal**: 指定 release branch，系統自動比對與目標分支的差異，了解哪些變更尚未進入 release

**Independent Test**: 設定 `FetchMode=ReleaseBranch` 並指定最新版 release branch，驗證系統正確回傳 TargetBranch 有但 Release Branch 沒有的 PR 資訊

### Tests for User Story 1 (TDD: Red Phase)

- [X] T023 [P] [US1] 撰寫 GitLabApiClient.GetBranchesAsync 單元測試 src/tests/ReleaseSync.Infrastructure.UnitTests/Platforms/GitLab/GitLabApiClientTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T024 [P] [US1] 撰寫 GitLabApiClient.CompareBranchesAsync 單元測試 src/tests/ReleaseSync.Infrastructure.UnitTests/Platforms/GitLab/GitLabApiClientTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T025 [P] [US1] 撰寫 BitBucketApiClient.GetBranchesAsync 單元測試 src/tests/ReleaseSync.Infrastructure.UnitTests/Platforms/BitBucket/BitBucketApiClientTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T026 [P] [US1] 撰寫 BitBucketApiClient.CompareBranchesAsync 單元測試 src/tests/ReleaseSync.Infrastructure.UnitTests/Platforms/BitBucket/BitBucketApiClientTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 1 (TDD: Green Phase)

- [X] T027 [P] [US1] 建立 BranchInfo 記錄類別 src/ReleaseSync.Infrastructure/Platforms/Models/BranchInfo.cs [Build: ✅] [Tests: ✅]
- [X] T028 [P] [US1] 建立 BranchCompareResult 記錄類別 src/ReleaseSync.Infrastructure/Platforms/Models/BranchCompareResult.cs [Build: ✅] [Tests: ✅]
- [X] T029 [P] [US1] 建立 CommitInfo 記錄類別 src/ReleaseSync.Infrastructure/Platforms/Models/CommitInfo.cs [Build: ✅] [Tests: ✅]
- [X] T030 [US1] 在 GitLabApiClient 新增 GetBranchesAsync 方法（呼叫 /repository/branches API）src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabApiClient.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T031 [US1] 在 GitLabApiClient 新增 CompareBranchesAsync 方法（呼叫 /repository/compare API）src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabApiClient.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T032 [US1] 在 BitBucketApiClient 新增 GetBranchesAsync 方法（呼叫 /refs/branches API）src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketApiClient.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T033 [US1] 在 BitBucketApiClient 新增 CompareBranchesAsync 方法（呼叫 /diffstat API）src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketApiClient.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T034 [US1] 修改 GitLabPullRequestRepository 新增 GetByReleaseBranchAsync 方法（最新版比對 TargetBranch）src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabPullRequestRepository.cs [Build: ✅] [Tests: ✅]
- [X] T035 [US1] 修改 BitBucketPullRequestRepository 新增 GetByReleaseBranchAsync 方法（最新版比對 TargetBranch）src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketPullRequestRepository.cs [Build: ✅] [Tests: ✅]
- [X] T036 [US1] 建立 ReleaseBranchNotFoundException 自訂例外類別 src/ReleaseSync.Domain/Exceptions/ReleaseBranchNotFoundException.cs [Build: ✅] [Tests: ✅]
- [X] T037 [US1] 更新 IPlatformService 介面支援 FetchMode 參數 src/ReleaseSync.Domain/Services/IPlatformService.cs [Build: ✅] [Tests: ✅]
- [X] T038 [US1] 更新 GitLabService 根據 FetchMode 選擇抓取策略 src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabService.cs [Build: ✅] [Tests: ✅]
- [X] T039 [US1] 更新 BitBucketService 根據 FetchMode 選擇抓取策略 src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketService.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 1 完成，Release Branch 比對（最新版）正常運作

---

## Phase 6: User Story 3 - 比對歷史版本 Release Branch 差異 (Priority: P2)

**Goal**: 比對兩個不同版本的 release branch 差異，追溯特定 release 之間的變更

**Independent Test**: 設定較舊版本的 release branch（如 release/20260113），驗證系統自動找到下一版 release branch（如 release/20260120）並進行比對

### Tests for User Story 3 (TDD: Red Phase)

- [X] T040 [P] [US3] 撰寫 ReleaseBranchVersionResolver 單元測試（找出下一版 release branch）src/tests/ReleaseSync.Infrastructure.UnitTests/Services/ReleaseBranchVersionResolverTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 3 (TDD: Green Phase)

- [X] T041 [US3] 建立 IReleaseBranchVersionResolver 介面與實作（判斷是否為最新版、找出下一版）src/ReleaseSync.Infrastructure/Services/ReleaseBranchVersionResolver.cs [Build: ✅] [Tests: ✅ (Green)]
- [X] T042 [US3] 修改 GitLabPullRequestRepository.GetByReleaseBranchAsync 支援歷史版比對 src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabPullRequestRepository.cs [Build: ✅] [Tests: ✅]
- [X] T043 [US3] 修改 BitBucketPullRequestRepository.GetByReleaseBranchAsync 支援歷史版比對 src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketPullRequestRepository.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 3 完成，歷史版 Release Branch 比對正常運作

---

## Phase 7: Console Layer - CLI 整合

**Purpose**: 命令列介面整合，讓使用者能透過 CLI 使用新功能

- [X] T044 修改 SyncCommand 新增 --release-branch 參數 src/ReleaseSync.Console/Commands/SyncCommand.cs [Build: ✅] [Tests: ✅]
- [X] T045 修改 SyncCommand 新增 --fetch-mode 參數 src/ReleaseSync.Console/Commands/SyncCommand.cs [Build: ✅] [Tests: ✅]
- [X] T046 更新 Program.cs 註冊 SyncOptionsSettings 與相關服務 src/ReleaseSync.Console/Program.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: CLI 整合完成，可透過命令列使用所有新功能

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: 跨 User Story 的改進與完善

- [X] T047 [P] 撰寫整合測試（GitLab ReleaseBranch 模式）src/tests/ReleaseSync.Integration.Tests/EndToEnd/GitLabReleaseBranchIntegrationTests.cs [Build: ✅] [Tests: ✅]
- [X] T048 [P] 撰寫整合測試（BitBucket ReleaseBranch 模式）src/tests/ReleaseSync.Integration.Tests/EndToEnd/BitBucketReleaseBranchIntegrationTests.cs [Build: ✅] [Tests: ✅]
- [X] T049 [P] 更新 XML 註解確保所有公開類別與方法都有繁體中文說明 [Build: ✅] [Tests: ✅]
- [X] T050 [P] 執行 quickstart.md 驗證所有使用案例 [Build: ✅] [Tests: ✅]
- [X] T051 驗證向後相容性（現有 DateRange 模式使用者無需修改配置）[Build: ✅] [Tests: ✅]

**Checkpoint**: 所有任務完成，功能實作完畢

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    │
    ▼
Phase 2: Foundational (Domain Layer)
    │
    ├─────────────────────────────────────────────┐
    ▼                                             │
Phase 3: US4 - 簡化 TargetBranch 配置             │
    │                                             │
    ▼                                             │
Phase 4: US2 - 時間範圍抓取 (向後相容) ◄───────────┘
    │
    ▼
Phase 5: US1 - Release Branch 比對 (最新版) 🎯 核心
    │
    ▼
Phase 6: US3 - Release Branch 比對 (歷史版)
    │
    ▼
Phase 7: CLI 整合
    │
    ▼
Phase 8: Polish
```

### User Story Dependencies

| Story | 依賴 | 說明 |
|-------|------|------|
| US4 (P2) | Phase 2 | 配置結構變更為基礎，先行實作 |
| US2 (P1) | US4 | 需要新配置結構支援 FetchMode |
| US1 (P1) | US2 | 需要 FetchMode 選擇邏輯 |
| US3 (P2) | US1 | 擴展 US1 的 Release Branch 比對邏輯 |

### Parallel Opportunities

**Phase 2 內部**:
```bash
# 可同時執行（不同檔案）
T003: FetchMode 測試
T004: ReleaseBranchName 測試
---
T005: FetchMode 實作
T006: ReleaseBranchName 實作
```

**Phase 3 內部**:
```bash
# 可同時執行（不同檔案）
T007: SyncOptionsSettings 測試
T008: GitLabProjectSettings 測試
T009: BitBucketProjectSettings 測試
---
T010: SyncOptionsSettings 實作
T011: GitLabSettings 修改
T012: BitBucketSettings 修改
```

**Phase 5 內部**:
```bash
# 可同時執行（不同平台）
T023-T024: GitLab API 測試
T025-T026: BitBucket API 測試
---
T030-T031: GitLab API 實作
T032-T033: BitBucket API 實作
---
T034: GitLab Repository
T035: BitBucket Repository
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 + 4)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (Domain Layer)
3. Complete Phase 3: US4 - 配置結構（必須先完成）
4. Complete Phase 4: US2 - DateRange 模式（確保向後相容）
5. Complete Phase 5: US1 - ReleaseBranch 模式（核心功能）
6. **STOP and VALIDATE**: 測試 MVP 功能

### Incremental Delivery

1. Setup + Foundational → 基礎架構就緒
2. Add US4 → 配置結構更新 → 驗證向後相容
3. Add US2 → DateRange 模式強化 → 驗證現有功能
4. Add US1 → ReleaseBranch 核心功能 → **MVP 完成！**
5. Add US3 → 歷史版比對進階功能
6. CLI + Polish → 完整功能交付

---

## Summary

| 指標 | 數值 |
|------|------|
| 總任務數 | 51 |
| Phase 數 | 8 |
| User Story 數 | 4 |
| 可並行任務 | 27 (標記 [P]) |
| TDD 測試任務 | 12 |
| 實作任務 | 39 |

### 每個 User Story 任務數

| User Story | 測試任務 | 實作任務 | 總計 |
|------------|----------|----------|------|
| US4 (配置簡化) | 3 | 8 | 11 |
| US2 (DateRange) | 1 | 4 | 5 |
| US1 (ReleaseBranch 最新版) | 4 | 13 | 17 |
| US3 (ReleaseBranch 歷史版) | 1 | 3 | 4 |

### MVP 範圍建議

**建議 MVP**: Phase 1-5（共 39 任務）
- 包含 US4、US2、US1
- 交付核心 Release Branch 比對功能
- 確保向後相容

**後續增量**: Phase 6-8（共 12 任務）
- US3 歷史版比對
- CLI 完整整合
- 品質強化

---

## Notes

- [P] 任務 = 不同檔案，無相依性，可並行執行
- [Story] 標籤對應 spec.md 中的 User Story
- 每個 User Story 可獨立完成與測試
- 遵循 TDD：先寫測試 (Red) → 實作 (Green) → 重構
- 每個任務完成後確認建置與測試狀態
- 任何 Checkpoint 都可停下來驗證該 Story 的獨立功能
