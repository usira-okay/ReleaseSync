# Implementation Plan: Google Sheet 同步匯出功能

**Branch**: `002-google-sheet-sync` | **Date**: 2025-11-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-google-sheet-sync/spec.md`

## Summary

實作 Google Sheet 同步匯出功能，允許使用者透過 `--google-sheet` 命令列參數將 PR/MR 資訊同步至指定的 Google Sheet 工作表。功能包含：

- 新增 `--google-sheet` 命令列參數控制同步行為
- 支援 Service Account JSON 憑證驗證
- 批次讀取/寫入策略以符合 API 速率限制
- 使用 Polly retry 機制處理暫時性錯誤
- 透過 UK (Unique Key = WorkItemId + RepositoryName) 比對現有 row 並合併資料
- 可配置的欄位對應 (Column Mapping)

技術方案採用 Google.Apis.Sheets.v4 官方套件，遵循現有 Clean Architecture 分層設計。

## Technical Context

**Language/Version**: C# / .NET 9.0 (Console), .NET 8.0 (Libraries)
**Primary Dependencies**: Google.Apis.Sheets.v4, Polly, Microsoft.Extensions.Options, Serilog
**Storage**: Google Sheets API (外部雲端服務)
**Testing**: xUnit + FluentAssertions + NSubstitute (沿用現有測試框架)
**Target Platform**: Windows, Linux, macOS (跨平台 CLI)
**Project Type**: CLI Tool (Console Application)
**Performance Goals**: 同步 100 筆 PR/MR 於 60 秒內完成
**Constraints**: Google Sheets API 速率限制 (300 requests/min), 批次處理
**Scale/Scope**: 預期每次同步處理 10-100 個 repositories, 100-1000 筆 PR/MR

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

此功能設計必須符合以下憲章原則 (詳見 `.specify/memory/constitution.md`):

- **✅ DDD 戰術模式**: 清楚定義 Value Objects (GoogleSheetConfiguration, SheetRowData, GoogleSheetColumnMapping) 與邊界
- **✅ CQRS 模式**: 同步操作為命令 (寫入 Sheet)，不直接返回查詢資料；讀取 Sheet 為獨立查詢
- **✅ SOLID 原則**:
  - SRP: 每個類別單一職責 (ApiClient 負責 API 呼叫, DataMapper 負責資料轉換, SyncService 負責協調)
  - OCP: 透過介面擴展，不修改現有匯出邏輯
  - LSP: GoogleSheetSyncService 不繼承 IResultExporter (避免違反 LSP，因為行為差異太大)
  - ISP: 介面精簡，只包含必要方法
  - DIP: 依賴抽象介面，非具體實作
- **✅ TDD 強制執行**: 先撰寫單元測試再實作，目標覆蓋率 80%+
- **✅ KISS 原則**: 重用現有 IWorkItemIdParser，避免重複實作；使用官方 Google API 套件而非自建
- **✅ 例外處理**: 自訂例外類型 (GoogleSheetSyncException 系列)，僅在必要時捕捉，提供明確錯誤訊息
- **✅ 繁體中文**: 所有文件、註解、錯誤訊息使用繁體中文
- **✅ 註解規範**: 所有公開成員包含 XML 註解，複雜邏輯包含 inline comment
- **✅ 重用優先**: 重用 IWorkItemIdParser、RepositoryBasedOutputDto、現有 DI 模式
- **✅ Program.cs 最小化**: 僅新增 `services.AddGoogleSheetServices(configuration)` 一行
- **✅ 分層架構**: Domain (重用) → Application (SyncService, DataMapper) → Infrastructure (ApiClient, Settings) → Console (Command, Handler)

**複雜度警告**: 無違反憲章原則的設計。

## Project Structure

### Documentation (this feature)

```text
specs/002-google-sheet-sync/
├── plan.md              # 本文件
├── research.md          # 技術研究與決策
├── data-model.md        # 資料模型定義
├── quickstart.md        # 快速開始指南
├── contracts/           # 服務介面契約
│   └── interfaces.md
└── checklists/
    └── requirements.md  # 規格品質檢查清單
```

### Source Code (repository root)

```text
src/
├── ReleaseSync.Domain/                    # 領域層 (重用現有)
│   └── Services/
│       └── IWorkItemIdParser.cs           # 重用
│
├── ReleaseSync.Application/               # 應用層
│   ├── DTOs/
│   │   └── RepositoryBasedOutputDto.cs    # 重用 (輸入資料)
│   ├── Services/
│   │   ├── IGoogleSheetSyncService.cs     # 新增：同步服務介面
│   │   └── GoogleSheetSyncService.cs      # 新增：同步服務實作
│   ├── Mappers/
│   │   ├── IGoogleSheetDataMapper.cs      # 新增：資料對應介面
│   │   └── GoogleSheetDataMapper.cs       # 新增：資料對應實作
│   └── Models/
│       ├── GoogleSheetColumnMapping.cs    # 新增：欄位對應 Value Object
│       ├── SheetRowData.cs                # 新增：Sheet Row 資料 Value Object
│       ├── SheetSyncOperation.cs          # 新增：同步操作 Value Object
│       └── GoogleSheetSyncResult.cs       # 新增：同步結果 Value Object
│
├── ReleaseSync.Infrastructure/            # 基礎設施層
│   ├── Configuration/
│   │   └── GoogleSheetSettings.cs         # 新增：組態模型
│   ├── GoogleSheet/                       # 新增：Google Sheet 整合模組
│   │   ├── GoogleSheetApiClient.cs        # 新增：API 客戶端
│   │   ├── IGoogleSheetApiClient.cs       # 新增：API 客戶端介面
│   │   ├── GoogleSheetRowParser.cs        # 新增：Row 解析器
│   │   └── IGoogleSheetRowParser.cs       # 新增：Row 解析器介面
│   ├── DependencyInjection/
│   │   └── GoogleSheetServiceExtensions.cs # 新增：DI 擴展方法
│   └── Exceptions/
│       └── GoogleSheetExceptions.cs        # 新增：自訂例外類型
│
├── ReleaseSync.Console/                   # 呈現層
│   ├── Program.cs                         # 修改：新增 AddGoogleSheetServices
│   ├── appsettings.json                   # 修改：新增 GoogleSheet 區塊
│   ├── Commands/
│   │   └── SyncCommand.cs                 # 修改：新增 --google-sheet 參數
│   └── Handlers/
│       ├── SyncCommandOptions.cs          # 修改：新增 GoogleSheet 選項
│       └── SyncCommandHandler.cs          # 修改：整合 GoogleSheetSyncService
│
└── tests/                                 # 測試專案
    ├── ReleaseSync.Application.UnitTests/
    │   ├── Services/
    │   │   └── GoogleSheetSyncServiceTests.cs    # 新增
    │   └── Mappers/
    │       └── GoogleSheetDataMapperTests.cs     # 新增
    ├── ReleaseSync.Infrastructure.UnitTests/
    │   └── GoogleSheet/
    │       ├── GoogleSheetApiClientTests.cs      # 新增
    │       └── GoogleSheetRowParserTests.cs      # 新增
    └── ReleaseSync.Console.UnitTests/
        └── Handlers/
            └── SyncCommandHandlerGoogleSheetTests.cs  # 新增
```

**Structure Decision**: 遵循現有 Clean Architecture 分層，在 Application 層新增 Google Sheet 同步服務，在 Infrastructure 層新增 Google Sheets API 客戶端，在 Console 層修改命令處理以支援新參數。

## Complexity Tracking

> **無違反憲章原則的設計**

本功能設計完全符合憲章原則，無需記錄複雜度違反項目。

設計決策說明：
- **不實作 IResultExporter 介面**: GoogleSheetSyncService 不適合實作 IResultExporter，因為：
  1. 行為差異太大 (寫入雲端服務 vs 寫入本地檔案)
  2. 需要額外的組態驗證
  3. 回傳型別不同 (需要詳細的同步結果)
  4. 違反 LSP 原則
- **批次處理策略**: 為了符合 Google API 速率限制，採用記憶體中處理後批次寫入
- **Polly Retry**: 使用者明確要求的功能，提供穩健的錯誤處理

## Generated Artifacts

### Phase 0 Output
- [research.md](./research.md) - 技術研究與決策文件

### Phase 1 Output
- [data-model.md](./data-model.md) - 資料模型定義
- [contracts/interfaces.md](./contracts/interfaces.md) - 服務介面契約
- [quickstart.md](./quickstart.md) - 快速開始指南

### Next Steps
執行 `/speckit.tasks` 命令產生任務清單，遵循 TDD 流程實作功能。
