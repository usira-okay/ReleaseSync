# Implementation Plan: 資料過濾機制 - UserMapping 與 Team Mapping

**Branch**: `003-filter-unmapped-data` | **Date**: 2025-10-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-filter-unmapped-data/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

本功能擴展現有的 UserMapping 機制,在資料抓取階段過濾掉未對應的 PR/MR 和 Work Item,確保發版報告只包含團隊關注的範圍。主要包含三個部分:

1. **PR/MR 作者過濾**: 根據 UserMapping 配置過濾掉未定義的作者的 PR/MR
2. **Team Mapping 配置**: 在 Azure DevOps 配置中新增 TeamMapping 參數
3. **Work Item 團隊過濾**: 根據 TeamMapping 配置過濾掉未定義團隊的 Work Item

技術方法:
- 在現有的 Repository 層實作過濾邏輯
- 擴展 AzureDevOpsSettings 加入 TeamMapping 支援
- 擴展 WorkItemInfo 實體加入 Team 屬性
- 維持向後相容性 (空 Mapping 時不過濾)
- 使用 IUserMappingService 判斷作者是否在對應清單中
- 新增 ITeamMappingService 處理團隊對應邏輯

## Technical Context

**Language/Version**: C# (.NET 8.0 for libraries, .NET 9.0 for Console)
**Primary Dependencies**:
- Microsoft.Extensions.DependencyInjection (依賴注入)
- Microsoft.Extensions.Configuration (配置管理)
- Serilog (結構化日誌)
- xUnit + FluentAssertions (單元測試)
- System.CommandLine (命令列介面)

**Storage**: 無持久化儲存 (僅從外部 API 抓取資料並輸出 JSON)
**Testing**: xUnit + FluentAssertions + 整合測試 (使用 WebApplicationFactory)
**Target Platform**: Linux/Windows Console Application (.NET CLI tool)
**Project Type**: Clean Architecture Console Application (Domain/Application/Infrastructure/Console)
**Performance Goals**:
- 過濾邏輯對整體執行時間的影響 <5%
- 支援數百筆 UserMapping 和 TeamMapping 配置
- 記憶體使用合理 (過濾過程不應造成顯著增加)

**Constraints**:
- 必須維持向後相容 (空 Mapping 時不影響現有行為)
- 大小寫不敏感比對 (使用 StringComparison.OrdinalIgnoreCase)
- Work Item 被過濾時,PR/MR 仍保留但關聯顯示為空

**Scale/Scope**:
- 支援數百筆 PR/MR 同時過濾
- 支援數十筆 Work Item 同時過濾
- UserMapping 預期 <100 筆
- TeamMapping 預期 <20 筆

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Principle I: Test-First Development
- **Status**: PASS
- **Compliance**: 所有過濾邏輯將先撰寫測試 (單元測試 + 整合測試)
- **Plan**:
  - UserMapping 過濾測試 (測試已對應/未對應/空清單情況)
  - TeamMapping 過濾測試 (測試已對應/未對應/空清單情況)
  - 向後相容性測試
  - 關聯資料完整性測試 (Work Item 被過濾時 PR/MR 行為)

### ✅ Principle II: Code Quality Standards
- **Status**: PASS
- **Compliance**:
  - 使用 `.editorconfig` 確保格式一致
  - `TreatWarningsAsErrors` 已啟用
  - `GenerateDocumentationFile` 已啟用
  - 所有公開 API 將包含 XML 文件註解 (繁體中文)
  - 遵循 KISS 原則 - 過濾邏輯使用簡單的 LINQ Where 條件,不引入複雜抽象

### ✅ Principle III: Test Coverage & Types
- **Status**: PASS
- **Plan**:
  - **Contract Tests**: 驗證 TeamMapping 配置結構 (JSON Schema)
  - **Integration Tests**: 驗證完整的過濾流程 (從配置載入到資料輸出)
  - **Unit Tests**:
    - UserMappingService 單元測試
    - TeamMappingService 單元測試 (新增)
    - Repository 過濾邏輯單元測試

### ✅ Principle IV: Performance Requirements
- **Status**: PASS
- **Plan**:
  - 使用 HashSet<string> 快速查找 (O(1)) 而非線性搜尋
  - 大小寫不敏感比對使用 StringComparer.OrdinalIgnoreCase
  - 過濾在記憶體中完成,不需要額外的 I/O
  - 整合測試將驗證效能影響 <5%

### ✅ Principle V: Observability & Debugging
- **Status**: PASS
- **Plan**:
  - 使用 ILogger<T> 記錄過濾統計資訊
  - 記錄被過濾的紀錄數量和識別資訊
  - 使用結構化日誌參數: `logger.LogInformation("Filtered {Count} PRs without UserMapping", filteredCount)`
  - 警告訊息當 Mapping 為空時

### ✅ Principle VI: Domain-Driven Design
- **Status**: PASS
- **Compliance**:
  - **Entities**: WorkItemInfo 需新增 Team 屬性
  - **Value Objects**: TeamMapping 配置 (OriginalTeamName, DisplayName)
  - **Domain Services**: ITeamMappingService (判斷團隊是否在對應清單中)
  - **Repositories**: 在既有 Repository 實作過濾邏輯
  - **Ubiquitous Language**: "OriginalTeamName", "DisplayName", "UserMapping", "TeamMapping", "過濾"
  - 業務邏輯保持在 Domain/Application 層

### ✅ Principle VII: Design Patterns & SOLID
- **Status**: PASS
- **Patterns**:
  - **Repository Pattern**: 在既有 Repository 中實作過濾
  - **Service Pattern**: IUserMappingService, ITeamMappingService
  - **Dependency Injection**: 所有服務透過 DI 注入
- **SOLID**:
  - Single Responsibility: 每個服務只負責一種對應邏輯
  - Open/Closed: 過濾邏輯可擴展,不修改既有程式碼
  - Dependency Inversion: 依賴 IUserMappingService, ITeamMappingService 介面
- **KISS Compliance**: 不引入不必要的抽象,使用簡單的過濾邏輯

### ✅ Principle VIII: CQRS
- **Status**: PASS (N/A for this feature)
- **Note**: 本功能主要是資料抓取和過濾 (Query side),不涉及狀態變更

### ✅ Principle IX: Program.cs Organization
- **Status**: PASS
- **Plan**:
  - TeamMappingService 註冊將放在 DependencyInjection 擴展方法中
  - 不在 Program.cs 中加入複雜邏輯

### ✅ Principle X: XML Documentation Comments
- **Status**: PASS
- **Plan**:
  - 所有新增的公開類別、介面、方法加入 XML 註解
  - 使用繁體中文描述業務邏輯
  - 使用英文描述純技術細節

### ✅ Principle XI: Inline Comments
- **Status**: PASS
- **Plan**:
  - 在過濾邏輯中加入註解說明業務規則
  - 使用繁體中文解釋為何要過濾
  - TODO 標記向後相容性考量

### ✅ Principle XII: Performance Standards
- **Status**: PASS
- **Plan**:
  - 使用 async/await 不阻塞
  - 避免 N+1 查詢
  - 使用 HashSet 快速查找

### ✅ Principle XIII: Documentation Language Standards
- **Status**: PASS
- **Compliance**:
  - spec.md 使用繁體中文 ✅
  - plan.md 使用繁體中文 ✅
  - XML 註解使用繁體中文描述業務邏輯
  - quickstart.md 將使用繁體中文

## Project Structure

### Documentation (this feature)

```
specs/003-filter-unmapped-data/
├── spec.md              # 功能規格 (已完成)
├── plan.md              # 本檔案 (進行中)
├── research.md          # Phase 0 輸出 (待產生)
├── data-model.md        # Phase 1 輸出 (待產生)
├── quickstart.md        # Phase 1 輸出 (待產生)
├── contracts/           # Phase 1 輸出 (待產生)
│   ├── appsettings-schema-update.json
│   └── team-mapping-example.json
├── checklists/
│   └── requirements.md  # 規格檢查清單 (已完成)
└── tasks.md             # Phase 2 輸出 (由 /speckit.tasks 產生)
```

### Source Code (repository root)

```
src/
├── ReleaseSync.Domain/              # 領域層
│   ├── Models/
│   │   ├── WorkItemInfo.cs          # [修改] 新增 Team 屬性
│   │   └── PullRequestInfo.cs       # [檢視] 確認 Author 屬性
│   ├── Repositories/
│   │   ├── IPullRequestRepository.cs
│   │   └── IWorkItemRepository.cs
│   └── Services/
│       └── ITeamMappingService.cs   # [新增] 團隊對應服務介面
│
├── ReleaseSync.Application/         # 應用層
│   ├── Services/
│   │   ├── IUserMappingService.cs   # [既有] 使用者對應服務
│   │   └── SyncOrchestrator.cs      # [檢視] 確認過濾時機
│   └── DTOs/
│       └── SyncResultDto.cs         # [檢視] 確認輸出格式
│
├── ReleaseSync.Infrastructure/      # 基礎設施層
│   ├── Configuration/
│   │   ├── AzureDevOpsSettings.cs   # [修改] 新增 TeamMapping 屬性
│   │   ├── UserMappingSettings.cs   # [既有] 參考現有實作
│   │   └── TeamMappingSettings.cs   # [新增] 團隊對應配置
│   ├── Services/
│   │   ├── UserMappingService.cs    # [既有] 參考現有實作
│   │   └── TeamMappingService.cs    # [新增] 實作 ITeamMappingService
│   ├── Platforms/
│   │   ├── GitLab/
│   │   │   └── GitLabPullRequestRepository.cs  # [修改] 加入過濾邏輯
│   │   ├── BitBucket/
│   │   │   └── BitBucketPullRequestRepository.cs  # [修改] 加入過濾邏輯
│   │   └── AzureDevOps/
│   │       ├── AzureDevOpsWorkItemRepository.cs  # [修改] 加入過濾邏輯
│   │       └── AzureDevOpsApiClient.cs          # [檢視] 確認 team field 抓取
│   └── DependencyInjection/
│       └── TeamMappingServiceExtensions.cs  # [新增] DI 註冊
│
├── ReleaseSync.Console/             # 主控台層
│   ├── Program.cs                   # [檢視] 確認不需修改
│   └── appsettings.example.json     # [修改] 加入 TeamMapping 範例
│
└── tests/
    ├── ReleaseSync.Domain.UnitTests/
    │   └── Models/
    │       └── WorkItemInfoTests.cs  # [新增] Team 屬性測試
    ├── ReleaseSync.Infrastructure.UnitTests/
    │   ├── Services/
    │   │   └── TeamMappingServiceTests.cs  # [新增]
    │   └── Platforms/
    │       ├── GitLabPullRequestRepositoryTests.cs  # [修改] 加入過濾測試
    │       ├── BitBucketPullRequestRepositoryTests.cs  # [修改] 加入過濾測試
    │       └── AzureDevOpsWorkItemRepositoryTests.cs  # [修改] 加入過濾測試
    └── ReleaseSync.Integration.Tests/
        └── EndToEnd/
            └── FilteringWorkflowTests.cs  # [新增] 完整過濾流程測試
```

**Structure Decision**:
專案使用 Clean Architecture 分層架構:
- **Domain** 層包含核心實體和介面
- **Application** 層包含應用服務和 DTOs
- **Infrastructure** 層包含外部系統整合和配置
- **Console** 層是主程式入口

本功能主要修改 Infrastructure 層 (配置和 Repository 過濾邏輯) 和 Domain 層 (WorkItemInfo 加入 Team 屬性),新增 TeamMappingService 處理團隊對應邏輯。

## Complexity Tracking

*無 Constitution 違規事項需要說明*

本功能完全符合專案章程要求,不引入額外複雜度。過濾邏輯使用簡單的 LINQ 條件和 HashSet 查找,遵循 KISS 原則。

