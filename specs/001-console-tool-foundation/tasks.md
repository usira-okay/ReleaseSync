# Tasks: ReleaseSync Console 工具基礎架構

**Input**: Design documents from `/specs/001-console-tool-foundation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: 本功能遵循 Test-First Development 原則,所有測試將在實作前撰寫。

**Organization**: 任務依使用者故事組織,以支援獨立實作與測試。

## Format: `[ID] [P?] [Story] Description`
- **[P]**: 可平行執行 (不同檔案,無依賴關係)
- **[Story]**: 此任務屬於哪個使用者故事 (例如 US1, US2, US3)
- 描述中包含精確的檔案路徑

## Path Conventions
- **Single Console Project**: `src/ReleaseSync.Console/`, `tests/ReleaseSync.Console.UnitTests/`
- 路徑依據 plan.md 中的專案結構定義

---

## Phase 1: Setup (專案初始化)

**Purpose**: 建立專案結構與基本設定

- [ ] T001 建立 Solution 檔案與 src/tests 目錄結構
- [ ] T002 建立 ReleaseSync.Console 專案 (src/ReleaseSync.Console/ReleaseSync.Console.csproj)
- [ ] T003 建立 ReleaseSync.Console.UnitTests 測試專案 (tests/ReleaseSync.Console.UnitTests/ReleaseSync.Console.UnitTests.csproj)
- [ ] T004 [P] 安裝 System.CommandLine NuGet 套件到 Console 專案
- [ ] T005 [P] 安裝 Serilog 與 Serilog.Sinks.Console 到 Console 專案
- [ ] T006 [P] 安裝 Microsoft.Extensions.DependencyInjection 到 Console 專案
- [ ] T007 [P] 安裝 Microsoft.Extensions.Configuration 與 Configuration.Json 到 Console 專案
- [ ] T008 [P] 安裝 Microsoft.Extensions.Hosting 到 Console 專案
- [ ] T009 [P] 安裝 xUnit, FluentAssertions, Moq 到測試專案
- [ ] T010 設定專案編譯選項 (TreatWarningsAsErrors, GenerateDocumentationFile, Nullable) 於 Console 專案 .csproj
- [ ] T011 設定測試專案編譯選項於 UnitTests 專案 .csproj
- [ ] T012 [P] 建立 .editorconfig 於 repository 根目錄
- [ ] T013 [P] 建立 appsettings.json 於 src/ReleaseSync.Console/
- [ ] T014 [P] 建立 secure.json.example 於 src/ReleaseSync.Console/
- [ ] T015 [P] 建立空的 secure.json 並加入 .gitignore
- [ ] T016 建立子目錄結構 (Services/, Extensions/) 於 src/ReleaseSync.Console/
- [ ] T017 建立子目錄結構 (Services/, Fixtures/, TestHelpers/) 於 tests/ReleaseSync.Console.UnitTests/

**Checkpoint**: 專案結構與套件安裝完成,可開始實作服務

---

## Phase 2: Foundational (核心基礎設施)

**Purpose**: 建立所有使用者故事都依賴的核心基礎設施

**⚠️ CRITICAL**: 必須完成此階段才能開始任何使用者故事的實作

- [ ] T018 建立 ICommandLineParserService 介面於 src/ReleaseSync.Console/Services/ (依據 contracts/ICommandLineParserService.cs)
- [ ] T019 [P] 建立 IDataFetchingService 介面於 src/ReleaseSync.Console/Services/ (依據 contracts/IDataFetchingService.cs)
- [ ] T020 [P] 建立 IApplicationRunner 介面於 src/ReleaseSync.Console/Services/ (依據 contracts/IApplicationRunner.cs)
- [ ] T021 建立 ServiceCollectionExtensions 類別於 src/ReleaseSync.Console/Extensions/ (定義 AddApplicationServices 擴充方法)

**Checkpoint**: 核心介面與擴充方法已定義,使用者故事實作可平行進行

---

## Phase 3: User Story 1 - 應用程式基本結構與啟動 (Priority: P1) 🎯 MVP

**Goal**: 建立可執行的 Console 應用程式,能夠正常啟動、顯示訊息並結束

**Independent Test**: 執行應用程式,驗證能夠啟動、輸出日誌、拋出 NotImplementedException 並正常結束

### Tests for User Story 1 (Test-First Development)

**NOTE: 先撰寫這些測試,確保測試失敗後再實作**

- [ ] T022 [P] [US1] 撰寫 Program.cs 啟動測試於 tests/ReleaseSync.Console.UnitTests/ (驗證應用程式能編譯與執行)
- [ ] T023 [P] [US1] 撰寫 ServiceCollectionExtensions 測試於 tests/ReleaseSync.Console.UnitTests/Extensions/ServiceCollectionExtensionsTests.cs (驗證服務註冊)

### Implementation for User Story 1

- [ ] T024 [US1] 實作 Program.cs 於 src/ReleaseSync.Console/ (包含 Serilog 設定、Host Builder、服務註冊、ApplicationRunner 呼叫)
- [ ] T025 [US1] 實作 ServiceCollectionExtensions.AddApplicationServices 於 src/ReleaseSync.Console/Extensions/ServiceCollectionExtensions.cs (註冊三個服務)
- [ ] T026 [US1] 驗證應用程式能在 3 秒內啟動並輸出基本資訊 (Success Criteria SC-001)
- [ ] T027 [US1] 驗證應用程式能通過 dotnet build 無錯誤與警告 (Success Criteria SC-002)
- [ ] T028 [US1] 驗證應用程式能在 Windows, Linux, macOS 上正常啟動 (Success Criteria SC-006)

**Checkpoint**: User Story 1 完成 - 應用程式能正常啟動與結束

---

## Phase 4: User Story 2 - 指令參數解析服務入口 (Priority: P2)

**Goal**: 建立參數解析服務的介面與實作,拋出 NotImplementedException

**Independent Test**: 呼叫 CommandLineParserService.Parse 方法,驗證拋出 NotImplementedException 並包含清楚的訊息

### Tests for User Story 2 (Test-First Development)

**NOTE: 先撰寫這些測試,確保測試失敗後再實作**

- [ ] T029 [US2] 撰寫 CommandLineParserService 測試於 tests/ReleaseSync.Console.UnitTests/Services/CommandLineParserServiceTests.cs (驗證拋出 NotImplementedException)

### Implementation for User Story 2

- [ ] T030 [US2] 實作 CommandLineParserService 類別於 src/ReleaseSync.Console/Services/CommandLineParserService.cs (實作 ICommandLineParserService,拋出 NotImplementedException)
- [ ] T031 [US2] 加入 XML 文件註解 (繁體中文) 於 CommandLineParserService
- [ ] T032 [US2] 驗證服務正確拋出 NotImplementedException (Success Criteria SC-003)
- [ ] T033 [US2] 驗證例外訊息清楚說明功能尚未實作 (Functional Requirement FR-005)

**Checkpoint**: User Story 2 完成 - 參數解析服務入口已預留

---

## Phase 5: User Story 3 - 資料拉取服務入口 (Priority: P2)

**Goal**: 建立資料拉取服務的介面與實作,拋出 NotImplementedException

**Independent Test**: 呼叫 DataFetchingService.FetchDataAsync 方法,驗證拋出 NotImplementedException 並包含清楚的訊息

### Tests for User Story 3 (Test-First Development)

**NOTE: 先撰寫這些測試,確保測試失敗後再實作**

- [ ] T034 [US3] 撰寫 DataFetchingService 測試於 tests/ReleaseSync.Console.UnitTests/Services/DataFetchingServiceTests.cs (驗證拋出 NotImplementedException)

### Implementation for User Story 3

- [ ] T035 [US3] 實作 DataFetchingService 類別於 src/ReleaseSync.Console/Services/DataFetchingService.cs (實作 IDataFetchingService,拋出 NotImplementedException)
- [ ] T036 [US3] 實作 ApplicationRunner 類別於 src/ReleaseSync.Console/Services/ApplicationRunner.cs (實作 IApplicationRunner,拋出 NotImplementedException)
- [ ] T037 [US3] 加入 XML 文件註解 (繁體中文) 於 DataFetchingService 與 ApplicationRunner
- [ ] T038 [US3] 驗證服務正確拋出 NotImplementedException (Success Criteria SC-003)
- [ ] T039 [US3] 驗證 FetchDataAsync 為非同步方法且支援 CancellationToken (Functional Requirement FR-004)

**Checkpoint**: User Story 3 完成 - 資料拉取服務入口已預留

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 改善影響多個使用者故事的品質面向

- [ ] T040 [P] 驗證程式碼符合 .NET 編碼規範 (Success Criteria SC-004)
- [ ] T041 [P] 執行靜態程式碼分析工具檢查 (如 Roslyn Analyzers)
- [ ] T042 [P] 驗證所有公開 API 包含 XML 文件註解 (Constitution Principle X)
- [ ] T043 [P] 驗證 Program.cs 僅包含服務註冊與啟動邏輯,無業務邏輯 (Constitution Principle IX)
- [ ] T044 驗證其他開發者能在 10 分鐘內理解基本架構 (Success Criteria SC-005) - 請團隊成員審閱
- [ ] T045 [P] 執行 quickstart.md 中的所有步驟驗證完整性
- [ ] T046 執行所有單元測試確保通過 (dotnet test)
- [ ] T047 建立 Git commit 並推送到遠端 (遵循 Constitution 的 Git 提交指南)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無依賴 - 可立即開始
- **Foundational (Phase 2)**: 依賴 Setup 完成 - **阻擋所有使用者故事**
- **User Stories (Phase 3-5)**: 所有使用者故事依賴 Foundational 完成
  - User Stories 可平行進行 (如果有多位開發者)
  - 或依優先順序循序進行 (P1 → P2 → P2)
- **Polish (Phase 6)**: 依賴所有期望的使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: 可在 Foundational 完成後開始 - 無其他故事依賴
- **User Story 2 (P2)**: 可在 Foundational 完成後開始 - 無其他故事依賴,可獨立測試
- **User Story 3 (P2)**: 可在 Foundational 完成後開始 - 無其他故事依賴,可獨立測試

### Within Each User Story

- 測試必須先撰寫並失敗,再進行實作
- 實作完成後驗證測試通過
- 故事完成後再進行下一個優先故事

### Parallel Opportunities

- **Setup Phase**: T004-T009 (套件安裝), T012-T015 (設定檔案) 可平行執行
- **Foundational Phase**: T018-T020 (介面定義) 可平行執行
- **User Story 1**: T022-T023 (測試) 可平行撰寫
- **Different User Stories**: US2 與 US3 可由不同團隊成員平行開發

---

## Parallel Example: Setup Phase

```bash
# 平行安裝所有 NuGet 套件:
Task: "安裝 System.CommandLine NuGet 套件"
Task: "安裝 Serilog 與 Serilog.Sinks.Console"
Task: "安裝 Microsoft.Extensions.DependencyInjection"
Task: "安裝 Microsoft.Extensions.Configuration"
Task: "安裝 xUnit, FluentAssertions, Moq"

# 平行建立所有設定檔案:
Task: "建立 .editorconfig"
Task: "建立 appsettings.json"
Task: "建立 secure.json.example"
```

## Parallel Example: Foundational Phase

```bash
# 平行建立所有服務介面:
Task: "建立 ICommandLineParserService 介面"
Task: "建立 IDataFetchingService 介面"
Task: "建立 IApplicationRunner 介面"
```

## Parallel Example: User Story 1

```bash
# 平行撰寫所有測試:
Task: "撰寫 Program.cs 啟動測試"
Task: "撰寫 ServiceCollectionExtensions 測試"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational (CRITICAL - 阻擋所有故事)
3. 完成 Phase 3: User Story 1
4. **停止並驗證**: 獨立測試 User Story 1
5. 若準備好則部署/示範

### Incremental Delivery

1. 完成 Setup + Foundational → 基礎就緒
2. 加入 User Story 1 → 獨立測試 → 部署/示範 (MVP!)
3. 加入 User Story 2 → 獨立測試 → 部署/示範
4. 加入 User Story 3 → 獨立測試 → 部署/示範
5. 每個故事增加價值而不破壞先前故事

### Parallel Team Strategy

若有多位開發者:

1. 團隊一起完成 Setup + Foundational
2. Foundational 完成後:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. 故事獨立完成並整合

---

## Task Summary

**Total Tasks**: 47
- **Setup (Phase 1)**: 17 tasks
- **Foundational (Phase 2)**: 4 tasks
- **User Story 1**: 7 tasks (2 tests + 5 implementation)
- **User Story 2**: 5 tasks (1 test + 4 implementation)
- **User Story 3**: 6 tasks (1 test + 5 implementation)
- **Polish (Phase 6)**: 8 tasks

**Parallel Opportunities**: 20 tasks marked [P] can run in parallel within their phases

**MVP Scope**: Phases 1-3 (Setup + Foundational + User Story 1) = 28 tasks

---

## Notes

- [P] 任務 = 不同檔案,無依賴關係
- [Story] 標籤將任務對應到特定使用者故事,便於追蹤
- 每個使用者故事應可獨立完成與測試
- 實作前驗證測試失敗
- 每個任務或邏輯群組後提交
- 在任何 checkpoint 停止以獨立驗證故事
- 避免:模糊任務、同檔案衝突、破壞獨立性的跨故事依賴

---

**Generated**: 2025-10-18
**Based on**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md
**Ready for**: Implementation via `/speckit.implement` or manual execution
