# Implementation Plan: Release Branch 差異比對功能

**Branch**: `001-release-branch-diff` | **Date**: 2026-01-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-release-branch-diff/spec.md`

## Summary

此功能擴展 ReleaseSync 的 PR 抓取能力，從純粹的時間範圍查詢擴展到支援 Release Branch 比對模式。核心變更包括：配置結構調整（TargetBranches 從陣列改為單一值、新增 SyncOptions 全域設定、專案層級覆寫機制）以及新增 FetchMode 策略模式支援 DateRange 與 ReleaseBranch 兩種抓取方式。

## Technical Context

**Language/Version**: C# / .NET 9.0
**Primary Dependencies**: NGitLab（GitLab API）、HttpClient（BitBucket API）、Microsoft.Extensions.DependencyInjection、Serilog
**Storage**: N/A（僅呼叫外部 API，無本地儲存）
**Testing**: xUnit、Moq
**Target Platform**: Linux/Windows CLI（跨平台 Console Application）
**Project Type**: single（CLI 應用程式）
**Performance Goals**: 單一平台查詢 < 30 秒（100 筆 PR）
**Constraints**: 依賴外部 API 回應時間；需處理 API 認證與權限
**Scale/Scope**: 支援多平台（GitLab、BitBucket、Azure DevOps）、每平台多個專案

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

此功能設計必須符合以下憲章原則 (詳見 `.specify/memory/constitution.md`):

- **✅ DDD 戰術模式**: 是否清楚定義 Entity、Value Object、Aggregate 邊界?
  - `ReleaseBranchName` 為 Value Object，封裝命名規則驗證與日期解析
  - `FetchMode` 為列舉，定義抓取模式
  - `SyncOptions` 為配置聚合，管理全域同步選項
- **✅ CQRS 模式**: 命令與查詢是否明確分離?
  - 此功能為純查詢操作，不涉及狀態變更命令
- **✅ SOLID 原則**: 設計是否符合 SRP、OCP、LSP、ISP、DIP?
  - 透過 Strategy Pattern 實現 OCP（新增 FetchMode 不需修改現有程式碼）
  - 透過 DIP 依賴 `IPullRequestRepository` 介面
- **✅ TDD 強制執行**: 是否規劃先寫測試再實作?測試覆蓋率目標?
  - 所有 Value Object 需達 100% 覆蓋率
  - 核心業務邏輯（FetchMode 選擇、配置覆寫）需達 80% 覆蓋率
- **✅ KISS 原則**: 是否選擇最簡單的解決方案?是否避免過度設計?
  - 重用現有的 `BasePlatformService` 和 `BasePullRequestRepository` 架構
  - 不引入額外的抽象層或複雜設計模式
- **✅ 例外處理**: 例外處理策略是否明確?是否僅在必要時捕捉例外?
  - 當 ReleaseBranch 不存在時拋出明確例外
  - 配置驗證錯誤使用自訂例外類型
- **✅ 繁體中文**: 所有文件、註解、溝通是否使用繁體中文?
  - 所有 XML 註解與 inline comment 使用繁體中文
- **✅ 註解規範**: 是否規劃 XML 註解與 inline comment?
  - 所有公開類別、介面、方法需有 XML 註解
- **✅ 重用優先**: 是否搜尋並重用現有邏輯與元件?
  - 重用 `BranchName` Value Object 模式
  - 重用 `BasePullRequestRepository` 的 Template Method 架構
- **✅ Program.cs 最小化**: 是否保持 Program.cs 簡潔 (啟動、DI、組態)?
  - 新增 DI 註冊透過擴展方法，不影響 Program.cs 複雜度
- **✅ 分層架構**: 是否遵循 Domain → Application → Infrastructure → Presentation?
  - `FetchMode` 和 `ReleaseBranchName` 位於 Domain 層
  - 配置模型位於 Infrastructure 層
  - API 呼叫實作位於 Infrastructure 層

**複雜度警告**: 無違反原則，無需額外說明。

## Project Structure

### Documentation (this feature)

```text
specs/001-release-branch-diff/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── configuration-schema.json
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── ReleaseSync.Domain/
│   ├── Models/
│   │   ├── ReleaseBranchName.cs      # 新增：Release Branch 值物件
│   │   └── FetchMode.cs              # 新增：抓取模式列舉
│   └── Services/
│       └── IPlatformService.cs       # 修改：新增 FetchMode 支援
│
├── ReleaseSync.Application/
│   ├── DTOs/
│   │   └── SyncRequest.cs            # 修改：新增 ReleaseBranch 參數
│   └── Services/
│       └── SyncOrchestrator.cs       # 修改：支援 FetchMode 選擇
│
├── ReleaseSync.Infrastructure/
│   ├── Configuration/
│   │   ├── SyncOptionsSettings.cs    # 新增：全域同步選項
│   │   ├── GitLabSettings.cs         # 修改：TargetBranches → TargetBranch
│   │   └── BitBucketSettings.cs      # 修改：TargetBranches → TargetBranch
│   └── Platforms/
│       ├── GitLab/
│       │   ├── GitLabApiClient.cs    # 修改：新增分支比對 API
│       │   └── GitLabPullRequestRepository.cs  # 修改：支援 ReleaseBranch 模式
│       └── BitBucket/
│           ├── BitBucketApiClient.cs # 修改：新增分支比對 API
│           └── BitBucketPullRequestRepository.cs  # 修改：支援 ReleaseBranch 模式
│
└── ReleaseSync.Console/
    ├── appsettings.json              # 修改：配置結構調整
    └── Commands/
        └── SyncCommand.cs            # 修改：新增 --release-branch 參數

tests/
├── ReleaseSync.Domain.UnitTests/
│   └── Models/
│       ├── ReleaseBranchNameTests.cs # 新增
│       └── FetchModeTests.cs         # 新增
└── ReleaseSync.Infrastructure.UnitTests/
    └── Configuration/
        └── SyncOptionsSettingsTests.cs  # 新增
```

**Structure Decision**: 延續現有 Clean Architecture 分層結構，新增程式碼按照功能歸類至對應層級。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

無違反憲章原則，此表格留空。

## Implementation Phases

### Phase 1: Domain Layer - 值物件與列舉

1. 建立 `FetchMode` 列舉（DateRange、ReleaseBranch）
2. 建立 `ReleaseBranchName` 值物件（含命名格式驗證、日期解析）
3. 撰寫單元測試

### Phase 2: Infrastructure Layer - 配置結構

1. 建立 `SyncOptionsSettings` 全域同步選項
2. 修改 `GitLabProjectSettings`（TargetBranches → TargetBranch）
3. 修改 `BitBucketProjectSettings`（TargetBranches → TargetBranch）
4. 新增專案層級覆寫屬性（FetchMode、ReleaseBranch、StartDate、EndDate）
5. 更新 DI 擴展方法
6. 撰寫單元測試

### Phase 3: Infrastructure Layer - API 擴展

1. GitLabApiClient 新增 `GetBranchesAsync()` 和 `CompareBranchesAsync()`
2. BitBucketApiClient 新增 `GetBranchesAsync()` 和 `CompareBranchesAsync()`
3. 撰寫整合測試（需要真實 API 憑證）

### Phase 4: Repository 層 - FetchMode 支援

1. 修改 `GitLabPullRequestRepository` 支援 ReleaseBranch 模式
2. 修改 `BitBucketPullRequestRepository` 支援 ReleaseBranch 模式
3. 實作分支比對邏輯（最新版 vs 歷史版）
4. 撰寫單元測試

### Phase 5: Application Layer - Orchestrator 調整

1. 修改 `SyncRequest` DTO
2. 修改 `SyncOrchestrator` 支援 FetchMode 選擇與配置覆寫
3. 撰寫單元測試

### Phase 6: Console Layer - CLI 整合

1. 更新 `appsettings.json` 配置結構
2. 更新 `SyncCommand` 命令列參數
3. 端對端測試

## Dependencies

### External API Dependencies

| 平台 | 端點 | 用途 |
|------|------|------|
| GitLab | `GET /projects/:id/repository/branches` | 列出所有分支 |
| GitLab | `GET /projects/:id/repository/compare` | 比對兩個分支 |
| BitBucket | `GET /2.0/repositories/{workspace}/{repo_slug}/refs/branches` | 列出所有分支 |
| BitBucket | `GET /2.0/repositories/{workspace}/{repo_slug}/diffstat/{spec}` | 比對兩個分支 |

### Internal Dependencies

- 現有的 `BranchName` Value Object 作為設計參考
- 現有的 `DateRange` Value Object 作為設計參考
- 現有的 `BasePlatformService` Template Method 架構
- 現有的 `BasePullRequestRepository` Template Method 架構

## Risk Assessment

| 風險 | 影響 | 緩解策略 |
|------|------|----------|
| API 權限不足 | 分支比對 API 可能需要額外權限 | 文件說明所需權限，提供明確錯誤訊息 |
| Release Branch 命名不一致 | 部分 Repository 可能不遵循 `release/yyyyMMdd` 格式 | 提供專案層級覆寫，支援自訂格式 |
| 向後相容性 | TargetBranches → TargetBranch 變更可能破壞現有配置 | 考慮短期內同時支援兩種格式 |

## Next Steps

執行 `/speckit.tasks` 命令以產生詳細的任務清單。
