# Feature Specification: Release Branch 差異比對功能

**Feature Branch**: `001-release-branch-diff`
**Created**: 2026-01-20
**Status**: Draft
**Input**: 團隊需要比對不同 branch 之間的 commit hash 差異，支援週更新開發週期的 release branch 管理

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 使用 Release Branch 比對取得待發布變更 (Priority: P1)

身為專案管理者，我希望能夠指定一個 release branch，系統自動比對與目標分支的差異，以便了解哪些變更尚未進入 release。

**Why this priority**: 這是核心功能，直接解決團隊週更新開發週期中最重要的需求 - 確認哪些 commit 尚未進入 release。

**Independent Test**: 可透過設定 `FetchMode=ReleaseBranch` 並指定最新版 release branch，驗證系統是否正確回傳 TargetBranch 有但 Release Branch 沒有的 PR 資訊。

**Acceptance Scenarios**:

1. **Given** 全域設定 `SyncOptions.ReleaseBranch = "release/20260120"` 且專案設定 `FetchMode = ReleaseBranch`，**When** 執行同步，**Then** 系統比對 release/20260120 與 TargetBranch，回傳 TargetBranch 中尚未合併至 release branch 的 PR 資訊
2. **Given** 指定的 release branch 為該 repository 的最新版本，**When** 執行同步，**Then** 系統以 TargetBranch 為比對基準
3. **Given** repository 層級設定覆寫了全域 ReleaseBranch，**When** 執行同步，**Then** 系統使用 repository 層級的設定值

---

### User Story 2 - 使用時間範圍抓取 PR 資訊 (Priority: P1)

身為專案管理者，我希望能夠保留使用時間範圍抓取 PR 的方式，以便在不使用 release branch 的情境下仍能取得變更資訊。

**Why this priority**: 與 P1 並列，因為這是現有功能的延續，確保向後相容性，且部分 repository 可能不使用 release branch 模式。

**Independent Test**: 可透過設定 `FetchMode=DateRange` 並指定 StartDate/EndDate，驗證系統是否正確回傳該時間範圍內的 PR 資訊。

**Acceptance Scenarios**:

1. **Given** 專案設定 `FetchMode = DateRange` 且指定 StartDate 與 EndDate，**When** 執行同步，**Then** 系統回傳該時間範圍內合併的 PR 資訊
2. **Given** repository 層級設定覆寫了全域時間範圍，**When** 執行同步，**Then** 系統使用 repository 層級的 StartDate/EndDate

---

### User Story 3 - 比對歷史版本 Release Branch 差異 (Priority: P2)

身為專案管理者，我希望能夠比對兩個不同版本的 release branch 差異，以便追溯特定 release 之間的變更。

**Why this priority**: 雖然重要但屬於進階功能，多數使用情境集中在最新版 release branch 比對。

**Independent Test**: 可透過設定較舊版本的 release branch（如 release/20260113），驗證系統是否自動找到下一版 release branch（如 release/20260120）並進行比對。

**Acceptance Scenarios**:

1. **Given** 設定的 release branch 為舊版本（如 release/20260113），**When** 執行同步，**Then** 系統自動識別下一版 release branch（release/20260120）並比對差異
2. **Given** 設定的 release branch 已是最新版本，**When** 執行同步，**Then** 系統比對與 TargetBranch 的差異（如 User Story 1）

---

### User Story 4 - 簡化 TargetBranch 配置 (Priority: P2)

身為系統管理者，我希望 TargetBranch 配置從陣列改為單一值，以簡化設定並明確每個專案的主要目標分支。

**Why this priority**: 配置簡化屬於改善開發者體驗的工作，對功能本身影響較小。

**Independent Test**: 可透過修改 appsettings.json 中的 TargetBranch 為單一字串值，驗證系統能正確讀取並使用該設定。

**Acceptance Scenarios**:

1. **Given** GitLab 專案設定 `TargetBranch = "master"`（單一值），**When** 執行同步，**Then** 系統正確使用 master 作為目標分支
2. **Given** BitBucket 專案設定 `TargetBranch = "develop"`（單一值），**When** 執行同步，**Then** 系統正確使用 develop 作為目標分支

---

### Edge Cases

- 當 `FetchMode = ReleaseBranch` 但指定的 release branch 在 repository 中不存在時，系統應拋出明確錯誤並終止執行
- 當設定為舊版 release branch，但找不到下一版 release branch 時，系統應視為最新版並比對 TargetBranch
- 當 repository 層級未設定 ReleaseBranch，且全域也未設定時，系統應拋出設定錯誤
- 當 release branch 命名格式不符合 `release/yyyyMMdd` 時，系統應拋出格式驗證錯誤
- 當 DateRange 模式下 StartDate/EndDate 皆未設定時，系統應拋出設定錯誤

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統 MUST 支援新的 `SyncOptions` 全域配置節點，包含 `ReleaseBranch`、`StartDate` 與 `EndDate` 設定
- **FR-002**: 系統 MUST 將 GitLab 與 BitBucket 的 `TargetBranches` 陣列配置改為 `TargetBranch` 單一值
- **FR-003**: 系統 MUST 支援 `FetchMode` 列舉設定，允許選擇 `DateRange` 或 `ReleaseBranch` 抓取模式
- **FR-004**: 當 `FetchMode = ReleaseBranch` 且為最新版本時，系統 MUST 比對 Release Branch 與 TargetBranch，取得「TargetBranch 有但 Release Branch 沒有」的 commit hash
- **FR-005**: 當 `FetchMode = ReleaseBranch` 且為舊版本時，系統 MUST 自動識別下一版 Release Branch 並比對差異
- **FR-006**: 系統 MUST 支援 repository 層級的 ReleaseBranch 設定覆寫全域設定
- **FR-007**: 系統 MUST 支援 repository 層級的 StartDate/EndDate 設定覆寫全域設定
- **FR-008**: 當 `FetchMode = ReleaseBranch` 但指定的 release branch 不存在時，系統 MUST 拋出錯誤並終止執行
- **FR-009**: Release branch 命名格式 MUST 遵循 `release/yyyyMMdd` 規範，系統 MUST 驗證格式正確性
- **FR-010**: 系統 MUST 根據 FetchMode 與 Release Branch 狀態，動態選擇對應的資料抓取策略

### Key Entities

- **SyncOptions**: 全域同步選項，包含預設的 ReleaseBranch、StartDate、EndDate 設定
- **FetchMode**: 列舉值，定義抓取模式（DateRange、ReleaseBranch）
- **ProjectConfig**: 專案配置，新增 FetchMode、ReleaseBranch、StartDate、EndDate 選填欄位，用於覆寫全域設定
- **ReleaseBranchName**: 值物件，封裝 release branch 命名規則驗證與日期解析邏輯
- **CommitDiff**: 表示兩個分支之間的 commit 差異資訊

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 使用者可在 30 秒內完成從 DateRange 模式切換至 ReleaseBranch 模式的配置變更
- **SC-002**: 系統能正確識別 release branch 版本順序，準確率達 100%
- **SC-003**: 系統在 release branch 不存在時，於 5 秒內回傳明確的錯誤訊息
- **SC-004**: 配置覆寫邏輯正確運作，repository 層級設定優先於全域設定的測試通過率達 100%
- **SC-005**: 現有使用 DateRange 模式的使用者，在升級後無需修改配置即可繼續正常運作（向後相容）
