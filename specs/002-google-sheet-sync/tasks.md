# Tasks: Google Sheet 同步匯出功能

**Input**: Design documents from `/specs/002-google-sheet-sync/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: 根據專案憲章 TDD 強制執行要求，本任務清單包含測試任務。所有測試必須先寫並確認失敗後才進行實作。

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description [Build: ✅/❌] [Tests: ✅/❌]`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- **[Build]**: 完成後程式碼是否可成功建置 (✅ 可建置 / ❌ 不可建置 / ⚠ 部分建置)
- **[Tests]**: 完成後單元測試是否全部通過 (✅ 通過 / ❌ 不通過 / ⚠ 部分通過 / N/A 無測試)
- Include exact file paths in descriptions

**重要**: 根據專案憲章要求，每個階段性任務完成後必須標註建置與測試狀態，確保開發過程的品質與可追溯性。

## Path Conventions

本專案使用 Clean Architecture 分層結構：
- **Application Layer**: `src/ReleaseSync.Application/`
- **Infrastructure Layer**: `src/ReleaseSync.Infrastructure/`
- **Console Layer**: `src/ReleaseSync.Console/`
- **Tests**: `src/tests/ReleaseSync.*.UnitTests/`

---

## Phase 1: Setup (NuGet Packages & Project Configuration)

**Purpose**: 安裝必要的 NuGet 套件並建立基礎專案結構

- [X] T001 安裝 Google.Apis.Sheets.v4 NuGet 套件至 ReleaseSync.Infrastructure 專案 [Build: ✅] [Tests: N/A]
- [X] T002 安裝 Polly NuGet 套件至 ReleaseSync.Infrastructure 專案 [Build: ✅] [Tests: N/A]
- [X] T003 [P] 建立 src/ReleaseSync.Infrastructure/GoogleSheet/ 目錄結構 [Build: ✅] [Tests: N/A]
- [X] T004 [P] 建立 src/ReleaseSync.Infrastructure/Exceptions/ 目錄結構 [Build: ✅] [Tests: N/A]
- [X] T005 [P] 建立 src/ReleaseSync.Application/Mappers/ 目錄結構 [Build: ✅] [Tests: N/A]
- [X] T006 [P] 建立 src/ReleaseSync.Application/Models/ 目錄結構 (若不存在) [Build: ✅] [Tests: N/A]

---

## Phase 2: Foundational (Core Value Objects & Exceptions)

**Purpose**: 建立所有使用者故事共用的基礎元件

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Foundational Tests (TDD - Red Phase)

- [X] T007 [P] 建立 GoogleSheetColumnMapping 單元測試 in src/tests/ReleaseSync.Application.UnitTests/Models/GoogleSheetColumnMappingTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T008 [P] 建立 SheetRowData 單元測試 in src/tests/ReleaseSync.Application.UnitTests/Models/SheetRowDataTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T009 [P] 建立 GoogleSheetSyncResult 單元測試 in src/tests/ReleaseSync.Application.UnitTests/Models/GoogleSheetSyncResultTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Foundational Implementation

- [X] T010 [P] 實作 GoogleSheetColumnMapping Value Object in src/ReleaseSync.Application/Models/GoogleSheetColumnMapping.cs [Build: ✅] [Tests: ✅]
- [X] T011 [P] 實作 SheetRowData Value Object in src/ReleaseSync.Application/Models/SheetRowData.cs [Build: ✅] [Tests: ✅]
- [X] T012 [P] 實作 SheetSyncOperation Value Object in src/ReleaseSync.Application/Models/SheetSyncOperation.cs [Build: ✅] [Tests: ✅]
- [X] T013 [P] 實作 GoogleSheetSyncResult Value Object in src/ReleaseSync.Application/Models/GoogleSheetSyncResult.cs [Build: ✅] [Tests: ✅]
- [X] T014 [P] 實作自訂例外類型 in src/ReleaseSync.Infrastructure/Exceptions/GoogleSheetExceptions.cs [Build: ✅] [Tests: N/A]
- [X] T015 實作 GoogleSheetSettings 組態模型 in src/ReleaseSync.Infrastructure/Configuration/GoogleSheetSettings.cs [Build: ✅] [Tests: N/A]
- [X] T016 更新 appsettings.json 新增 GoogleSheet 組態區塊 in src/ReleaseSync.Console/appsettings.json [Build: ✅] [Tests: N/A]

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - 啟用 Google Sheet 同步匯出 (Priority: P1) 🎯 MVP

**Goal**: 使用者能透過 `--google-sheet` 命令列參數啟用 Google Sheet 同步功能

**Independent Test**: 執行 `sync --google-sheet` 命令，驗證系統能正確識別參數並嘗試同步

### Tests for User Story 1 (TDD - Red Phase) ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T017 [P] [US1] 建立 GoogleSheetRowParser 單元測試 in src/tests/ReleaseSync.Infrastructure.UnitTests/GoogleSheet/GoogleSheetRowParserTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T018 [P] [US1] 建立 GoogleSheetDataMapper 單元測試 in src/tests/ReleaseSync.Application.UnitTests/Mappers/GoogleSheetDataMapperTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T019 [P] [US1] 建立 GoogleSheetSyncService 單元測試 in src/tests/ReleaseSync.Application.UnitTests/Services/GoogleSheetSyncServiceTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [X] T020 [P] [US1] 建立 SyncCommandHandler Google Sheet 整合測試 in src/tests/ReleaseSync.Console.UnitTests/Handlers/SyncCommandHandlerGoogleSheetTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 1

- [X] T021 [P] [US1] 實作 IGoogleSheetRowParser 介面 in src/ReleaseSync.Application/Services/IGoogleSheetRowParser.cs [Build: ✅] [Tests: ❌]
- [X] T022 [US1] 實作 GoogleSheetRowParser in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetRowParser.cs [Build: ✅] [Tests: ✅]
- [X] T023 [P] [US1] 實作 IGoogleSheetDataMapper 介面 in src/ReleaseSync.Application/Mappers/IGoogleSheetDataMapper.cs [Build: ✅] [Tests: ❌]
- [X] T024 [US1] 實作 GoogleSheetDataMapper in src/ReleaseSync.Application/Mappers/GoogleSheetDataMapper.cs [Build: ✅] [Tests: ✅]
- [X] T025 [P] [US1] 實作 IGoogleSheetApiClient 介面 in src/ReleaseSync.Application/Services/IGoogleSheetApiClient.cs [Build: ✅] [Tests: ❌]
- [X] T026 [US1] 實作 GoogleSheetApiClient (含 Polly Retry) in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ⚠️ (Mock 測試)]
- [X] T027 [P] [US1] 實作 IGoogleSheetSyncService 介面 in src/ReleaseSync.Application/Services/IGoogleSheetSyncService.cs [Build: ✅] [Tests: ❌]
- [X] T028 [US1] 實作 GoogleSheetSyncService in src/ReleaseSync.Application/Services/GoogleSheetSyncService.cs [Build: ✅] [Tests: ✅]
- [X] T029 [US1] 實作 GoogleSheetServiceExtensions DI 擴展 in src/ReleaseSync.Infrastructure/DependencyInjection/GoogleSheetServiceExtensions.cs [Build: ✅] [Tests: N/A]
- [X] T030 [US1] 修改 SyncCommandOptions 新增 EnableGoogleSheet 屬性 in src/ReleaseSync.Console/Handlers/SyncCommandOptions.cs [Build: ✅] [Tests: ❌]
- [X] T031 [US1] 修改 SyncCommand 新增 --google-sheet 參數 in src/ReleaseSync.Console/Commands/SyncCommand.cs [Build: ✅] [Tests: ❌]
- [X] T032 [US1] 修改 SyncCommandHandler 整合 GoogleSheetSyncService in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs [Build: ✅] [Tests: ✅]
- [X] T033 [US1] 修改 Program.cs 新增 AddGoogleSheetServices 呼叫 in src/ReleaseSync.Console/Program.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 1 完成 - 系統可透過 --google-sheet 參數觸發同步流程

---

## Phase 4: User Story 2 - 設定 Google Sheet 匯出目標 (Priority: P1)

**Goal**: 使用者能夠指定 Google Sheet ID 和工作表名稱

**Independent Test**: 在 appsettings.json 設定不同的 Sheet ID 和工作表名稱，驗證系統能正確讀取並連接

### Tests for User Story 2 (TDD - Red Phase) ⚠️

- [ ] T034 [P] [US2] 建立 GoogleSheetSettings 組態綁定測試 in src/tests/ReleaseSync.Infrastructure.UnitTests/Configuration/GoogleSheetSettingsTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [ ] T035 [P] [US2] 建立 GoogleSheetApiClient 連線驗證測試 in src/tests/ReleaseSync.Infrastructure.UnitTests/GoogleSheet/GoogleSheetApiClientConnectionTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 2

- [ ] T036 [US2] 完善 GoogleSheetApiClient 的 SpreadsheetExistsAsync 方法 in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ✅]
- [ ] T037 [US2] 完善 GoogleSheetApiClient 的 SheetExistsAsync 方法 in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ✅]
- [ ] T038 [US2] 實作 ValidateConfigurationAsync 方法驗證 Sheet ID 和工作表 in src/ReleaseSync.Application/Services/GoogleSheetSyncService.cs [Build: ✅] [Tests: ✅]
- [ ] T039 [US2] 新增組態缺失時的錯誤訊息處理 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 2 完成 - 系統能驗證並連接至指定的 Google Sheet

---

## Phase 5: User Story 3 - Service Account 憑證管理 (Priority: P1)

**Goal**: 使用者能安全地設定 Google Service Account 憑證

**Independent Test**: 提供有效的 Service Account JSON 憑證，驗證系統能成功驗證並連線

### Tests for User Story 3 (TDD - Red Phase) ⚠️

- [ ] T040 [P] [US3] 建立憑證檔案驗證測試 in src/tests/ReleaseSync.Infrastructure.UnitTests/GoogleSheet/GoogleSheetCredentialTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [ ] T041 [P] [US3] 建立憑證錯誤處理測試 in src/tests/ReleaseSync.Application.UnitTests/Services/GoogleSheetSyncServiceCredentialTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 3

- [ ] T042 [US3] 實作 GoogleSheetApiClient 的 AuthenticateAsync 方法 in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ✅]
- [ ] T043 [US3] 實作憑證檔案存在性檢查 in src/ReleaseSync.Application/Services/GoogleSheetSyncService.cs [Build: ✅] [Tests: ✅]
- [ ] T044 [US3] 新增憑證無效時的詳細錯誤訊息 in src/ReleaseSync.Infrastructure/Exceptions/GoogleSheetExceptions.cs [Build: ✅] [Tests: ✅]
- [ ] T045 [US3] 確保憑證路徑不被記錄到日誌 (安全性) in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 3 完成 - 系統能安全地管理 Service Account 憑證

---

## Phase 6: User Story 4 - 同步執行狀態回饋 (Priority: P2)

**Goal**: 使用者在同步過程中能看到執行狀態與摘要

**Independent Test**: 執行同步命令，觀察 console 輸出是否包含進度與摘要資訊

### Tests for User Story 4 (TDD - Red Phase) ⚠️

- [ ] T046 [P] [US4] 建立同步結果摘要測試 in src/tests/ReleaseSync.Application.UnitTests/Services/GoogleSheetSyncServiceResultTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for User Story 4

- [ ] T047 [US4] 新增同步進度日誌記錄 in src/ReleaseSync.Application/Services/GoogleSheetSyncService.cs [Build: ✅] [Tests: ✅]
- [ ] T048 [US4] 實作同步完成摘要輸出 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs [Build: ✅] [Tests: ✅]
- [ ] T049 [US4] 新增 Google Sheet URL 產生邏輯 in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: User Story 4 完成 - 系統提供清楚的執行狀態與摘要

---

## Phase 7: Core Sync Logic Implementation

**Purpose**: 實作核心同步邏輯（UK 比對、row 更新/插入）

### Tests for Core Logic (TDD - Red Phase) ⚠️

- [ ] T050 [P] 建立 UK 比對邏輯測試 in src/tests/ReleaseSync.Application.UnitTests/Services/GoogleSheetSyncServiceUkMatchingTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [ ] T051 [P] 建立 Row 更新邏輯測試 in src/tests/ReleaseSync.Application.UnitTests/Services/GoogleSheetSyncServiceRowUpdateTests.cs [Build: ✅] [Tests: ❌ (Red)]
- [ ] T052 [P] 建立 Row 插入邏輯測試 in src/tests/ReleaseSync.Application.UnitTests/Services/GoogleSheetSyncServiceRowInsertTests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for Core Logic

- [ ] T053 實作 GoogleSheetApiClient 的 ReadSheetDataAsync 方法 in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ✅]
- [ ] T054 實作 UK 比對邏輯 (WorkItemId + RepositoryName) in src/ReleaseSync.Application/Services/GoogleSheetSyncService.cs [Build: ✅] [Tests: ✅]
- [ ] T055 實作現有 row 更新邏輯 (合併 Authors 和 PR URLs) in src/ReleaseSync.Application/Services/GoogleSheetSyncService.cs [Build: ✅] [Tests: ✅]
- [ ] T056 實作新 row 插入位置計算邏輯 in src/ReleaseSync.Application/Services/GoogleSheetSyncService.cs [Build: ✅] [Tests: ✅]
- [ ] T057 實作 GoogleSheetApiClient 的 BatchUpdateAsync 方法 in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetApiClient.cs [Build: ✅] [Tests: ✅]
- [ ] T058 實作超連結格式化 (Feature URL) in src/ReleaseSync.Infrastructure/GoogleSheet/GoogleSheetRowParser.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: 核心同步邏輯完成 - 系統能正確比對 UK、更新/插入 rows

---

## Phase 8: Data Source Conditions

**Purpose**: 實作資料來源條件判斷（條件一：即時同步；條件二：從 JSON 檔案）

### Tests for Data Source Conditions (TDD - Red Phase) ⚠️

- [ ] T059 [P] 建立條件一邏輯測試 (平台啟用 + AZDO + GoogleSheet) in src/tests/ReleaseSync.Console.UnitTests/Handlers/SyncCommandHandlerCondition1Tests.cs [Build: ✅] [Tests: ❌ (Red)]
- [ ] T060 [P] 建立條件二邏輯測試 (JSON 檔案 + GoogleSheet) in src/tests/ReleaseSync.Console.UnitTests/Handlers/SyncCommandHandlerCondition2Tests.cs [Build: ✅] [Tests: ❌ (Red)]

### Implementation for Data Source Conditions

- [ ] T061 實作條件一判斷邏輯 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs [Build: ✅] [Tests: ✅]
- [ ] T062 實作條件二判斷邏輯 (讀取並反序列化 JSON) in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs [Build: ✅] [Tests: ✅]
- [ ] T063 實作 JSON 檔案存在性與反序列化驗證 in src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs [Build: ✅] [Tests: ✅]

**Checkpoint**: 資料來源條件完成 - 系統支援兩種同步模式

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: 最終整合、錯誤處理與文件更新

- [ ] T064 [P] 驗證所有憲章合規性 (XML 註解、繁體中文) [Build: ✅] [Tests: ✅]
- [ ] T065 [P] 確認部分失敗容錯機制 (Google Sheet 失敗不影響 JSON 匯出) [Build: ✅] [Tests: ✅]
- [ ] T066 執行整合測試確認端對端流程 [Build: ✅] [Tests: ✅]
- [ ] T067 [P] 更新 README.md 新增 Google Sheet 同步說明 [Build: ✅] [Tests: N/A]
- [ ] T068 執行 quickstart.md 驗證所有設定步驟 [Build: ✅] [Tests: N/A]
- [ ] T069 執行 dotnet build 確認無警告 [Build: ✅] [Tests: ✅]
- [ ] T070 執行 dotnet test 確認所有測試通過且覆蓋率達標 [Build: ✅] [Tests: ✅]

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User Story 1 (P1): Core sync enablement
  - User Story 2 (P1): Sheet configuration
  - User Story 3 (P1): Credential management
  - User Story 4 (P2): Status feedback
- **Core Logic (Phase 7)**: Depends on User Stories 1-3 completion
- **Data Source Conditions (Phase 8)**: Depends on Core Logic
- **Polish (Phase 9)**: Depends on all phases completion

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - Foundation of sync feature
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Independent of US1 but logically follows
- **User Story 3 (P1)**: Can start after Foundational (Phase 2) - Independent of US1/US2 but logically follows
- **User Story 4 (P2)**: Depends on US1/US2/US3 - Needs sync flow to be functional

### Within Each User Story

- Tests (TDD) MUST be written and FAIL before implementation
- Interfaces before implementations
- Models before services
- Infrastructure before application
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks (T003-T006) can run in parallel
- All Foundational tests (T007-T009) can run in parallel
- All Foundational models (T010-T016) can run in parallel
- All tests within a user story marked [P] can run in parallel
- Interfaces marked [P] can run in parallel

---

## Parallel Example: Phase 2 Foundational

```bash
# Launch all foundational tests together (TDD Red Phase):
Task: "T007 GoogleSheetColumnMapping 單元測試"
Task: "T008 SheetRowData 單元測試"
Task: "T009 GoogleSheetSyncResult 單元測試"

# After tests written, launch all models together (TDD Green Phase):
Task: "T010 GoogleSheetColumnMapping Value Object"
Task: "T011 SheetRowData Value Object"
Task: "T012 SheetSyncOperation Value Object"
Task: "T013 GoogleSheetSyncResult Value Object"
Task: "T014 自訂例外類型"
```

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (TDD Red Phase):
Task: "T017 GoogleSheetRowParser 單元測試"
Task: "T018 GoogleSheetDataMapper 單元測試"
Task: "T019 GoogleSheetSyncService 單元測試"
Task: "T020 SyncCommandHandler Google Sheet 整合測試"

# Launch all interfaces together:
Task: "T021 IGoogleSheetRowParser 介面"
Task: "T023 IGoogleSheetDataMapper 介面"
Task: "T025 IGoogleSheetApiClient 介面"
Task: "T027 IGoogleSheetSyncService 介面"
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 Only)

1. Complete Phase 1: Setup (NuGet packages)
2. Complete Phase 2: Foundational (Value Objects, Exceptions)
3. Complete Phase 3: User Story 1 (Sync enablement)
4. Complete Phase 4: User Story 2 (Sheet configuration)
5. Complete Phase 5: User Story 3 (Credential management)
6. **STOP and VALIDATE**: Test basic sync flow end-to-end
7. Deploy/demo if ready (MVP!)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test `--google-sheet` parameter → MVP Phase 1
3. Add User Story 2 → Test Sheet ID/Name configuration → MVP Phase 2
4. Add User Story 3 → Test Service Account credentials → MVP Phase 3
5. Add User Story 4 → Test status feedback → Enhanced UX
6. Add Core Logic → Test UK matching and row operations → Full functionality
7. Add Data Source Conditions → Test both sync modes → Complete feature
8. Polish phase → Production ready

### TDD Cycle Per Task

1. **Red**: Write test that fails (compile error or assertion failure)
2. **User Review**: Present test to user for approval
3. **Green**: Implement minimum code to pass test
4. **Refactor**: Clean up code while tests pass
5. **Repeat**: Move to next task

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- **TDD 強制執行**: 所有測試必須先寫並確認失敗後才進行實作
- **使用者審核**: 測試撰寫完成後必須經使用者審核才能進行實作
- Commit after each task or logical group (建議每個 [Build: ✅] 後 commit)
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- **XML 註解**: 所有公開成員必須包含繁體中文 XML 註解
- **錯誤處理**: 使用自訂例外類型，提供明確錯誤訊息
