# Feature Specification: PR/MR 變更資訊聚合工具

**Feature Branch**: `002-pr-aggregation-tool`
**Created**: 2025-10-18
**Status**: Draft
**Input**: User description: "建立一個 ReleaseSync Console 工具,用於抓取版控平台 Pull Request (PR) 或 Merge Request (MR) 變更資訊,並整理成 JSON 格式匯出"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 從單一平台抓取 PR/MR 資訊 (Priority: P1)

作為發布管理員,我需要能夠從 GitLab 或 BitBucket 抓取指定時間範圍內的 PR/MR 資訊,以便了解該期間內的程式碼變更。

**Why this priority**: 這是核心功能的最小可行版本,讓工具能夠從單一來源獲取基本資料,是後續功能的基礎。

**Independent Test**: 可以透過執行工具並指定單一平台(例如 GitLab)和時間範圍,驗證是否能成功抓取並顯示 PR/MR 清單資訊。

**Acceptance Scenarios**:

1. **Given** 使用者指定 GitLab 平台與時間範圍 2025-01-01 到 2025-01-31, **When** 執行工具, **Then** 工具成功抓取該時間範圍內所有 MR 資訊,包含 MR ID、標題、Branch 名稱、建立時間
2. **Given** 使用者指定 BitBucket 平台與時間範圍, **When** 執行工具, **Then** 工具成功抓取該時間範圍內所有 PR 資訊
3. **Given** 使用者同時指定 GitLab 和 BitBucket 平台, **When** 執行工具, **Then** 工具從兩個平台分別抓取資訊並合併結果

---

### User Story 2 - 將 PR/MR 資訊匯出為 JSON 檔案 (Priority: P2)

作為發布管理員,我需要能夠將抓取到的 PR/MR 資訊匯出為 JSON 檔案,以便進行後續分析或與其他系統整合。

**Why this priority**: 提供資料持久化和資料交換能力,讓使用者可以保存和分享抓取結果。

**Independent Test**: 執行工具後,驗證是否能選擇匯出選項並成功產生包含所有 PR/MR 資訊的 JSON 檔案。

**Acceptance Scenarios**:

1. **Given** 已成功抓取 PR/MR 資訊, **When** 使用者選擇匯出 JSON, **Then** 工具在當前目錄產生格式正確的 JSON 檔案
2. **Given** 已成功抓取 PR/MR 資訊, **When** 使用者指定匯出檔案路徑, **Then** 工具在指定路徑產生 JSON 檔案
3. **Given** 使用者選擇不匯出, **When** 工具完成抓取, **Then** 工具僅在 console 顯示結果而不產生檔案

---

### User Story 3 - 從 Branch 名稱解析 Azure DevOps Work Item (Priority: P3)

作為發布管理員,我需要從 PR/MR 的 Branch 名稱中解析出 Azure DevOps Work Item ID,並抓取對應的 Parent Work Item 資訊,以便追蹤變更與需求的對應關係。

**Why this priority**: 這是增值功能,提供跨系統的追溯能力,但不影響基本的 PR/MR 資訊抓取功能。

**Independent Test**: 使用包含 Work Item ID 的 Branch 名稱(例如 feature/12345-new-feature),驗證工具是否能成功解析 ID 並抓取 Azure DevOps 資訊。

**Acceptance Scenarios**:

1. **Given** PR/MR 的 Branch 名稱包含 Work Item ID(例如 feature/12345-description), **When** 啟用 Azure DevOps 整合選項, **Then** 工具成功解析 Work Item ID 並抓取該 Work Item 及其 Parent Work Item 資訊
2. **Given** Branch 名稱不包含有效的 Work Item ID, **When** 啟用 Azure DevOps 整合選項, **Then** 工具記錄警告但繼續處理其他 PR/MR
3. **Given** 使用者未啟用 Azure DevOps 整合選項, **When** 執行工具, **Then** 工具僅抓取 PR/MR 基本資訊,不進行 Work Item 解析

---

### Edge Cases

- 當指定的時間範圍內沒有任何 PR/MR 時,工具應顯示訊息告知使用者並產生空的結果集
- 當平台 API 認證失敗時,工具應顯示清楚的錯誤訊息並指引使用者如何設定認證資訊
- 當 Branch 名稱格式不符合預期(無法解析 Work Item ID)時,工具應記錄警告但繼續處理
- 當 Azure DevOps Work Item 不存在或無權限存取時,工具應記錄錯誤但不中斷整體流程
- 當網路連線中斷或 API 回應逾時時,工具應提供重試機制或友善的錯誤訊息
- 當同時抓取多個平台資料且其中一個平台失敗時,工具應繼續處理其他平台並報告部分成功
- 當 JSON 檔案匯出路徑已存在同名檔案時,工具應詢問使用者是否覆蓋或提供覆蓋/略過選項

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 工具必須提供命令列參數,允許使用者選擇啟用/停用 GitLab 平台的 PR/MR 抓取功能
- **FR-002**: 工具必須提供命令列參數,允許使用者選擇啟用/停用 BitBucket 平台的 PR/MR 抓取功能
- **FR-003**: 工具必須提供命令列參數,允許使用者指定開始日期與結束日期以篩選 PR/MR
- **FR-004**: 工具必須能夠從 GitLab 抓取指定時間範圍內的 Merge Request 資訊,包含 MR ID、標題、描述、來源 Branch 名稱、建立時間、合併時間、作者資訊
- **FR-005**: 工具必須能夠從 BitBucket 抓取指定時間範圍內的 Pull Request 資訊,包含 PR ID、標題、描述、來源 Branch 名稱、建立時間、合併時間、作者資訊
- **FR-006**: 工具必須提供命令列參數,允許使用者選擇啟用/停用 Azure DevOps Work Item 整合功能
- **FR-007**: 當啟用 Azure DevOps 整合時,工具必須能夠從 Branch 名稱中解析 Work Item ID (具體命名規則格式將在規劃階段確認)
- **FR-008**: 工具必須能夠使用解析出的 Work Item ID 從 Azure DevOps 抓取 Work Item 資訊,包含 ID、標題、類型、狀態
- **FR-009**: 工具必須能夠抓取 Work Item 的 Parent Work Item 資訊(如果存在)
- **FR-010**: 工具必須能夠將所有抓取的資訊(PR/MR 及 Work Item)整理成結構化的 JSON 格式
- **FR-011**: 工具必須提供選項讓使用者選擇是否匯出 JSON 檔案到本機
- **FR-012**: 工具必須在執行過程中顯示進度資訊,讓使用者了解當前處理狀態
- **FR-013**: 工具必須處理 API 認證,支援透過環境變數或設定檔提供各平台的存取憑證
- **FR-014**: 工具必須在遇到錯誤時提供清楚的錯誤訊息,包含錯誤原因與建議解決方式
- **FR-015**: 工具必須能夠在部分資料抓取失敗時繼續執行,並在最後提供完整的成功/失敗報告

### Key Entities

- **PR/MR 資訊**: 代表版控平台上的 Pull Request 或 Merge Request,包含識別碼、標題、描述、來源 Branch、目標 Branch、建立時間、合併時間、狀態、作者資訊
- **Work Item 資訊**: 代表 Azure DevOps 上的工作項目,包含 Work Item ID、標題、類型(例如 User Story、Bug、Task)、狀態、Parent Work Item 參照
- **聚合結果**: 整合 PR/MR 與對應 Work Item 的完整資訊,建立 PR/MR 與 Work Item 之間的關聯關係
- **平台設定**: 包含各平台(GitLab、BitBucket、Azure DevOps)的 API 端點、認證資訊、專案/儲存庫識別碼

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 使用者能夠在 30 秒內完成工具的基本執行(抓取單一平台、單月資料),不包含網路傳輸時間
- **SC-002**: 工具能夠成功處理包含 100 個以上 PR/MR 的資料集而不發生錯誤
- **SC-003**: 當發生 API 錯誤時,90% 的錯誤訊息能讓使用者在不查閱文件的情況下理解問題並採取行動
- **SC-004**: 產生的 JSON 檔案符合標準 JSON 格式規範,能被常見 JSON 解析工具正確讀取
- **SC-005**: 工具能在單一平台 API 失敗時,仍成功完成其他平台的資料抓取並產生部分結果
- **SC-006**: Work Item ID 解析準確率達 95% 以上(對於符合預期格式的 Branch 名稱)

## Assumptions

- 使用者已具備各平台(GitLab、BitBucket、Azure DevOps)的 API 存取權限與認證資訊
- Branch 命名遵循團隊的命名慣例,並包含可識別的 Work Item ID(需要進一步確認格式)
- 使用者執行工具的環境能夠連接到網際網路,並能存取各平台的 API 端點
- PR/MR 的時間篩選是基於建立時間(非合併時間),除非使用者另有指定
- 各平台 API 的回應時間在合理範圍內(通常小於 10 秒),不會發生長時間無回應
- JSON 檔案匯出預設位置為工具執行的當前目錄,除非使用者指定其他路徑
- 初期階段工具方法可以僅建立架構,實作細節可拋出 NotImplementedException,待後續實作
