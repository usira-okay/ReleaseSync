# Feature Specification: 資料過濾機制 - UserMapping 與 Team Mapping

**Feature Branch**: `003-filter-unmapped-data`
**Created**: 2025-10-25
**Status**: Draft
**Input**: User description: "目前有個 UserMapping 的設定,我希望在抓取 PR/MR 資訊時如果 Author 在 UserMapping 裡面沒有對應的值,就不要收錄這筆資訊。請多新增一個 azure devops 團隊設定的參數,用來 mapping work item 的 team field 與顯示用的 team 名稱。如果抓取的 work item team field 沒有對應到這個 mapping 裡面的值,也不要收錄這筆資訊。"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 過濾未對應作者的 PR/MR (Priority: P1)

作為發版管理人員,我希望系統只收錄 UserMapping 中定義的團隊成員所提交的 PR/MR,這樣我就能確保產出的發版報告只包含我們關注的開發人員的貢獻。

**Why this priority**: 這是核心功能,確保資料品質和報告的相關性。避免收錄外部協作者、機器人帳號或已離職人員的 PR/MR。

**Independent Test**: 設定 UserMapping 包含 3 個使用者,抓取包含 5 個不同作者的 PR/MR 清單,系統應只保留 UserMapping 中定義的 3 個使用者的 PR/MR,其餘 2 筆應被過濾掉。

**Acceptance Scenarios**:

1. **Given** UserMapping 中定義了使用者 "john.doe" 和 "jane.smith", **When** 從 GitLab 抓取包含作者為 "john.doe"、"jane.smith"、"external.user" 的 PR, **Then** 系統應只收錄前兩個 PR,並記錄過濾掉 1 筆未對應的 PR
2. **Given** UserMapping 中定義了 BitBucket 使用者 "jdoe", **When** 從 BitBucket 抓取包含作者為 "jdoe" 和 "unknown" 的 PR, **Then** 系統應只收錄 "jdoe" 的 PR
3. **Given** UserMapping 為空清單, **When** 從任何平台抓取 PR/MR, **Then** 系統應記錄警告並收錄所有 PR/MR (維持向後相容)
4. **Given** UserMapping 中定義了多平台對應 (同一人在 GitLab 和 BitBucket 的帳號), **When** 從不同平台抓取該使用者的 PR/MR, **Then** 兩個平台的 PR/MR 都應被正確收錄並使用相同的 DisplayName

---

### User Story 2 - 新增 Azure DevOps Team Mapping 配置 (Priority: P2)

作為系統管理員,我希望能在配置檔中定義 Azure DevOps Work Item 的團隊欄位對應,讓系統知道如何將 Work Item 的內部團隊代碼對應到可讀的團隊名稱。

**Why this priority**: 這是 P3 功能的前置需求,必須先有配置結構才能進行過濾。但不影響現有 PR/MR 過濾功能。

**Independent Test**: 在 appsettings.json 中新增 AzureDevOps.TeamMapping 區段,定義 3 組 team field 到 display name 的對應,系統啟動時應能成功載入配置而不報錯。

**Acceptance Scenarios**:

1. **Given** appsettings.json 中定義了 TeamMapping `[{"OriginalTeamName": "Team Alpha", "DisplayName": "產品開發組"}]`, **When** 系統啟動並載入配置, **Then** 系統應成功解析 TeamMapping 並可供後續使用
2. **Given** TeamMapping 區段不存在於配置檔中, **When** 系統啟動, **Then** 系統應使用空的 TeamMapping (維持向後相容) 並記錄訊息
3. **Given** TeamMapping 定義了重複的 OriginalTeamName, **When** 系統載入配置, **Then** 系統應記錄警告,後定義的對應會覆蓋先前的對應

---

### User Story 3 - 過濾未對應團隊的 Work Item (Priority: P3)

作為發版管理人員,我希望系統只收錄 TeamMapping 中定義的團隊的 Work Item,這樣發版報告就只包含我負責的團隊範圍,不會混入其他團隊的資訊。

**Why this priority**: 依賴 P2 的配置結構,且僅影響 Work Item 資料,不影響 PR/MR 核心功能。

**Independent Test**: 設定 TeamMapping 包含 2 個團隊,抓取包含 4 個不同團隊的 Work Item,系統應只保留 TeamMapping 中定義的 2 個團隊的 Work Item,其餘應被過濾掉。

**Acceptance Scenarios**:

1. **Given** TeamMapping 定義了 "Team Alpha" 和 "Team Beta", **When** 從 Azure DevOps 抓取包含 team field 為 "Team Alpha"、"Team Beta"、"Team Gamma" 的 Work Item, **Then** 系統應只收錄前兩個 Work Item
2. **Given** TeamMapping 定義了團隊對應, **When** Work Item 的 team field 為空值或不存在, **Then** 該 Work Item 應被過濾掉
3. **Given** TeamMapping 為空清單, **When** 從 Azure DevOps 抓取 Work Item, **Then** 系統應記錄警告並收錄所有 Work Item (維持向後相容)
4. **Given** TeamMapping 定義了團隊對應, **When** 收錄的 Work Item 在最終報告中顯示, **Then** 團隊名稱應顯示為 DisplayName 而非原始的 OriginalTeamName 值

---

### Edge Cases

- **UserMapping 中同一 DisplayName 對應到多個平台帳號但其中一個平台 ID 為 null**: 系統應允許部分平台 ID 為 null,只檢查對應平台的 ID
- **PR/MR 的 Author 欄位為 null 或空字串**: 應被視為未對應,該 PR/MR 應被過濾掉並記錄警告
- **Work Item 缺少 team field 欄位** (Azure DevOps API 未回傳該欄位): 當 TeamMapping 非空時,該 Work Item 應被過濾掉
- **大小寫敏感性**: UserMapping 和 TeamMapping 的比對應該不區分大小寫
- **移除對應後的資料完整性**: 當 UserMapping 或 TeamMapping 設定被縮減後,先前收錄的資料不應受影響,只影響新的資料抓取
- **效能考量**: 當 UserMapping 包含數百筆對應時,過濾邏輯應能在合理時間內完成 (不應造成明顯延遲)
- **關聯資料的完整性**: 當 Work Item 被過濾掉時,引用該 Work Item 的 PR/MR 仍應保留,但 Work Item 關聯資訊應顯示為空或標記為「已過濾」,確保 PR/MR 資料的完整性

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 系統必須在抓取 PR/MR 資料後,根據 UserMapping 配置過濾掉 Author 未在對應清單中的紀錄
- **FR-002**: 系統必須支援 GitLab 和 BitBucket 兩個平台的使用者 ID 對應檢查
- **FR-003**: 系統必須在過濾掉未對應的 PR/MR 時,記錄被過濾的紀錄數量和作者資訊到日誌
- **FR-004**: 系統必須在 AzureDevOps 配置區段中支援 TeamMapping 參數,包含 OriginalTeamName 和 DisplayName 的對應清單
- **FR-005**: 系統必須在抓取 Work Item 資料後,根據 TeamMapping 配置過濾掉 team field 未在對應清單中的紀錄
- **FR-006**: 系統必須在過濾掉未對應的 Work Item 時,記錄被過濾的紀錄數量和團隊資訊到日誌
- **FR-007**: 當 UserMapping 為空清單時,系統必須保持向後相容,不過濾任何 PR/MR 並記錄警告訊息
- **FR-008**: 當 TeamMapping 為空清單或不存在時,系統必須保持向後相容,不過濾任何 Work Item 並記錄訊息
- **FR-009**: 系統必須在最終報告中使用 TeamMapping 定義的 DisplayName 來顯示團隊名稱,而非原始的 OriginalTeamName 值
- **FR-010**: UserMapping 和 TeamMapping 的比對必須不區分大小寫
- **FR-011**: 系統必須能處理 Author 或 team field 為 null/空字串的情況,將其視為未對應並過濾掉
- **FR-012**: 當 Work Item 被過濾掉時,引用該 Work Item 的 PR/MR 必須仍然保留,但其 Work Item 關聯資訊應顯示為空或標記為「已過濾」狀態

### Key Entities

- **UserMapping**: 使用者對應設定,包含 GitLabUserId、BitBucketUserId 和 DisplayName 三個屬性,用於跨平台使用者識別和顯示名稱統一
- **TeamMapping**: 團隊對應設定,包含 OriginalTeamName (Azure DevOps Work Item 的團隊欄位原始值) 和 DisplayName (報告中顯示的團隊名稱) 兩個屬性
- **PullRequestInfo**: PR/MR 資訊實體,包含 Author 欄位,用於與 UserMapping 進行比對
- **WorkItemInfo**: Work Item 資訊實體,需新增 Team 欄位用於與 TeamMapping 進行比對

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 當設定包含 10 筆 UserMapping 的配置,抓取 100 筆 PR/MR (其中 30 筆作者未對應) 時,系統能正確過濾並只保留 70 筆符合的 PR/MR
- **SC-002**: 當設定包含 5 筆 TeamMapping 的配置,抓取 50 筆 Work Item (其中 20 筆團隊未對應) 時,系統能正確過濾並只保留 30 筆符合的 Work Item
- **SC-003**: 過濾功能執行後,日誌必須包含明確的統計資訊 (過濾前總數、過濾後總數、被過濾的紀錄數)
- **SC-004**: 在包含 UserMapping 和 TeamMapping 配置的情況下,系統從配置載入到完成資料抓取和過濾的整體時間不應比未啟用過濾功能時增加超過 5%
- **SC-005**: 產出的報告中,團隊名稱必須顯示為 TeamMapping 定義的 DisplayName,而非 Azure DevOps 內部的 OriginalTeamName 值
