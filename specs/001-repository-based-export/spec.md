# Feature Specification: Repository-Based Export Format

**Feature Branch**: `001-repository-based-export`
**Created**: 2025-11-15
**Status**: Draft
**Input**: User description: "我需要修正匯出的 json 內容,我希望改成以 repository 為主,方便後續同步到 google sheet 的流程。不需要保留原始的匯出結構"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 基本 Repository 分組匯出 (Priority: P1)

當使用者執行同步命令後,系統應將 Pull Request 資料以 Repository 為主體進行分組匯出,每個 Repository 包含其關聯的所有 Pull Requests,以便使用者能夠快速識別每個專案的變更情況。

**Why this priority**: 這是核心功能,提供最基本的 Repository 分組能力,是後續 Google Sheets 同步的基礎。沒有此功能,無法達成使用者的主要目標。

**Independent Test**: 可透過執行 `sync` 命令並檢查輸出 JSON 檔案結構來完整測試。檔案應以 repositories 陣列為頂層結構,每個 repository 物件包含該專案的所有 PR 資訊。

**Acceptance Scenarios**:

1. **Given** 系統從 GitLab 與 BitBucket 抓取到 5 個 PR,分屬 3 個不同的 repositories, **When** 使用者執行匯出, **Then** 輸出的 JSON 應包含 3 個 repository 物件,每個物件包含對應的 PR 清單
2. **Given** 某個 repository 在指定日期區間內沒有任何 PR, **When** 使用者執行匯出, **Then** 該 repository 不應出現在輸出檔案中
3. **Given** 系統抓取到的 PR 都屬於同一個 repository, **When** 使用者執行匯出, **Then** 輸出應包含單一 repository 物件,包含所有 PR

---

### User Story 2 - Repository 彙總統計資訊 (Priority: P2)

當使用者檢視匯出的 JSON 檔案時,每個 Repository 應顯示彙總統計資訊(如 PR 總數、作者清單),讓使用者能快速了解各專案的活躍程度,無需手動計算。

**Why this priority**: 提供彙總統計能提升資料可讀性與分析效率,但不是核心功能。即使沒有統計資訊,使用者仍可手動統計或透過試算表公式計算。

**Independent Test**: 可透過檢查輸出 JSON 中每個 repository 物件是否包含 `totalPullRequests`、`uniqueAuthors` 等統計欄位來驗證。

**Acceptance Scenarios**:

1. **Given** 某個 repository 有 5 個 PR,由 3 位不同作者提交, **When** 使用者執行匯出, **Then** 該 repository 的統計資訊應顯示 `totalPullRequests: 5` 與 `uniqueAuthors: 3`
2. **Given** 某個 repository 只有 1 個 PR,作者為同一人, **When** 使用者執行匯出, **Then** 統計資訊應正確顯示 `totalPullRequests: 1` 與 `uniqueAuthors: 1`

---

### User Story 3 - 保留 Work Item 關聯資訊 (Priority: P3)

當 Pull Request 關聯到 Work Item 時,系統應在 Repository 分組的結構下保留此資訊,讓使用者能追蹤每個 PR 對應的工作項目,方便進行專案管理與追蹤。

**Why this priority**: Work Item 關聯是額外的參考資訊,對於需要追蹤開發任務的團隊很有價值,但不影響核心的 Repository 分組功能。

**Independent Test**: 可透過檢查輸出 JSON 中每個 PR 物件是否包含 `associatedWorkItem` 欄位(如果有關聯)來驗證。

**Acceptance Scenarios**:

1. **Given** 某個 PR 的 branch 名稱包含 Work Item ID (如 `123-feature-name`), **When** 使用者執行匯出, **Then** 該 PR 的 `associatedWorkItem` 欄位應包含 Work Item 的 ID、標題、類型、團隊與 URL
2. **Given** 某個 PR 沒有關聯到任何 Work Item, **When** 使用者執行匯出, **Then** 該 PR 的 `associatedWorkItem` 欄位應為 null

---

### Edge Cases

- **空資料集**: 當指定日期區間內沒有任何 PR 時,系統應輸出包含空 repositories 陣列的有效 JSON (`{"repositories": []}`)
- **Repository 名稱格式差異**: 不同平台的 Repository 名稱格式可能不同(如 `owner/repo` vs `repo`),系統應正確識別並分組
- **相同 Repository 名稱但不同平台**: 如果 GitLab 與 BitBucket 存在相同名稱的 repository,應視為不同的 repository(可透過 platform 欄位區分)
- **作者資訊缺失**: 當 PR 的作者資訊(AuthorUserId 或 AuthorDisplayName)為 null 時,統計功能仍應正常運作
- **日期範圍邊界**: 系統應正確處理落在 startDate 與 endDate 邊界的 PR(包含邊界值)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統必須將 Pull Request 資料以 Repository 為主體進行分組,輸出結構為 `{ repositories: [...] }`
- **FR-002**: 每個 Repository 物件必須包含: repositoryName(字串)、platform(字串)、pullRequests(陣列)
- **FR-003**: 每個 Pull Request 物件必須包含: title、sourceBranch、targetBranch、mergedAt、authorUserId、authorDisplayName、url、associatedWorkItem(可為 null)
- **FR-004**: 系統必須提供每個 Repository 的彙總統計資訊,包含: totalPullRequests(PR 總數)、uniqueAuthors(不重複作者數量)
- **FR-005**: 系統必須保留頂層的 metadata,包含: startDate(查詢開始日期)、endDate(查詢結束日期)
- **FR-006**: 當 Pull Request 關聯到 Work Item 時,必須在 associatedWorkItem 欄位包含: workItemId、workItemTitle、workItemType、workItemTeam、workItemUrl
- **FR-007**: 系統不得保留原有的 Work Item 為中心的匯出結構(`WorkItemCentricOutputDto`)
- **FR-008**: Repository 分組必須同時考慮 repositoryName 與 platform,相同名稱但不同平台的 repository 應分開處理
- **FR-009**: 輸出的 JSON 必須使用 camelCase 命名規則,格式化縮排,並正確處理中文字元(不跳脫)

### Key Entities

- **Repository**: 代表版本控制系統中的專案,包含名稱、平台類型、關聯的 Pull Requests 與統計資訊
- **Pull Request**: 代表程式碼變更請求,包含標題、分支資訊、合併時間、作者資訊、URL 與可選的 Work Item 關聯
- **Work Item**: 代表開發任務或需求,包含 ID、標題、類型、所屬團隊與 URL
- **Export Metadata**: 代表匯出作業的元資料,包含查詢的日期範圍

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 使用者能在 3 秒內從 JSON 檔案中找到特定 repository 的所有 PR(透過結構化的 repository 分組)
- **SC-002**: 匯出的 JSON 檔案可直接匯入 Google Sheets,無需手動重新整理資料結構
- **SC-003**: 系統能正確處理包含 100 個 repositories、每個 repository 平均 20 個 PR 的大型資料集,匯出時間不超過 5 秒
- **SC-004**: 95% 的使用者能在首次檢視新格式 JSON 後,立即理解資料結構而無需查閱文件

## Assumptions *(optional)*

- 假設使用者主要使用 Google Sheets 進行後續資料分析,因此 Repository 分組結構應符合試算表的行列邏輯
- 假設不同平台的相同名稱 repository 應視為不同專案(例如內部 GitLab 與外部 BitBucket 可能有相同專案名稱)
- 假設使用者希望移除舊的匯出格式,不需要同時支援兩種格式
- 假設現有的 JSON 序列化設定(`WriteIndented: true`, `CamelCase`, `UnsafeRelaxedJsonEscaping`)應繼續使用

## Out of Scope

- 不包含多種匯出格式的選項(如同時支援 Work Item-centric 與 Repository-based),僅提供 Repository-based 格式
- 不包含自訂分組邏輯(如按作者、按日期分組),僅支援 Repository 分組
- 不包含匯出格式的 CLI 參數選擇(如 `--export-format=repository`),直接替換現有格式
- 不包含向後相容性處理,舊的 Work Item-centric 格式將完全移除
- 不包含 CSV 或 Excel 格式的匯出,僅限 JSON

## Dependencies

- 依賴現有的 `PullRequestInfo` 領域模型(包含 AssociatedWorkItem 屬性)
- 依賴現有的 `JsonFileExporter` 序列化邏輯與檔案寫入功能
- 依賴現有的 `SyncOrchestrator` 提供的 `SyncResultDto` 資料結構

## References

- 現有實作: `src/ReleaseSync.Application/DTOs/WorkItemCentricOutputDto.cs`
- 現有實作: `src/ReleaseSync.Application/Exporters/JsonFileExporter.cs`
- 現有實作: `src/ReleaseSync.Domain/Models/PullRequestInfo.cs`
