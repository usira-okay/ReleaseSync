# Changelog

所有重要的專案變更都會記錄在此檔案。

格式基於 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.0.0/),
專案遵循 [Semantic Versioning](https://semver.org/lang/zh-TW/)。

## [Unreleased]

### 新增功能

- Phase 6 完成 - Polish & Cross-Cutting Concerns
  - 完整的結構化日誌記錄於所有服務層
  - 使用者友善的錯誤訊息與錯誤處理
  - --verbose 參數支援 Debug 等級日誌
  - 完整的 XML 文件註解 (繁體中文)
  - 效能指標自動記錄與監控
  - 安全性審查通過 (無敏感資訊洩漏)
  - README.md 完整文件

### 改進

- JsonFileExporter 加入詳細的日誌記錄
- SyncOrchestrator 加入效能指標計算與警告
- Program.cs 根據 --verbose 參數動態設定日誌等級
- SyncCommandHandler 加入多種錯誤情境的友善訊息

## [0.3.0] - Phase 5 Complete - 2025-10-18

### 新增功能

- User Story 3 完成 - 從 Branch 名稱解析 Azure DevOps Work Item
  - RegexWorkItemIdParser 支援多個 Regex Pattern
  - AzureDevOpsService 整合 Work Item 查詢
  - AzureDevOpsWorkItemRepository 實作
  - SyncOrchestrator 加入 Work Item 關聯邏輯
  - --enable-azure-devops 參數支援

### 改進

- Work Item 解析失敗時記錄 Warning 但繼續執行
- 支援 Parent Work Item 自動關聯
- Branch 名稱解析支援多種命名規則

## [0.2.0] - Phase 4 Complete - 2025-10-18

### 新增功能

- User Story 2 完成 - 將 PR/MR 資訊匯出為 JSON 檔案
  - JsonFileExporter 使用 System.Text.Json
  - --output-file 參數支援
  - --force 參數強制覆蓋檔案
  - 檔案已存在時的友善錯誤訊息

### 改進

- SyncCommandHandler 加入 JSON 匯出邏輯
- 自動建立輸出目錄

## [0.1.0] - Phase 3 Complete - 2025-10-18

### 新增功能

- User Story 1 完成 - 從單一平台抓取 PR/MR 資訊 (MVP)
  - GitLabService 整合 NGitLab SDK
  - BitBucketService 整合 REST API
  - SyncOrchestrator 協調多平台查詢
  - 並行查詢提升效能
  - 部分失敗容錯處理

### 改進

- 支援時間範圍篩選 (--start-date, --end-date)
- 支援目標分支篩選
- 平台啟用參數 (--enable-gitlab, --enable-bitbucket)

## [0.0.2] - Phase 2 Complete - 2025-10-18

### 新增功能

- Foundational Layer 完成
  - Domain Models: DateRange, BranchName, WorkItemId, PullRequestInfo, WorkItemInfo, SyncResult
  - Repository Interfaces: IPullRequestRepository, IWorkItemRepository
  - Configuration Models: GitLabSettings, BitBucketSettings, AzureDevOpsSettings
  - Application DTOs: SyncRequest, SyncResultDto
  - Application Interfaces: ISyncOrchestrator, IResultExporter

## [0.0.1] - Phase 1 Complete - 2025-10-18

### 新增功能

- 專案結構建立
  - Clean Architecture 專案架構 (Domain, Application, Infrastructure, Console)
  - NuGet 套件安裝完成
  - .editorconfig 與 StyleCop Analyzers 設定
  - .gitignore 排除敏感檔案
  - appsettings 範本檔案

---

## 格式說明

- **新增功能**: 新功能
- **改進**: 現有功能的改進
- **棄用**: 即將移除的功能
- **移除**: 已移除的功能
- **修復**: 任何錯誤修復
- **安全性**: 安全性相關的修復

---

**版本**: 1.0.0
**最後更新**: 2025-10-18
