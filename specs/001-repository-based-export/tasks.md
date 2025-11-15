# Tasks: Repository-Based Export Format

**Input**: Design documents from `/specs/001-repository-based-export/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: 本專案強制執行 TDD,所有測試必須在實作前完成並經使用者審核。

**Organization**: 任務按 User Story 分組,實現獨立實作與測試。根據 research.md,User Story 2 (統計資訊) 標記為 Out of Scope,因此僅實作 US1 與 US3。

## Format: `[ID] [P?] [Story] Description [Build: ✅/❌] [Tests: ✅/❌]`

- **[P]**: 可並行執行 (不同檔案、無相依性)
- **[Story]**: 所屬 User Story (US1, US3)
- **[Build]**: 完成後程式碼是否可成功建置 (✅ 可建置 / ❌ 不可建置 / ⚠ 部分建置)
- **[Tests]**: 完成後單元測試是否全部通過 (✅ 通過 / ❌ 不通過 / ⚠ 部分通過 / N/A 無測試)
- 所有描述包含精確檔案路徑

**重要**: 根據專案憲章要求,每個階段性任務完成後必須標註建置與測試狀態,確保開發過程的品質與可追溯性。

## Path Conventions

專案結構:
- **Source**: `src/ReleaseSync.Application/`, `src/ReleaseSync.Domain/`
- **Tests**: `src/tests/ReleaseSync.Application.UnitTests/`
- **Solution**: `src/src.sln`

---

## Phase 1: Setup (共用基礎設施)

**Purpose**: 專案初始化與基本結構準備

- [ ] T001 驗證專案結構與相依性完整性 (執行 `dotnet build src/src.sln`) [Build: ✅] [Tests: N/A]
- [ ] T002 檢查現有 DTO 結構,識別需要移除的 WorkItemCentricOutputDto 相關程式碼 [Build: ✅] [Tests: N/A]

---

## Phase 2: Foundational (阻塞性前置條件)

**Purpose**: 無 - 此功能為資料格式轉換,無阻塞性基礎設施需求

**⚠️ 注意**: 此功能僅變更 Application Layer 的 DTO 結構,無需建立新的基礎設施。

**Checkpoint**: 可直接進入 User Story 實作階段

---

## Phase 3: User Story 1 + 3 - Repository 分組匯出與 Work Item 關聯 (Priority: P1 + P3) 🎯 MVP

**合併理由**: US1 與 US3 技術上不可分離 - DTO 結構本身已包含 Work Item 欄位,分開實作會導致重複工作。

**Goal**:
- **US1**: 將 Pull Request 資料以 Repository 為主體分組匯出,每個 Repository 包含其關聯的所有 Pull Requests
- **US3**: 當 Pull Request 關聯到 Work Item 時,保留此資訊在 Repository 分組結構下

**Independent Test**:
1. 執行 `dotnet run --project src/ReleaseSync.Console -- sync -s 2025-01-01 -e 2025-01-31 --gitlab --bitbucket -o output.json`
2. 檢查 `output.json` 檔案結構:
   - 頂層為 `{ "startDate", "endDate", "repositories": [...] }`
   - 每個 repository 物件包含 `repositoryName`、`platform`、`pullRequests`
   - Repository 名稱已提取 (如 `owner/repo` → `repo`)
   - PR 包含 `workItem` 欄位 (可為 `null`)
3. 驗證不同平台的相同名稱 repository 獨立分組

### Tests for User Story 1 + 3 (TDD - 必須先完成並經使用者審核) ⚠️

**TDD 流程**:
1. 撰寫測試 (T003-T018)
2. 確認測試失敗 (紅燈)
3. **等待使用者審核測試程式碼**
4. 審核通過後才能進行實作 (T019-T028)

#### Repository 分組邏輯測試

- [ ] T003 [P] [US1] 建立 `RepositoryBasedOutputDtoTests.cs` 測試類別在 `src/tests/ReleaseSync.Application.UnitTests/DTOs/RepositoryBasedOutputDtoTests.cs` [Build: ✅] [Tests: ❌ (Red)]
- [ ] T004 [P] [US1] 撰寫測試: `FromSyncResult_EmptyData_ReturnsEmptyRepositories` - 驗證空資料處理 [Build: ✅] [Tests: ❌ (Red)]
- [ ] T005 [P] [US1] 撰寫測試: `FromSyncResult_SingleRepository_GroupsCorrectly` - 驗證單一 Repository 分組 [Build: ✅] [Tests: ❌ (Red)]
- [ ] T006 [P] [US1] 撰寫測試: `FromSyncResult_MultipleRepositories_GroupsByNameAndPlatform` - 驗證多 Repository 依名稱與平台分組 [Build: ✅] [Tests: ❌ (Red)]
- [ ] T007 [P] [US1] 撰寫測試: `FromSyncResult_SameName_DifferentPlatforms_CreatesSeperateGroups` - 驗證相同名稱但不同平台的 repository 分開處理 [Build: ✅] [Tests: ❌ (Red)]

#### Repository 名稱提取測試

- [ ] T008 [P] [US1] 撰寫測試: `ExtractRepositoryName_WithSlash_ReturnsLastPart` - 驗證 `owner/repo` → `repo` [Build: ✅] [Tests: ❌ (Red)]
- [ ] T009 [P] [US1] 撰寫測試: `ExtractRepositoryName_WithoutSlash_ReturnsOriginal` - 驗證 `standalone` → `standalone` [Build: ✅] [Tests: ❌ (Red)]
- [ ] T010 [P] [US1] 撰寫測試: `ExtractRepositoryName_MultipleSlashes_ReturnsLastPart` - 驗證 `org/team/project` → `project` [Build: ✅] [Tests: ❌ (Red)]
- [ ] T011 [P] [US1] 撰寫測試: `ExtractRepositoryName_EmptyString_ReturnsEmpty` - 驗證邊界情況 [Build: ✅] [Tests: ❌ (Red)]

#### Work Item 關聯測試

- [ ] T012 [P] [US3] 撰寫測試: `FromSyncResult_WorkItemNull_SetsWorkItemToNull` - 驗證無 Work Item 時明確設為 `null` [Build: ✅] [Tests: ❌ (Red)]
- [ ] T013 [P] [US3] 撰寫測試: `FromSyncResult_WorkItemExists_MapsCorrectly` - 驗證 Work Item 正確對映 [Build: ✅] [Tests: ❌ (Red)]
- [ ] T014 [P] [US3] 撰寫測試: `FromSyncResult_WorkItemWithNullTeam_HandlesGracefully` - 驗證 Team 為 null 時處理 [Build: ✅] [Tests: ❌ (Red)]

#### 日期與資料完整性測試

- [ ] T015 [P] [US1] 撰寫測試: `FromSyncResult_PreservesDateRange` - 驗證 StartDate 與 EndDate 正確保留 [Build: ✅] [Tests: ❌ (Red)]
- [ ] T016 [P] [US1] 撰寫測試: `FromSyncResult_PreservesAllPullRequestFields` - 驗證所有 PR 欄位正確對映 [Build: ✅] [Tests: ❌ (Red)]

#### 效能測試

- [ ] T017 [P] [US1] 撰寫測試: `FromSyncResult_LargeDataset_CompletesWithin5Seconds` - 驗證 2000 PRs 處理效能 [Build: ✅] [Tests: ❌ (Red)]

#### JSON 序列化測試

- [ ] T018 [US1] 更新 `JsonFileExporterTests.cs` 在 `src/tests/ReleaseSync.Application.UnitTests/Exporters/JsonFileExporterTests.cs`:
  - `ExportAsync_RepositoryBasedDto_SerializesCorrectly` - 驗證新 DTO 序列化
  - `ExportAsync_RepositoryBasedDto_HandlesNullWorkItem` - 驗證 null Work Item 序列化為 JSON `null`
  - `ExportAsync_RepositoryBasedDto_UsesCamelCase` - 驗證 camelCase 命名
  - `ExportAsync_RepositoryBasedDto_HandlesChineseCharacters` - 驗證中文字元不跳脫 [Build: ✅] [Tests: ❌ (Red)]

**🛑 TDD Checkpoint**: 測試撰寫完成,**必須經使用者審核後才能進行實作**

---

### Implementation for User Story 1 + 3 (審核通過後執行)

#### DTO 類別實作

- [ ] T019 [P] [US1+US3] 建立 `RepositoryBasedOutputDto.cs` 在 `src/ReleaseSync.Application/DTOs/RepositoryBasedOutputDto.cs`:
  - 定義 `RepositoryBasedOutputDto` record (頂層 DTO)
  - 定義 `RepositoryGroupDto` record (Repository 分組)
  - 定義 `RepositoryPullRequestDto` record (簡化 PR DTO)
  - 定義 `PullRequestWorkItemDto` record (Work Item DTO)
  - 加入完整繁體中文 XML 註解 (`<summary>`, `<param>`, `<returns>`)
  - 使用 `required` 關鍵字標註必填欄位
  - 使用 `?` 修飾符標註 nullable 欄位 [Build: ❌] [Tests: ❌]

- [ ] T020 [US1+US3] 實作 `FromSyncResult` 靜態方法在 `RepositoryBasedOutputDto.cs`:
  - 使用 LINQ `GroupBy` 依 `(RepositoryName, Platform)` 分組
  - 對每個分組建立 `RepositoryGroupDto`
  - 對映 Pull Requests 到 `RepositoryPullRequestDto`
  - 加入 inline comment 說明轉換邏輯 [Build: ❌] [Tests: ❌]

- [ ] T021 [US1] 實作 `ExtractRepositoryName` 私有靜態方法在 `RepositoryBasedOutputDto.cs`:
  - 使用 `String.Split('/')` 並取最後元素 (`parts[^1]`)
  - 加入 inline comment 說明提取規則
  - Defensive programming: 處理無 `/` 的情況 [Build: ❌] [Tests: ❌]

- [ ] T022 [US3] 實作 `FromWorkItemDto` 靜態方法在 `PullRequestWorkItemDto.cs`:
  - 對映 `WorkItemDto` 欄位到簡化 DTO
  - 加入 XML 註解說明與 `WorkItemDto` 的差異 [Build: ❌] [Tests: ❌]

- [ ] T023 [US1+US3] 加入 `JsonPropertyName` 屬性到所有 DTO 欄位,確保 camelCase 序列化 [Build: ✅] [Tests: ⚠ (部分通過)]

#### DTO 測試驗證

- [ ] T024 [US1+US3] 執行所有 `RepositoryBasedOutputDtoTests` 測試,確認綠燈 [Build: ✅] [Tests: ✅ (Green)]

#### 匯出器調整

- [ ] T025 [US1+US3] 調整 `IResultExporter.cs` 在 `src/ReleaseSync.Application/Exporters/IResultExporter.cs`:
  - 將泛型參數從 `WorkItemCentricOutputDto` 改為 `RepositoryBasedOutputDto`
  - 更新 XML 註解 [Build: ❌] [Tests: N/A]

- [ ] T026 [US1+US3] 調整 `JsonFileExporter.cs` 在 `src/ReleaseSync.Application/Exporters/JsonFileExporter.cs`:
  - 更新 `ExportAsync` 方法接受 `RepositoryBasedOutputDto`
  - 確保 JSON 序列化設定不變 (WriteIndented, CamelCase, UnsafeRelaxedJsonEscaping)
  - 更新 XML 註解 [Build: ❌] [Tests: ❌]

- [ ] T027 [US1+US3] 執行 `JsonFileExporterTests`,確認所有測試通過 [Build: ✅] [Tests: ✅]

#### SyncOrchestrator 整合

- [ ] T028 [US1+US3] 調整 `SyncOrchestrator.cs` 在 `src/ReleaseSync.Application/Services/SyncOrchestrator.cs`:
  - 移除 `WorkItemCentricOutputDto.FromSyncResult` 呼叫
  - 改用 `RepositoryBasedOutputDto.FromSyncResult`
  - 更新相關 XML 註解 [Build: ✅] [Tests: ✅]

#### 清理舊程式碼

- [ ] T029 [P] [US1+US3] 刪除 `WorkItemCentricOutputDto.cs` 在 `src/ReleaseSync.Application/DTOs/WorkItemCentricOutputDto.cs` 及其內嵌類別:
  - `WorkItemCentricOutputDto`
  - `WorkItemWithPullRequestsDto`
  - `SimplifiedPullRequestDto` [Build: ✅] [Tests: ✅]

- [ ] T030 [P] [US1+US3] 刪除 `WorkItemCentricOutputDtoTests.cs` 在 `src/tests/ReleaseSync.Application.UnitTests/DTOs/WorkItemCentricOutputDtoTests.cs` (如果存在) [Build: ✅] [Tests: ✅]

#### 完整建置與測試驗證

- [ ] T031 [US1+US3] 執行完整解決方案建置: `dotnet build src/src.sln` [Build: ✅] [Tests: N/A]

- [ ] T032 [US1+US3] 執行所有單元測試: `dotnet test src/src.sln` 確認所有測試通過 [Build: ✅] [Tests: ✅]

- [ ] T033 [US1+US3] 執行端對端測試: `dotnet run --project src/ReleaseSync.Console -- sync -s 2025-01-01 -e 2025-01-31 --gitlab -o test-output.json` 並驗證輸出格式符合 JSON Schema [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 1 + 3 完整實作完成,所有測試通過,可獨立驗證功能

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: 跨 User Story 的改進與優化

- [ ] T034 [P] 更新專案文件:
  - 確認 `specs/001-repository-based-export/quickstart.md` 與實作一致
  - 確認 JSON Schema (`contracts/repository-based-output-schema.json`) 與 DTO 定義一致 [Build: ✅] [Tests: N/A]

- [ ] T035 [P] 程式碼品質檢查:
  - 所有公開成員包含完整 XML 註解
  - 複雜邏輯包含 inline comment
  - 符合 SOLID 原則
  - 無 compiler warnings [Build: ✅] [Tests: ✅]

- [ ] T036 執行 `quickstart.md` 驗證:
  - 依照 quickstart.md 的步驟執行同步命令
  - 驗證輸出 JSON 與文件範例一致
  - 確認 Google Sheets 整合範例可執行 [Build: ✅] [Tests: ✅]

- [ ] T037 安全性檢查:
  - 確認日誌不輸出敏感資訊 (Token, Password)
  - 確認輸出 JSON 不包含內部實作細節 [Build: ✅] [Tests: ✅]

- [ ] T038 效能驗證:
  - 使用 `Stopwatch` 測量 2000 PRs 的匯出時間
  - 確認在 5 秒內完成 (目標: < 1 秒)
  - 若超過 1 秒記錄警告日誌 [Build: ✅] [Tests: ✅]

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無相依性 - 可立即開始
- **Foundational (Phase 2)**: 無 - 直接跳過
- **User Story 1 + 3 (Phase 3)**: 依賴 Setup 完成
  - **TDD 子階段**: T003-T018 (測試撰寫) → **使用者審核** → T019-T033 (實作)
- **Polish (Phase 4)**: 依賴 Phase 3 完成

### User Story Dependencies

**注意**: US1 與 US3 技術上合併為單一階段,因為:
- DTO 結構本身包含 Work Item 欄位 (US3)
- 分開實作會導致重複變更相同檔案
- 符合 KISS 原則,避免不必要的複雜度

### Within User Story Phase

1. **測試階段 (T003-T018)**:
   - 所有標記 `[P]` 的測試可並行撰寫
   - 完成後確認紅燈 (測試失敗)
   - **等待使用者審核**

2. **實作階段 (T019-T033)**:
   - T019-T022 (DTO 類別) 可並行實作
   - T023 (JSON 屬性) 依賴 T019-T022
   - T024 (DTO 測試驗證) 依賴 T023
   - T025-T027 (匯出器調整) 依賴 T024
   - T028 (SyncOrchestrator) 依賴 T027
   - T029-T030 (清理) 可並行執行,依賴 T028
   - T031-T033 (驗證) 依賴 T029-T030

### Parallel Opportunities

#### 測試階段並行組

```bash
# Group 1: Repository 分組測試 (T003-T007)
Task T004: "FromSyncResult_EmptyData_ReturnsEmptyRepositories"
Task T005: "FromSyncResult_SingleRepository_GroupsCorrectly"
Task T006: "FromSyncResult_MultipleRepositories_GroupsByNameAndPlatform"
Task T007: "FromSyncResult_SameName_DifferentPlatforms_CreatesSeperateGroups"

# Group 2: 名稱提取測試 (T008-T011)
Task T008: "ExtractRepositoryName_WithSlash_ReturnsLastPart"
Task T009: "ExtractRepositoryName_WithoutSlash_ReturnsOriginal"
Task T010: "ExtractRepositoryName_MultipleSlashes_ReturnsLastPart"
Task T011: "ExtractRepositoryName_EmptyString_ReturnsEmpty"

# Group 3: Work Item 測試 (T012-T014)
Task T012: "FromSyncResult_WorkItemNull_SetsWorkItemToNull"
Task T013: "FromSyncResult_WorkItemExists_MapsCorrectly"
Task T014: "FromSyncResult_WorkItemWithNullTeam_HandlesGracefully"

# Group 4: 資料完整性測試 (T015-T017)
Task T015: "FromSyncResult_PreservesDateRange"
Task T016: "FromSyncResult_PreservesAllPullRequestFields"
Task T017: "FromSyncResult_LargeDataset_CompletesWithin5Seconds"
```

#### 實作階段並行組

```bash
# Group 1: DTO 類別實作 (T019-T022)
Task T019: "建立 RepositoryBasedOutputDto.cs (所有 record 定義)"
Task T020: "實作 FromSyncResult 方法"
Task T021: "實作 ExtractRepositoryName 方法"
Task T022: "實作 FromWorkItemDto 方法"

# Group 2: 清理階段 (T029-T030)
Task T029: "刪除 WorkItemCentricOutputDto.cs"
Task T030: "刪除 WorkItemCentricOutputDtoTests.cs"

# Group 3: Polish 階段 (T034-T035)
Task T034: "更新專案文件"
Task T035: "程式碼品質檢查"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 3 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 3: User Story 1 + 3
   - **Sub-phase 1**: 測試撰寫 (T003-T018) → 確認紅燈 → **使用者審核**
   - **Sub-phase 2**: 實作 (T019-T033) → 確認綠燈
3. **STOP and VALIDATE**: 獨立測試 User Story 1 + 3 功能
4. Complete Phase 4: Polish (T034-T038)
5. 部署/展示

### TDD Workflow (CRITICAL)

**專案強制執行 TDD,必須嚴格遵循以下流程:**

1. **Red Phase (T003-T018)**:
   - 撰寫所有測試程式碼
   - 執行測試確認失敗 (紅燈)
   - **提交測試程式碼供使用者審核**

2. **User Review**:
   - 使用者檢查測試涵蓋率
   - 使用者確認測試案例正確性
   - 使用者批准後才能進行實作

3. **Green Phase (T019-T028)**:
   - 實作 DTO 類別與轉換邏輯
   - 執行測試確認通過 (綠燈)
   - 達到 80% 以上覆蓋率目標

4. **Refactor Phase (T029-T038)**:
   - 清理舊程式碼
   - 程式碼品質檢查
   - 效能驗證

### Incremental Delivery

由於 US2 (統計資訊) Out of Scope,此功能為單一增量交付:

1. Setup → Foundation (無) → US1+US3 → Polish → **Deploy (MVP)**

### Single Developer Strategy

1. 完成 Setup (T001-T002)
2. 撰寫所有測試 (T003-T018) - 可使用並行工具加速
3. 等待使用者審核
4. 審核通過後,依序實作:
   - DTO 類別 (T019-T023)
   - 驗證測試 (T024)
   - 匯出器調整 (T025-T027)
   - SyncOrchestrator 整合 (T028)
   - 清理 (T029-T030)
   - 驗證 (T031-T033)
5. 完成 Polish (T034-T038)

---

## Notes

- **[P] 標記**: 不同檔案、無相依性,可並行執行
- **[Story] 標記**: 任務所屬 User Story,方便追溯
- **TDD 強制執行**: 必須先寫測試、確認紅燈、經使用者審核後才能實作
- **測試覆蓋率目標**: 核心轉換邏輯達到 90% 以上 (高於憲章要求的 80%)
- **建置狀態追蹤**: 每個任務完成後標註 Build 與 Tests 狀態
- **Commit 策略**: 每個任務或邏輯群組完成後提交
- **Checkpoint 驗證**: 在每個 Checkpoint 獨立驗證功能
- **避免事項**: 模糊任務、相同檔案衝突、跨 Story 相依性

---

## Summary

**總任務數**: 38 個任務
- Phase 1 (Setup): 2 個任務
- Phase 2 (Foundational): 0 個任務 (無需求)
- Phase 3 (US1+US3 Tests): 16 個任務 (TDD Red Phase)
- Phase 3 (US1+US3 Implementation): 15 個任務 (TDD Green/Refactor Phase)
- Phase 4 (Polish): 5 個任務

**並行機會**:
- 測試階段: 17 個並行任務 (T004-T017, T018 部分)
- 實作階段: 4 個並行任務 (T019-T022)
- 清理階段: 2 個並行任務 (T029-T030)
- Polish 階段: 2 個並行任務 (T034-T035)

**MVP 範圍**: User Story 1 + 3 (合併實作)

**預估時間** (單人開發):
- 測試撰寫: 4-6 小時
- 使用者審核: 1-2 小時
- 實作: 4-6 小時
- 驗證與 Polish: 2-3 小時
- **總計**: 11-17 小時

**格式驗證**: ✅ 所有任務遵循 checklist 格式 (checkbox, ID, labels, file paths)
