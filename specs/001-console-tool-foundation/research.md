# Research: Console 工具基礎架構技術選擇

**Feature**: ReleaseSync Console 工具基礎架構
**Research Date**: 2025-10-18
**Status**: Completed

## 概述

本文件整合了針對 ReleaseSync Console 應用程式基礎架構的技術研究結果,解決了 Technical Context 中所有 NEEDS CLARIFICATION 項目。

---

## 研究主題 1: 命令列參數解析函式庫選擇

### 決策

**✅ 採用 System.CommandLine (2.0.0-rc.2 或後續版本)**

### 理由

1. **官方支援與長期維護保證**
   - Microsoft 官方開發與維護,是 .NET CLI 本身使用的底層函式庫
   - 預計於 2025 年 11 月與 .NET 10 同步發布穩定版 2.0.0
   - 目前版本: 2.0.0-rc.2 (Release Candidate,API 已穩定)

2. **效能表現優異**
   - 啟動時間提升 12%,參數解析速度提升 40%
   - 函式庫大小減少 32%,NativeAOT 應用程式大小減少 20%
   - 支援 Trim-friendly 和 Native AOT 編譯

3. **功能完整且現代化**
   - 符合 POSIX 和 Windows 命令列慣例
   - 內建 Tab 自動補全支援
   - 支援子命令 (subcommands)、參數驗證、自動生成說明文字
   - 現代化的 API 設計,符合 .NET 最新設計慣例

4. **符合專案需求**
   - 滿足 FR-003 (參數解析服務)
   - 滿足 FR-008 (便於依賴注入整合)
   - 滿足 SC-001 (3 秒內啟動要求)

5. **未來擴展性**
   - 當 ReleaseSync 需要新增子命令時,架構易於擴展
   - Tab 自動補全提升使用者體驗
   - Response Files 支援複雜參數傳遞

### 替代方案考量

**CommandLineParser**:
- ✅ 優點: 穩定版、使用簡單、2,600 萬+下載量
- ❌ 缺點: 維護活躍度降低、非官方支援、缺乏 Native AOT 優化
- 評估: 適合需要立即穩定版的專案,但長期維護風險較高

**自訂實作**:
- ✅ 優點: 完全控制、無外部依賴
- ❌ 缺點: 開發成本高、功能不完整、違反 KISS 原則
- 評估: 不適合生產環境,重複發明輪子

### 實作要點

```csharp
// 安裝套件
dotnet add package System.CommandLine --version 2.0.0-rc.2.25502.107

// 基本使用範例
var platformOption = new Option<string>(
    aliases: new[] { "--platform", "-p" },
    description: "版控平台類型 (github/gitlab)")
{
    IsRequired = true
};

var rootCommand = new RootCommand("ReleaseSync - 版控平台 PR/MR 資訊擷取工具");
rootCommand.AddOption(platformOption);
rootCommand.SetHandler(async (platform) => {
    // 處理邏輯
}, platformOption);

return await rootCommand.InvokeAsync(args);
```

---

## 研究主題 2: 日誌框架選擇與結構化日誌策略

### 決策

**✅ 採用 Serilog**

### 理由

1. **完美符合 KISS 原則**
   - 基本設定僅需 3-5 行程式碼
   - API 設計直觀,學習曲線平緩

2. **原生結構化日誌支援**
   - 天生為結構化日誌設計,非常適合記錄 PR/MR 資料
   - 使用 `@` 運算子可輕鬆解構複雜物件:
     ```csharp
     Log.Information("Fetched PR {@PullRequest} from {Platform}", pullRequest, "GitHub");
     ```

3. **優異的效能**
   - .NET 9 最佳化後效能提升約 50%
   - 延遲約為 NLog 的一半,吞吐量為兩倍

4. **豐富的生態系統**
   - 超過 200 個 sinks (輸出目標)
   - 未來可輕鬆擴展到檔案、雲端服務 (Azure Application Insights、Seq 等)

5. **社群高度認可**
   - 2025 年 .NET 社群普遍推薦
   - GitHub 超過 6,500 顆星,持續維護
   - 完整的官方文件與範例

### 替代方案考量

**Microsoft.Extensions.Logging (MEL)**:
- ✅ 優點: .NET 內建、官方支援、零依賴
- ❌ 缺點: 結構化日誌支援較弱、輸出目標有限
- 評估: 適合極簡專案,但 ReleaseSync 需要更強的結構化日誌能力

**NLog**:
- ✅ 優點: 成熟穩定、彈性設定
- ❌ 缺點: 設定複雜 (XML 為主)、效能較 Serilog 差
- 評估: 適合偏好 XML 設定檔的團隊

**Serilog + MEL 組合**:
- 可使用 Serilog 作為 MEL 的提供者 (`AddSerilog()`)
- 適合需要與現有 MEL 基礎設施整合的專案

### 結構化日誌最佳實踐

#### 1. 日誌等級使用指南

```csharp
// ❌ 錯誤:不應記錄敏感資料
Log.Information("User logged in with token: {Token}", token);

// ✅ 正確:遮罩敏感資料
Log.Information("User logged in with token: {Token}", MaskToken(token));

// 日誌等級使用
Log.Verbose("詳細診斷資訊 (開發環境)");
Log.Debug("除錯資訊 (開發環境)");
Log.Information("關鍵事件 (生產環境) - 如 PR 抓取成功");
Log.Warning("警告 (生產環境) - 如 API Rate Limit 接近上限");
Log.Error(ex, "錯誤 (生產環境) - 如 API 呼叫失敗");
Log.Fatal(ex, "嚴重錯誤 (生產環境) - 如應用程式無法啟動");
```

#### 2. 敏感資料處理

```csharp
// Token 遮罩函式
public static string MaskToken(string token)
{
    if (string.IsNullOrEmpty(token) || token.Length <= 8)
        return "***";

    return $"{token.Substring(0, 4)}...{token.Substring(token.Length - 4)}";
}

// 使用範例
Log.Information("Authenticating to {Platform} with token {Token}",
    platform, MaskToken(token));
// 輸出: Authenticating to GitHub with token ghp_...x7Kw
```

#### 3. 結構化資料記錄

```csharp
// ❌ 錯誤:字串插補破壞結構化
Log.Information($"Fetched PR {pr.Number} from {repository}");

// ✅ 正確:使用命名參數
Log.Information("Fetched PR {PullRequestNumber} from {Repository}",
    pr.Number, repository);

// ✅ 更好:解構整個物件
Log.Information("Fetched PR {@PullRequest} from {Repository}",
    pr, repository);
// 輸出 JSON: {"PullRequest":{"Number":123,"Title":"Add feature","State":"open"},"Repository":"owner/repo"}
```

### 實作要點

**基本設定 (Program.cs)**:

```csharp
using Serilog;

// 最簡化設定 (3 行程式碼)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("ReleaseSync Console Tool starting");

    // 建立 Host 並執行應用程式邏輯
    // ... (依賴注入、服務註冊等)

    Log.Information("ReleaseSync completed successfully");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();  // 確保所有日誌都被寫入
}
```

**生產級設定**:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ReleaseSync")
    .Enrich.WithMachineName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/releasesync-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

**安裝套件**:

```bash
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File  # 如果需要檔案輸出
```

---

## 研究主題 3: 測試專案組織最佳實踐

### 決策

**✅ 採用 src/tests 分離結構 + 按測試類型和專案雙軸分離**

### 理由

1. **清晰的關注點分離**
   - src 包含生產程式碼,tests 包含測試程式碼,一目了然
   - 符合 .NET 社群慣例與 Microsoft 官方文件

2. **滿足憲章要求**
   - 支援三種測試類型 (契約、整合、單元)
   - 使用 xUnit 作為測試框架
   - 使用 FluentAssertions 進行斷言
   - 使用 Moq 或 NSubstitute 進行模擬

3. **執行速度分離**
   - 單元測試應 <100ms,可頻繁執行
   - 整合測試較慢 (涉及資料庫、網路),應分離以避免拖慢開發流程
   - 契約測試需要獨立執行環境,應獨立管理

4. **CI/CD 彈性**
   - 可分階段執行不同類型測試
   - 每次 commit 執行單元測試 (快速反饋)
   - PR 時執行整合測試
   - 定期執行契約測試

### 推薦的專案結構

```
ReleaseSync/
├── src/
│   ├── ReleaseSync.Console/           # 主要的 Console 應用程式
│   │   ├── ReleaseSync.Console.csproj
│   │   ├── Program.cs
│   │   ├── Services/
│   │   │   ├── ICommandLineParserService.cs
│   │   │   ├── CommandLineParserService.cs
│   │   │   ├── IDataFetchingService.cs
│   │   │   └── DataFetchingService.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── ReleaseSync.Core/              # 核心業務邏輯 (未來階段)
│   ├── ReleaseSync.Application/       # 應用層 (未來階段)
│   └── ReleaseSync.Infrastructure/    # 基礎設施層 (未來階段)
│
├── tests/
│   ├── ReleaseSync.Console.UnitTests/
│   │   ├── ReleaseSync.Console.UnitTests.csproj
│   │   ├── Services/
│   │   │   ├── CommandLineParserServiceTests.cs
│   │   │   └── DataFetchingServiceTests.cs
│   │   ├── Fixtures/
│   │   └── TestHelpers/
│   │
│   ├── ReleaseSync.Infrastructure.IntegrationTests/  # 未來階段
│   │   ├── ReleaseSync.Infrastructure.IntegrationTests.csproj
│   │   ├── ExternalServices/
│   │   │   ├── GitHubApiClientTests.cs
│   │   │   └── GitLabApiClientTests.cs
│   │   └── Fixtures/
│   │
│   └── ReleaseSync.Console.ContractTests/  # 未來階段
│       ├── ReleaseSync.Console.ContractTests.csproj
│       ├── GitHub/
│       ├── GitLab/
│       └── Schemas/
│
└── ReleaseSync.sln
```

### 測試命名慣例

#### 測試專案命名

- 單元測試: `{SourceProject}.UnitTests`
- 整合測試: `{SourceProject}.IntegrationTests`
- 契約測試: `{SourceProject}.ContractTests`

#### 測試類別命名

**推薦方案: 傳統 Tests 後綴 (適合本階段簡單服務)**

```csharp
// 檔案: CommandLineParserServiceTests.cs
namespace ReleaseSync.Console.UnitTests.Services
{
    public class CommandLineParserServiceTests
    {
        [Fact(DisplayName = "當服務被呼叫時,應拋出 NotImplementedException")]
        public void Parse_ThrowsNotImplementedException_WhenCalled()
        {
            // Arrange
            var service = new CommandLineParserService();

            // Act
            Action act = () => service.Parse("github", "token", "owner/repo");

            // Assert
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*尚未實作*");
        }
    }
}
```

#### 測試方法命名

**推薦命名模式**:
```
{MethodName}_{ExpectedBehavior}_{Scenario}
```

**範例**:
```csharp
// ✅ 良好範例
Parse_ThrowsNotImplementedException_WhenCalled()
FetchData_ThrowsNotImplementedException_WhenCalled()

// ✅ 使用 DisplayName 提供繁體中文描述
[Fact(DisplayName = "當參數有效時,應返回有效的選項")]
public void Parse_ReturnsValidOptions_WhenArgumentsAreValid()
{
    // 測試實作
}
```

### 測試資料與 Fixture 管理

**使用 xUnit IClassFixture 管理昂貴資源**:

```csharp
// Fixtures/ServiceFixture.cs
public class ServiceFixture : IDisposable
{
    public CommandLineParserService ParserService { get; }

    public ServiceFixture()
    {
        // 初始化共用資源 (執行一次)
        ParserService = new CommandLineParserService();
    }

    public void Dispose()
    {
        // 清理資源
    }
}

// 測試類別使用 Fixture
public class CommandLineParserServiceTests : IClassFixture<ServiceFixture>
{
    private readonly ServiceFixture _fixture;

    public CommandLineParserServiceTests(ServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Parse_ThrowsNotImplementedException_WhenCalled()
    {
        // 使用 _fixture.ParserService 進行測試
    }
}
```

### 測試專案必要套件

```xml
<ItemGroup>
  <!-- 測試框架 -->
  <PackageReference Include="xunit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
  <PackageReference Include="Microsoft.NET.Test.SDK" Version="17.9.0" />

  <!-- 斷言 -->
  <PackageReference Include="FluentAssertions" Version="6.12.0" />

  <!-- 模擬 -->
  <PackageReference Include="Moq" Version="4.20.0" />
  <!-- 或使用 NSubstitute -->
  <!-- <PackageReference Include="NSubstitute" Version="5.1.0" /> -->
</ItemGroup>
```

### 替代方案考量

**單一測試專案包含所有測試**:
- ✅ 優點: 專案管理簡單
- ❌ 缺點: 無法分離依賴、CI/CD 不靈活、違反單一職責原則
- 評估: ❌ 不推薦,除非專案非常小型 (< 50 個測試)

**使用 NUnit 或 MSTest**:
- NUnit: 成熟穩定,但共用測試上下文可能導致污染
- MSTest: Microsoft 官方,但社群支援不如 xUnit
- 評估: ✅ 遵循憲章要求使用 xUnit

---

## 總結與後續行動

### 技術決策總覽

| 技術領域 | 決策 | 關鍵理由 |
|---------|------|---------|
| **命令列參數解析** | System.CommandLine 2.0.0-rc.2 | 官方支援、效能優異、功能完整 |
| **日誌框架** | Serilog | 結構化日誌原生支援、KISS 原則、效能優異 |
| **測試框架** | xUnit + FluentAssertions + Moq | 憲章要求、現代化設計、社群認可 |
| **測試專案結構** | src/tests 分離 + 按類型與專案雙軸 | 清晰分離、CI/CD 彈性、符合最佳實踐 |

### 更新的 Technical Context

所有 NEEDS CLARIFICATION 項目已解決:

- ✅ **命令列參數解析函式庫**: System.CommandLine
- ✅ **日誌框架**: Serilog
- ✅ **測試專案組織**: src/tests 分離 + 單元測試專案

### 下一步行動

1. **Phase 1 設計階段**:
   - 產生 data-model.md (本階段無實際 domain model,可略過或僅記錄未來規劃)
   - 產生 contracts/ (定義服務介面契約)
   - 產生 quickstart.md (快速入門指南)

2. **實作階段準備**:
   - 建立專案結構 (src/, tests/)
   - 安裝必要套件
   - 設定 .editorconfig 與編譯選項
   - 撰寫第一批測試 (NotImplementedException 驗證)

---

**Research Completed**: 2025-10-18
**Researched by**: Claude (Anthropic)
**Next Phase**: Phase 1 - Design & Contracts
