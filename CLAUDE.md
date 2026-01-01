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

## 常用開發命令

### 建置

```bash
# 建置整個解決方案
dotnet build src/src.sln

# 建置特定專案
dotnet build src/ReleaseSync.Console/ReleaseSync.Console.csproj

# Release 模式建置
dotnet build src/src.sln -c Release
```

### 測試

```bash
# 執行所有測試
dotnet test src/src.sln

# 執行特定測試專案
dotnet test src/tests/ReleaseSync.Domain.UnitTests/ReleaseSync.Domain.UnitTests.csproj

# 執行單一測試類別
dotnet test --filter "FullyQualifiedName~ReleaseSync.Domain.UnitTests.Models.DateRangeTests"

# 執行單一測試方法
dotnet test --filter "FullyQualifiedName~ReleaseSync.Domain.UnitTests.Models.DateRangeTests.Constructor_WithValidDates_ShouldCreateInstance"

# 執行測試並產生覆蓋率報告
dotnet test src/src.sln --collect:"XPlat Code Coverage"

# 僅執行單元測試 (排除整合測試)
dotnet test src/src.sln --filter "FullyQualifiedName!~Integration"

# 執行整合測試
dotnet test src/tests/ReleaseSync.Integration.Tests/ReleaseSync.Integration.Tests.csproj
```

### 執行應用程式

**重要**: 所有執行參數都從 `appsettings.json` 的 `SyncOptions` 區塊讀取,不再使用命令列參數。

```bash
# 從專案根目錄執行 (推薦)
# 先在 appsettings.json 中設定 SyncOptions 參數
dotnet run --project src/ReleaseSync.Console -- sync

# 或從 ReleaseSync.Console 目錄執行
cd src/ReleaseSync.Console
dotnet run -- sync

# 顯示說明
dotnet run --project src/ReleaseSync.Console -- --help
dotnet run --project src/ReleaseSync.Console -- sync --help
```

**參數設定範例** (appsettings.json):

```json
{
  "SyncOptions": {
    "StartDate": "2025-01-01",
    "EndDate": "2025-01-31",
    "EnableGitLab": true,
    "EnableBitBucket": false,
    "EnableAzureDevOps": false,
    "EnableExport": true,
    "OutputFile": "output.json",
    "Force": false,
    "Verbose": false,
    "EnableGoogleSheet": false,
    "GoogleSheetId": null,
    "GoogleSheetName": null
  }
}
```

### User Secrets 管理

```bash
# 設定 User Secrets
cd src/ReleaseSync.Console
dotnet user-secrets set "GitLab:PersonalAccessToken" "your-token-here"
dotnet user-secrets set "BitBucket:Email" "your-email@example.com"
dotnet user-secrets set "BitBucket:AccessToken" "your-token-here"
dotnet user-secrets set "AzureDevOps:PersonalAccessToken" "your-token-here"

# 列出所有 User Secrets
dotnet user-secrets list

# 移除特定 Secret
dotnet user-secrets remove "GitLab:PersonalAccessToken"

# 清除所有 Secrets
dotnet user-secrets clear
```

### 清理

```bash
# 清理建置產物
dotnet clean src/src.sln

# 清理並重新建置
dotnet clean src/src.sln && dotnet build src/src.sln
```

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

### 解決方案結構

- **解決方案檔案**: `src/src.sln` (注意:位於 `src` 目錄內,非專案根目錄)
- **測試專案位置**: `src/tests/` 目錄

### 目標框架

- **所有專案**: .NET 9.0
- Nullable Reference Types 已全面啟用

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

- **appsettings.json**: 非敏感設定 (API URLs, 專案清單, Regex 規則)
- **User Secrets**: 推薦的敏感資訊管理方式 (API Tokens),儲存在使用者設定檔中 (UserSecretsId: `1b985d8b-8619-4ade-87b8-1f41a1b54a7e`)
- **位置**: `src/ReleaseSync.Console/appsettings.json`

### 相依性注入架構

專案使用 Extension Methods 模式組織 DI 註冊:

- **平台服務註冊**: 各平台透過擴展方法註冊 (如 `AddGitLabServices()`, `AddBitBucketServices()`)
- **位置**: `ReleaseSync.Infrastructure/DependencyInjection/` 目錄
- **Program.cs**: 僅包含服務註冊呼叫,不包含複雜邏輯 (符合最小化原則)

範例:
```csharp
services.AddGitLabServices(configuration);
services.AddBitBucketServices(configuration);
services.AddAzureDevOpsServices(configuration);
```

### 擴展新平台支援

若要新增新的平台整合:

1. 在 `ReleaseSync.Infrastructure/Platforms/` 建立新資料夾
2. 實作 `IPullRequestRepository` 介面
3. 繼承 `BasePullRequestRepository` 以重用通用邏輯
4. 在 `ReleaseSync.Infrastructure/DependencyInjection/` 建立對應的 Extension Method
5. 在 `appsettings.json` 新增平台設定區塊
6. 在 `Program.cs` 中呼叫新的 DI 註冊方法
7. 在 `SyncCommand` 新增對應的命令列選項

### 程式碼風格

- **XML 文件註解**: 所有公開成員必須有完整的繁體中文 XML 註解
- **命名慣例**: 遵循 C# 標準慣例 (PascalCase for public, camelCase for private)
- **縮排**: 4 spaces (C# code), 2 spaces (JSON/XML)
- **Nullable Reference Types**: 已啟用,確保 null 安全

### Markdown 文件撰寫規範

- **僅在明確要求時撰寫**: 不要主動建立 .md 文件用於規劃、筆記或追蹤用途
- **優先記憶工作**: 規劃與追蹤資訊應保留在記憶中處理,而非建立文件
- **明確請求例外**: 僅當使用者明確要求特定名稱或路徑的 markdown 文件時才建立

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

- **專案憲章**: `.specify/memory/constitution.md` - 完整的開發原則與規範 (必讀)
- **README.md**: 使用者快速上手與功能說明
