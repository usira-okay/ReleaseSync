# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**重要:所有溝通請使用繁體中文 (ZH-TW)**

## 專案概述

ReleaseSync 是一個從多個平台 (GitLab, BitBucket, Azure DevOps) 聚合 Pull Request/Merge Request 變更資訊的工具,遵循 Clean Architecture 與 DDD Tactical Patterns。

## 專案憲章與核心原則

**本專案遵循嚴格的開發憲章** (詳見 `.specify/memory/constitution.md`),以下為關鍵原則:

### 設計原則 (不可妥協)

1. **DDD (領域驅動設計)**
   - 實體與值物件必須明確區分
   - 核心業務邏輯必須位於 Domain 層,不得依賴基礎設施層
   - 使用 Repository 模式進行資料存取抽象化

2. **CQRS 模式**
   - 命令 (Command) 負責狀態變更,不返回資料
   - 查詢 (Query) 負責資料讀取,不變更狀態
   - 命令與查詢的處理器必須明確分離

3. **SOLID 原則**
   - 所有程式碼必須遵循 SOLID 五大原則
   - 每個類別只有一個變更理由 (SRP)
   - 依賴抽象而非具體實作 (DIP)

4. **KISS (簡單至上)**
   - 遵循 YAGNI 原則,不實作當前不需要的功能
   - 優先選擇最簡單且能解決問題的方案
   - 避免過早最佳化

### TDD 要求 (強制執行)

**測試驅動開發為不可妥協的開發實踐:**

- ✅ **先寫測試**: 實作任何功能前必須先撰寫失敗的測試
- ✅ **紅燈-綠燈-重構**: 嚴格遵循 Red-Green-Refactor 循環
- ✅ **使用者審核**: 測試撰寫完成後必須經使用者審核才能進行實作
- ✅ **覆蓋率要求**: 核心業務邏輯必須達到 80% 以上單元測試覆蓋率
- ✅ **整合測試**: 變更契約、跨服務溝通、共用 Schema 時必須撰寫整合測試

### 程式碼註解規範 (強制執行)

- **XML 註解**: 所有公開的類別、介面、方法、屬性必須包含完整的繁體中文 `<summary>` 註解
- **參數說明**: 複雜方法必須包含 `<param>` 與 `<returns>` 說明
- **Inline Comment**: 複雜邏輯、重要決策、非直覺的實作必須包含 inline comment
- **註解品質**: 註解應說明「為什麼」而非「做什麼」,程式碼本身應該足夠清晰

### 例外處理策略

- **按需捕捉**: 僅在需求明確要求時才使用 try-catch
- **不吞例外**: 捕捉例外後必須進行有意義的處理,不得靜默忽略
- **自訂例外**: 使用自訂例外類型表達領域錯誤
- **失敗快速**: 在不可恢復的錯誤情況下,盡早失敗而非嘗試修復

### Program.cs 最小化原則

`Program.cs` 必須保持簡潔 (目標 50 行以內),僅負責:
- 應用程式啟動與建立主機
- 相依性注入註冊
- 組態載入
- **禁止**: 業務邏輯、複雜初始化、錯誤處理邏輯

### 開發流程規範

使用 SpecKit 命令進行結構化開發:

- **Plan 階段** (`/speckit.plan`):
  - 優先重用現有元件,避免重複
  - 清楚識別相依性
  - 設計方案必須符合所有核心原則

- **Task 階段** (`/speckit.tasks`, `/speckit.implement`):
  - 每個階段性任務必須標註:建置狀態、測試狀態、整合影響、回滾計畫

## 架構概覽

### Clean Architecture 分層

```
ReleaseSync.Domain/          # 核心領域層 (不依賴任何外層)
├── Models/                  # 實體與值物件 (PullRequestInfo, WorkItemInfo, etc.)
├── Services/                # 領域服務介面
└── Repositories/            # Repository 抽象介面

ReleaseSync.Application/     # 應用層 (依賴 Domain)
├── Services/                # 應用服務 (SyncOrchestrator - 主要編排邏輯)
├── DTOs/                    # 資料傳輸物件
└── Exporters/               # 匯出器 (JsonExporter)

ReleaseSync.Infrastructure/  # 基礎設施層 (依賴 Domain & Application)
├── Platforms/               # 外部平台整合
│   ├── GitLab/             # GitLab API 整合
│   ├── BitBucket/          # BitBucket API 整合
│   └── AzureDevOps/        # Azure DevOps API 整合
├── Configuration/           # 組態模型
├── Services/                # 基礎設施服務實作
└── Parsers/                 # Work Item ID 解析器

ReleaseSync.Console/         # 呈現層 (CLI)
├── Commands/                # System.CommandLine 命令定義
└── Handlers/                # 命令處理器
```

**相依性規則**:
- Domain Layer 不得依賴任何其他層
- Application Layer 透過介面依賴基礎設施,不得直接依賴具體實作
- Infrastructure Layer 實作 Domain 與 Application 定義的介面

### 關鍵設計模式

1. **Repository Pattern**: 所有平台整合都實作 `IPullRequestRepository` 介面
2. **Strategy Pattern**: 透過 `BasePlatformService` 和 `BasePullRequestRepository` 提供可擴展的平台支援
3. **Orchestrator Pattern**: `SyncOrchestrator` 協調多平台資料聚合流程
4. **Value Objects**: `BranchName`, `DateRange`, `WorkItemId` 等封裝領域概念
5. **Dependency Injection**: 使用 Microsoft.Extensions.DependencyInjection

### 資料流

```
CLI Command (SyncCommand)
    ↓
SyncOrchestrator (Application Layer)
    ↓
Multiple Platform Services (Infrastructure Layer)
    ↓ (parallel execution)
GitLabService | BitBucketService | AzureDevOpsService
    ↓
PullRequestInfo Collection (Domain Models)
    ↓
JsonExporter (Application Layer)
    ↓
output.json
```

## 開發重點

### 目標框架

- **主要專案**: .NET 9.0 (ReleaseSync.Console)
- **函式庫專案**: .NET 8.0 (Domain, Application, Infrastructure)
- 專案使用 `TreatWarningsAsErrors=true`,確保程式碼品質
- XML 文件生成已啟用 (`GenerateDocumentationFile=true`)

### 日誌記錄

- 使用 **Serilog** 進行結構化日誌記錄
- 預設等級: Information
- Verbose 模式 (`--verbose`): Debug 等級
- 日誌輸出不包含敏感資訊 (Token, Password)

### 測試策略

測試專案位於 `src/tests/`:
- `*.UnitTests`: 單元測試 (每個層級獨立)
- `*.Integration.Tests`: 整合測試 (實際 API 呼叫,需要真實憑證)

### 組態檔結構

- `appsettings.json`: 非敏感設定 (API URLs, 專案清單, Regex 規則) 與敏感資訊 (API Tokens)
- **User Secrets**: 推薦的敏感資訊管理方式,將 API Tokens 儲存在使用者設定檔中 (UserSecretsId: `1b985d8b-8619-4ade-87b8-1f41a1b54a7e`)

### 擴展新平台支援

若要新增新的平台整合:

1. 在 `ReleaseSync.Infrastructure/Platforms/` 建立新資料夾
2. 實作 `IPullRequestRepository` 介面
3. 繼承 `BasePullRequestRepository` 以重用通用邏輯
4. 在 `appsettings.json` 新增平台設定區塊
5. 在 DI 容器中註冊服務
6. 在 `SyncCommand` 新增對應的命令列選項

### 程式碼風格

- **XML 文件註解**: 所有公開成員必須有完整的繁體中文 XML 註解
- **命名慣例**: 遵循 C# 標準慣例 (PascalCase for public, camelCase for private)
- **縮排**: 4 spaces (C# code), 2 spaces (JSON/XML)
- **Nullable Reference Types**: 已啟用,確保 null 安全

### 時區處理

- 所有日期時間使用 `DateTimeOffset` 而非 `DateTime`
- 確保跨時區資料的一致性處理

### 錯誤處理原則

- 部分失敗容錯: 單一平台失敗不影響其他平台
- 友善的錯誤訊息: 提供具體的除錯步驟
- 結構化日誌: 記錄錯誤上下文以利問題追蹤

## 安全性要求

- **絕不**將 API Token 提交至版本控制
- 優先使用 User Secrets 管理敏感資訊
- 確保日誌輸出不包含 Token 或敏感資料
- 定期輪替 Personal Access Tokens

## 相關文件

- **專案憲章**: `.specify/memory/constitution.md` - 完整的開發原則與規範
- **技術規格**: `specs/002-pr-aggregation-tool/` - 詳細技術規格與設計決策
- **SpecKit 模板**: `.specify/templates/` - 文件與命令模板
