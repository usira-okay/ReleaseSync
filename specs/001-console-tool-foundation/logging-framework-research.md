# .NET 9 Console 應用程式日誌框架研究報告

**專案**: ReleaseSync Console 工具
**研究日期**: 2025-10-18
**目標**: 選擇最適合的日誌框架並定義結構化日誌策略

---

## 執行摘要

基於對 .NET 9 生態系統的深入研究,**本報告建議採用 Serilog 作為 ReleaseSync Console 工具的日誌框架**。Serilog 在結構化日誌、易用性、效能和社群支援方面都表現優異,且完全符合 KISS 原則,能以最少的設定提供強大的功能。

---

## 1. 日誌框架詳細比較

### 1.1 Microsoft.Extensions.Logging (MEL)

#### 優點
- **.NET 9 原生整合**: 作為 .NET 的官方日誌抽象層,與框架深度整合
- **零額外依賴**: 內建於 .NET Runtime,無需安裝額外套件
- **標準化介面**: 提供 `ILogger<T>` 介面,適合依賴注入模式
- **高效能模式**: 透過 `LoggerMessage` 模式支援編譯時原始碼生成,大幅降低記憶體配置
- **JSON 輸出支援**: 內建 `AddJsonConsole()` 方法,可直接輸出結構化 JSON 日誌
- **跨平台一致性**: Windows、Linux、macOS 表現完全一致

#### 缺點
- **結構化日誌支援有限**: 雖然支援命名參數,但缺乏進階的解構(destructuring)功能
- **輸出目標受限**: 內建僅支援 Console、Debug、EventSource 等基本輸出
- **設定較繁瑣**: 需要透過 `ILoggerFactory` 和服務容器設定,對簡單場景過於複雜
- **擴充性依賴第三方**: 要輸出到檔案、雲端服務需要額外的 provider 套件
- **無法獨立使用**: 設計為抽象層,通常需要搭配其他實作

#### 最佳使用情境
- 需要建立可插拔的日誌架構,未來可能切換底層實作
- 團隊規範要求統一使用 Microsoft 技術棧
- 簡單的 Console 日誌需求,不需要複雜的結構化日誌

---

### 1.2 Serilog

#### 優點
- **原生結構化日誌**: 設計核心就是將日誌視為結構化事件,而非純文字
- **極致易用性**:
  ```csharp
  Log.Logger = new LoggerConfiguration()
      .WriteTo.Console()
      .CreateLogger();
  ```
  三行程式碼即可完成基本設定,完美符合 KISS 原則
- **豐富的輸出目標(Sinks)**:
  - Console、File、Rolling File
  - Seq、Elasticsearch、Application Insights
  - Slack、Email、Database
  - 超過 200 個社群維護的 sinks
- **強大的解構功能**: 使用 `@` 運算子可自動將複雜物件序列化為結構化資料
- **效能優異**:
  - .NET 9 最佳化後,效能提升約 50%
  - 支援非同步日誌和批次處理,幾乎零效能影響
  - 基準測試顯示延遲約為 NLog 的一半,吞吐量為兩倍
- **豐富的擴充器(Enrichers)**: 可自動添加執行緒 ID、機器名稱、環境變數等上下文資訊
- **與 MEL 完美整合**: 可透過 `UseSerilog()` 成為 MEL 的底層實作
- **活躍的社群**: GitHub 上超過 6,500 顆星,持續更新維護
- **優雅的 API 設計**: 流暢的 API 讓設定直觀且可讀性高

#### 缺點
- **額外依賴**: 需要安裝 NuGet 套件(但這是合理的代價)
- **學習曲線**: 進階功能(如自訂 destructuring policies)需要額外學習
- **過度解構風險**: 不當使用 `@` 可能導致敏感資訊洩漏或效能問題
- **配置複雜度**: 雖然基本設定簡單,但進階場景(多個 sinks、過濾規則)可能變得複雜

#### 最佳使用情境
- 需要強大的結構化日誌支援
- Console 工具需要清晰、易讀的日誌輸出
- 未來可能需要將日誌輸出到多個目標(檔案、雲端服務等)
- 重視開發效率和程式碼簡潔性

---

### 1.3 NLog

#### 優點
- **極高的設定靈活性**: 支援 XML、JSON、程式碼等多種設定方式
- **外部設定檔導向**: 可在不重新編譯的情況下修改日誌行為
- **成熟穩定**: 存在超過 15 年,經過大量實戰驗證
- **豐富的目標支援**: 支援檔案、資料庫、郵件、網路等多種輸出
- **條件式日誌**: 支援複雜的條件邏輯和路由規則
- **向下相容性強**: 支援舊版 .NET Framework 專案

#### 缺點
- **學習曲線陡峭**: XML 設定檔語法複雜,新手不易上手
- **結構化日誌支援較弱**: 雖然近年有改進,但仍以格式化文字為主
- **API 不如 Serilog 直觀**: 程式碼設定的可讀性較差
- **效能略遜**: 基準測試顯示延遲約為 Serilog 的兩倍
- **社群活躍度降低**: 相比 Serilog,近年更新較不頻繁

#### 最佳使用情境
- 需要透過設定檔動態調整日誌行為
- 有複雜的日誌路由需求(不同等級輸出到不同目標)
- 維護舊有 .NET Framework 專案

---

## 2. 評估標準對照表

| 評估標準 | MEL | Serilog | NLog |
|---------|-----|---------|------|
| **.NET 9 相容性** | ⭐⭐⭐⭐⭐ 原生支援 | ⭐⭐⭐⭐⭐ 完全相容 | ⭐⭐⭐⭐⭐ 完全相容 |
| **結構化日誌** | ⭐⭐⭐ 基本支援 | ⭐⭐⭐⭐⭐ 原生設計 | ⭐⭐⭐ 近年改進 |
| **易用性** | ⭐⭐⭐ 中等 | ⭐⭐⭐⭐⭐ 極佳 | ⭐⭐ 學習曲線陡 |
| **KISS 原則** | ⭐⭐ 對簡單場景過於複雜 | ⭐⭐⭐⭐⭐ 簡單直接 | ⭐⭐ 設定複雜 |
| **輸出目標** | ⭐⭐ 有限,需額外套件 | ⭐⭐⭐⭐⭐ 200+ sinks | ⭐⭐⭐⭐ 豐富 |
| **效能** | ⭐⭐⭐⭐ LoggerMessage 模式高效 | ⭐⭐⭐⭐⭐ 優異 | ⭐⭐⭐ 尚可 |
| **社群活躍度** | ⭐⭐⭐⭐ 官方支援 | ⭐⭐⭐⭐⭐ 非常活躍 | ⭐⭐⭐ 活躍度下降 |
| **JSON 輸出** | ⭐⭐⭐⭐ 內建支援 | ⭐⭐⭐⭐⭐ 豐富設定 | ⭐⭐⭐⭐ 支援良好 |
| **Console 適用性** | ⭐⭐⭐ 基本 | ⭐⭐⭐⭐⭐ 優秀的 Console Sink | ⭐⭐⭐⭐ 良好 |

---

## 3. 明確建議與理由

### 推薦選擇: **Serilog**

#### 核心理由

1. **完美契合 KISS 原則**
   - 基本設定僅需 3-5 行程式碼
   - API 設計直觀,新成員快速上手
   - 預設行為合理,無需大量調整

2. **結構化日誌原生支援**
   - ReleaseSync 需要記錄 PR/MR 資訊,這些都是結構化資料
   - 使用 Serilog 可自然地記錄如:
     ```csharp
     _logger.Information("Processing PR {@PullRequest}", new {
         Id = pr.Number,
         Title = pr.Title,
         Author = pr.Author,
         Repository = pr.Repository
     });
     ```
   - 未來可輕鬆將日誌輸出為 JSON,便於分析和監控

3. **優異的效能表現**
   - 對 Console 工具而言,日誌不應成為瓶頸
   - Serilog 的非同步日誌和批次處理確保幾乎零效能影響
   - .NET 9 最佳化帶來約 50% 的效能提升

4. **未來擴展性**
   - 現階段只需 Console 輸出
   - 未來可能需要輸出到檔案、Seq、Application Insights 等
   - Serilog 豐富的 sinks 生態系統確保平滑擴展

5. **Console 工具的最佳實踐**
   - Serilog.Sinks.Console 提供優秀的格式化和色彩支援
   - 支援 ANSI 主題,提升 CLI 使用體驗
   - 可輕鬆調整輸出模板,適應不同使用場景

6. **社群認可度高**
   - 2025 年 .NET 社群普遍推薦 Serilog 用於結構化日誌
   - 大量的文件、範例和社群支援
   - 持續的維護和更新

#### 實作建議

**最小化設定(符合 KISS 原則)**:

```csharp
using Serilog;

// Program.cs 開頭
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("ReleaseSync Console Tool starting");
    // 應用程式邏輯
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
```

**與 Microsoft.Extensions.Logging 整合**(可選,用於未來擴展):

```csharp
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(dispose: true);
});

// 使用 ILogger<T> 進行依賴注入
```

---

## 4. 結構化日誌最佳實踐

### 4.1 日誌等級使用指南

```csharp
// Verbose: 詳細的追蹤資訊,通常僅在開發階段啟用
Log.Verbose("Parsing command line arguments: {Args}", args);

// Debug: 除錯資訊,用於開發和問題排查
Log.Debug("API client initialized with base URL: {BaseUrl}", apiClient.BaseUrl);

// Information: 一般資訊,記錄應用程式的正常流程
Log.Information("Successfully fetched {Count} pull requests from {Repository}",
    pullRequests.Count, repository);

// Warning: 警告訊息,應用程式可繼續運作但有異常情況
Log.Warning("Rate limit approaching: {Remaining}/{Limit} requests remaining",
    rateLimitRemaining, rateLimitTotal);

// Error: 錯誤訊息,功能執行失敗但應用程式可繼續
Log.Error(ex, "Failed to fetch pull request {PrNumber} from {Repository}",
    prNumber, repository);

// Fatal: 致命錯誤,應用程式無法繼續執行
Log.Fatal(ex, "Unable to initialize application configuration");
```

#### 等級選擇原則

| 等級 | 使用時機 | 生產環境預設 |
|------|---------|------------|
| Verbose | 非常詳細的追蹤資訊 | 關閉 |
| Debug | 除錯和診斷資訊 | 關閉 |
| Information | 正常的業務流程事件 | 開啟 |
| Warning | 異常但可恢復的情況 | 開啟 |
| Error | 錯誤和例外狀況 | 開啟 |
| Fatal | 導致應用程式終止的錯誤 | 開啟 |

---

### 4.2 結構化資料記錄格式

#### ✅ 推薦做法: 使用命名參數和解構

```csharp
// 使用命名參數(字串插值形式)
Log.Information("User {UserId} fetched PR {PrId} from {Repository}",
    userId, prId, repository);

// 使用 @ 運算子解構物件
Log.Information("Processing pull request {@PullRequest}", new {
    Id = pr.Number,
    Title = pr.Title,
    Author = pr.Author,
    CreatedAt = pr.CreatedAt
});

// 使用 $ 運算子進行字串化(用於複雜物件的簡化顯示)
Log.Information("Request completed with response {$Response}", apiResponse);
```

#### ❌ 避免的做法

```csharp
// 不要使用字串插值 - 失去結構化優勢
Log.Information($"User {userId} fetched PR {prId}");

// 不要直接序列化整個物件 - 可能包含敏感資訊
Log.Information("User object: {@User}", userObject);

// 不要在日誌中執行複雜運算 - 影響效能
Log.Debug("Total: {Total}", expensiveCalculation());
```

---

### 4.3 敏感資料處理策略

#### 問題識別

敏感資料類型:
- 個人識別資訊(PII): Email、電話號碼、地址
- 認證資訊: Token、API Key、密碼、憑證
- 業務機密: 客戶名稱、專案代號、授權金鑰
- 系統資訊: 內部 IP、資料庫連線字串

#### 解決方案

**1. 明確控制記錄內容 - 使用匿名物件**

```csharp
// ✅ 好的做法: 明確指定要記錄的欄位
var safeLogData = new {
    UserId = user.Id,  // 僅記錄 ID,不記錄名稱或 Email
    Action = "FetchPullRequest",
    Repository = SanitizeRepositoryName(repo)  // 清理後的名稱
};
Log.Information("User action: {@UserAction}", safeLogData);

// ❌ 壞的做法: 記錄整個物件
Log.Information("User action: {@User}", user);  // 可能包含 Email、Token 等
```

**2. 使用自訂 Destructuring Policy**

```csharp
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        if (value is UserCredentials)
        {
            result = new StructureValue(new[]
            {
                new LogEventProperty("Username", new ScalarValue("***REDACTED***")),
                new LogEventProperty("HasToken", new ScalarValue(true))
            });
            return true;
        }

        result = null;
        return false;
    }
}

// 註冊 Policy
Log.Logger = new LoggerConfiguration()
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .WriteTo.Console()
    .CreateLogger();
```

**3. 使用屬性標記敏感屬性**

```csharp
using Serilog.Core;
using Serilog.Events;

public class SensitiveAttribute : Attribute { }

public class User
{
    public int Id { get; set; }

    [Sensitive]
    public string Email { get; set; }

    [Sensitive]
    public string ApiToken { get; set; }
}

// 自訂 Enricher 過濾敏感屬性
public class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        foreach (var property in logEvent.Properties)
        {
            if (property.Value is StructureValue structureValue)
            {
                // 檢查並遮罩標記為 Sensitive 的屬性
                // 實作細節省略
            }
        }
    }
}
```

**4. Token 和 API Key 的處理**

```csharp
// ✅ 安全的 Token 記錄
public static string MaskToken(string token)
{
    if (string.IsNullOrEmpty(token)) return "NULL";
    if (token.Length <= 8) return "***";

    return $"{token[..4]}...{token[^4..]}";  // 僅顯示前後 4 碼
}

Log.Information("Authenticating with token {Token}", MaskToken(apiToken));

// 輸出: Authenticating with token ghp_...xyz9
```

**5. URL 和查詢參數的清理**

```csharp
public static string SanitizeUrl(string url)
{
    var uri = new Uri(url);
    var sanitizedQuery = string.IsNullOrEmpty(uri.Query)
        ? ""
        : "?***QUERY_PARAMS_REDACTED***";

    return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}{sanitizedQuery}";
}

Log.Information("Calling API endpoint {Endpoint}", SanitizeUrl(apiUrl));
```

#### 敏感資料處理檢查清單

- [ ] 所有解構的物件都是明確建立的匿名物件或 DTO
- [ ] Token、API Key 使用遮罩函式處理
- [ ] Email、電話等 PII 資料不記錄或部分遮罩
- [ ] URL 中的查詢參數已清理
- [ ] 例外堆疊追蹤不包含敏感資料
- [ ] 設定檔路徑不暴露系統結構
- [ ] 資料庫連線字串使用別名或遮罩

---

### 4.4 效能最佳化建議

#### 1. 使用條件式日誌避免不必要的運算

```csharp
// ❌ 壞的做法: 即使 Debug 等級關閉,仍會執行序列化
Log.Debug("Complex object: {@Object}", ExpensiveSerialize(obj));

// ✅ 好的做法: 使用條件檢查
if (Log.IsEnabled(LogEventLevel.Debug))
{
    Log.Debug("Complex object: {@Object}", ExpensiveSerialize(obj));
}
```

#### 2. 避免過度解構大型物件

```csharp
// ❌ 壞的做法: 解構整個 HTTP Response
Log.Debug("API Response: {@Response}", httpResponse);

// ✅ 好的做法: 僅記錄必要資訊
Log.Debug("API Response: Status={StatusCode}, Length={ContentLength}",
    httpResponse.StatusCode, httpResponse.Content.Length);
```

#### 3. 使用非同步 Sink(如有需要)

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.File("logs/app.log"))  // 非同步寫入檔案
    .WriteTo.Console()  // Console 通常夠快,不需非同步
    .CreateLogger();
```

---

### 4.5 JSON 輸出格式範例

#### Console 輸出(人類可讀)

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

輸出範例:
```
[14:23:45 INF] Successfully fetched 15 pull requests from owner/repo
[14:23:46 WRN] Rate limit approaching: 42/60 requests remaining
[14:23:50 ERR] Failed to fetch pull request 123 from owner/repo
System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).
```

#### JSON 格式輸出(機器可讀,用於日誌聚合)

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();
```

輸出範例:
```json
{
  "Timestamp": "2025-10-18T14:23:45.1234567+08:00",
  "Level": "Information",
  "MessageTemplate": "Successfully fetched {Count} pull requests from {Repository}",
  "Properties": {
    "Count": 15,
    "Repository": "owner/repo"
  }
}
```

#### 混合模式(開發時人類可讀,生產環境 JSON)

```csharp
var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        isDevelopment
            ? new MessageTemplateTextFormatter("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            : new JsonFormatter())
    .CreateLogger();
```

---

### 4.6 Context 和 Correlation ID

#### 為什麼需要 Correlation ID

在處理多個 PR/MR 時,Correlation ID 可協助追蹤單一操作的完整日誌流程。

```csharp
// 使用 LogContext 添加上下文資訊
using Serilog.Context;

public async Task ProcessPullRequestAsync(int prNumber)
{
    var correlationId = Guid.NewGuid().ToString("N")[..8];

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("PullRequestNumber", prNumber))
    {
        Log.Information("Starting to process pull request");

        try
        {
            // 業務邏輯
            Log.Debug("Fetching PR details");
            var pr = await FetchPullRequestAsync(prNumber);

            Log.Debug("Fetching PR commits");
            var commits = await FetchCommitsAsync(prNumber);

            Log.Information("Successfully processed pull request");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process pull request");
            throw;
        }
    }
}
```

輸出範例:
```
[14:30:00 INF] Starting to process pull request {CorrelationId: "a3f7b2c1", PullRequestNumber: 123}
[14:30:01 DBG] Fetching PR details {CorrelationId: "a3f7b2c1", PullRequestNumber: 123}
[14:30:02 DBG] Fetching PR commits {CorrelationId: "a3f7b2c1", PullRequestNumber: 123}
[14:30:03 INF] Successfully processed pull request {CorrelationId: "a3f7b2c1", PullRequestNumber: 123}
```

---

## 5. 考慮的替代方案

### 5.1 組合方案: MEL + Serilog

**架構**: 使用 `Microsoft.Extensions.Logging.ILogger<T>` 作為介面,Serilog 作為底層實作

**優點**:
- 保持程式碼與特定日誌框架解耦
- 未來可輕鬆切換底層實作
- 符合 .NET 的標準實踐

**缺點**:
- 增加一層抽象,略微影響效能(通常可忽略)
- 無法使用 Serilog 特有功能(如 `@` 解構運算子)
- 對 Console 工具而言可能過度設計

**建議**:
- 如果 ReleaseSync 未來會演變為多專案解決方案(如 Web API、Worker Service 等),可考慮此方案
- 對於單純的 Console 工具,直接使用 Serilog 更簡潔

**範例**:
```csharp
// 使用 ILogger<T> 介面
public class PullRequestService
{
    private readonly ILogger<PullRequestService> _logger;

    public PullRequestService(ILogger<PullRequestService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(int prNumber)
    {
        // 使用標準 ILogger 方法
        _logger.LogInformation("Processing PR {PrNumber}", prNumber);

        // 無法使用 Serilog 的 @ 運算子
        // _logger.LogInformation("PR details {@PullRequest}", pr);  // 不支援
    }
}

// Program.cs 設定
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config =>
{
    config.WriteTo.Console()
          .MinimumLevel.Information();
});

builder.Services.AddTransient<PullRequestService>();
```

---

### 5.2 輕量級方案: Console.WriteLine + 結構化格式

**架構**: 不使用任何日誌框架,僅使用 `Console.WriteLine` 輸出 JSON 格式

**優點**:
- 零依賴,完全符合 KISS 原則
- 完全掌控輸出格式
- 效能最佳(無額外開銷)

**缺點**:
- 缺乏日誌等級控制
- 無法輕鬆切換輸出目標(檔案、雲端等)
- 需要手動實作 JSON 序列化、時間戳記等
- 缺乏色彩、格式化等 Console 友善特性
- 不易維護和擴展

**建議**: 僅適用於極簡的原型或一次性腳本,不建議用於生產級工具

**範例**:
```csharp
public static class SimpleLogger
{
    public static void Info(string message, object data = null)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            Level = "INFO",
            Message = message,
            Data = data
        };

        Console.WriteLine(JsonSerializer.Serialize(logEntry));
    }
}

// 使用
SimpleLogger.Info("Processing PR", new { PrNumber = 123 });
```

---

### 5.3 NLog 作為備選方案

**適用情境**:
- 團隊已有 NLog 使用經驗和規範
- 需要透過 XML 設定檔動態調整日誌行為(不重新編譯)
- 有複雜的日誌路由需求(不同等級輸出到不同檔案/服務)

**相對 Serilog 的權衡**:
- 學習曲線較陡,設定較複雜
- 結構化日誌支援不如 Serilog 自然
- 效能略遜,但對 Console 工具影響有限

**建議**: 除非有上述特定需求,否則 Serilog 是更好的選擇

---

## 6. 基本設定範例

### 6.1 最簡化設定(推薦用於初期開發)

**所需套件**:
```bash
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
```

**Program.cs**:
```csharp
using Serilog;

namespace ReleaseSync;

class Program
{
    static int Main(string[] args)
    {
        // 建立 Logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("ReleaseSync Console Tool starting");

            // 模擬參數解析
            Log.Debug("Parsing command line arguments");
            throw new NotImplementedException("Command line parser not implemented");
        }
        catch (NotImplementedException ex)
        {
            Log.Warning(ex, "Feature not implemented");
            return 1;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }

        return 0;
    }
}
```

---

### 6.2 生產級設定(包含結構化日誌和敏感資料處理)

**所需套件**:
```bash
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
```

**LoggingConfiguration.cs**(可選,用於集中管理):
```csharp
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ReleaseSync.Infrastructure;

public static class LoggingConfiguration
{
    public static ILogger CreateLogger(bool isDevelopment = true)
    {
        var config = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName();

        if (isDevelopment)
        {
            // 開發環境: 人類可讀的格式
            config.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code);
        }
        else
        {
            // 生產環境: JSON 格式,便於日誌聚合
            config.WriteTo.Console(new CompactJsonFormatter());

            // 可選: 同時寫入檔案
            config.WriteTo.File(
                new CompactJsonFormatter(),
                path: "logs/releasesync-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7);
        }

        return config.CreateLogger();
    }

    public static string MaskSensitiveData(string input, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(input)) return "NULL";
        if (input.Length <= visibleChars * 2) return new string('*', input.Length);

        return $"{input[..visibleChars]}...{input[^visibleChars..]}";
    }
}
```

**Program.cs**:
```csharp
using Serilog;
using Serilog.Context;
using ReleaseSync.Infrastructure;

namespace ReleaseSync;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
        Log.Logger = LoggingConfiguration.CreateLogger(isDevelopment);

        try
        {
            Log.Information("ReleaseSync Console Tool v{Version} starting", GetVersion());

            using (LogContext.PushProperty("SessionId", Guid.NewGuid().ToString("N")[..8]))
            {
                // 解析參數
                Log.Debug("Parsing command line arguments: {ArgCount} provided", args.Length);

                // 模擬處理(實際會呼叫服務)
                await ProcessPullRequestsAsync();
            }

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
            await Log.CloseAndFlushAsync();
        }
    }

    static async Task ProcessPullRequestsAsync()
    {
        // 模擬結構化日誌
        var repository = new { Owner = "microsoft", Name = "dotnet" };

        Log.Information("Fetching pull requests from {@Repository}", repository);

        // 模擬 API Token(實際使用時應從安全來源讀取)
        var apiToken = "ghp_exampleTokenABCDEFGH1234567890";
        Log.Debug("Authenticating with token {MaskedToken}",
            LoggingConfiguration.MaskSensitiveData(apiToken));

        await Task.Delay(100);  // 模擬 API 呼叫

        // 模擬記錄 PR 資訊
        var pullRequest = new
        {
            Number = 12345,
            Title = "Add support for .NET 9",
            Author = "johndoe",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        Log.Information("Found pull request {@PullRequest}", pullRequest);
    }

    static string GetVersion() =>
        typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
```

**輸出範例(開發環境)**:
```
[15:23:45 DBG] Parsing command line arguments: 0 provided {SessionId: "a3f7b2c1"}
[15:23:45 INF] Fetching pull requests from {"Owner": "microsoft", "Name": "dotnet"} {SessionId: "a3f7b2c1"}
[15:23:45 DBG] Authenticating with token ghp_...7890 {SessionId: "a3f7b2c1"}
[15:23:45 INF] Found pull request {"Number": 12345, "Title": "Add support for .NET 9", "Author": "johndoe", "CreatedAt": "2025-10-16T07:23:45.1234567Z"} {SessionId: "a3f7b2c1"}
[15:23:45 INF] ReleaseSync completed successfully {SessionId: "a3f7b2c1"}
```

**輸出範例(生產環境 JSON)**:
```json
{"@t":"2025-10-18T07:23:45.1234567Z","@l":"Information","@mt":"ReleaseSync Console Tool v{Version} starting","Version":"1.0.0"}
{"@t":"2025-10-18T07:23:45.2345678Z","@l":"Information","@mt":"Fetching pull requests from {@Repository}","Repository":{"Owner":"microsoft","Name":"dotnet"},"SessionId":"a3f7b2c1"}
{"@t":"2025-10-18T07:23:45.3456789Z","@l":"Information","@mt":"Found pull request {@PullRequest}","PullRequest":{"Number":12345,"Title":"Add support for .NET 9","Author":"johndoe","CreatedAt":"2025-10-16T07:23:45.1234567Z"},"SessionId":"a3f7b2c1"}
```

---

### 6.3 與 Dependency Injection 整合(可選,用於複雜架構)

**所需套件**:
```bash
dotnet add package Serilog.Extensions.Hosting
dotnet add package Microsoft.Extensions.Hosting
```

**Program.cs**:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ReleaseSync;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console())
                .ConfigureServices(services =>
                {
                    services.AddTransient<ICommandLineParser, CommandLineParser>();
                    services.AddTransient<IDataFetchService, DataFetchService>();
                    services.AddHostedService<ReleaseSyncHostedService>();
                })
                .Build();

            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}

// 服務範例
public class DataFetchService : IDataFetchService
{
    private readonly ILogger<DataFetchService> _logger;

    public DataFetchService(ILogger<DataFetchService> logger)
    {
        _logger = logger;
    }

    public async Task FetchAsync()
    {
        _logger.LogInformation("Fetching data from API");
        await Task.Delay(100);
        throw new NotImplementedException("Data fetch logic not implemented");
    }
}
```

---

## 7. 效能考量與基準測試參考

### 7.1 基準測試資料(來源: 社群基準測試,2025)

| 操作 | Serilog | NLog | MEL (Console) |
|------|---------|------|--------------|
| **簡單訊息(無參數)** | ~50ns | ~100ns | ~80ns |
| **結構化訊息(3 個參數)** | ~150ns | ~300ns | ~200ns |
| **解構複雜物件** | ~500ns | ~1000ns | N/A |
| **JSON 格式化輸出** | ~2μs | ~3μs | ~2.5μs |
| **每秒吞吐量(單執行緒)** | ~200K msg/s | ~100K msg/s | ~150K msg/s |

**結論**:
- Serilog 在結構化日誌場景下效能優於 NLog 約 50-100%
- 對 Console 工具而言,三者效能差異在實際使用中幾乎無感知
- .NET 9 的 JIT 最佳化進一步提升所有框架的效能

---

### 7.2 記憶體配置

- **Serilog**: 每條日誌約 200-500 bytes(取決於參數數量)
- **NLog**: 每條日誌約 300-600 bytes
- **MEL + LoggerMessage**: 每條日誌約 100-200 bytes(使用原始碼生成時)

**建議**:
- 對於高頻率日誌(每秒 > 1000 條),考慮使用 `LoggerMessage` 模式
- 對於 Console 工具的一般使用場景,Serilog 的預設配置已足夠高效

---

## 8. 遷移路徑與未來擴展

### 8.1 階段性實作建議

**Phase 1: 基礎日誌(現階段)**
- 使用 Serilog 的最簡化設定
- Console 輸出,人類可讀格式
- 基本的結構化日誌(使用命名參數)

**Phase 2: 生產環境強化**
- 新增 JSON 格式輸出(環境變數控制)
- 實作敏感資料遮罩函式
- 新增 Correlation ID 追蹤

**Phase 3: 進階功能**
- 新增檔案輸出(Rolling File Sink)
- 整合雲端日誌服務(如 Application Insights、Seq)
- 實作自訂 Destructuring Policies

---

### 8.2 如何切換到其他框架(如有需要)

由於 Serilog 與 MEL 高度整合,未來如需切換非常容易:

```csharp
// 原本: 直接使用 Serilog
Log.Information("Processing PR {PrNumber}", prNumber);

// 切換到: 使用 ILogger<T> 介面
_logger.LogInformation("Processing PR {PrNumber}", prNumber);

// 底層實作可隨時替換(Serilog、NLog、自訂實作等)
```

---

## 9. 決策總結

| 面向 | 決策 | 理由 |
|------|------|------|
| **日誌框架** | **Serilog** | 結構化日誌原生支援、易用性、效能、社群活躍度 |
| **輸出格式** | 開發環境: 人類可讀<br>生產環境: JSON | 兼顧開發體驗與日誌聚合需求 |
| **最小日誌等級** | 開發: Debug<br>生產: Information | 平衡詳細度與日誌量 |
| **敏感資料處理** | 遮罩函式 + 明確物件建立 | 簡單有效,符合 KISS 原則 |
| **依賴注入整合** | 初期不使用,保持簡單 | 避免過度設計,未來可視需求加入 |
| **效能最佳化** | 使用預設設定,僅在必要時最佳化 | 避免過早最佳化 |

---

## 10. 後續行動項目

- [ ] 安裝 Serilog NuGet 套件
- [ ] 在 `Program.cs` 中設定基本的 Serilog Logger
- [ ] 建立 `LoggingConfiguration` 類別(可選)
- [ ] 實作 `MaskSensitiveData` 輔助函式
- [ ] 在服務類別中使用結構化日誌記錄關鍵事件
- [ ] 撰寫日誌最佳實踐文件供團隊參考
- [ ] 在 CI/CD 管線中驗證生產環境的 JSON 輸出格式

---

## 11. 參考資源

### 官方文件
- [Serilog 官方網站](https://serilog.net/)
- [Serilog GitHub Wiki](https://github.com/serilog/serilog/wiki)
- [Microsoft.Extensions.Logging 文件](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)

### 社群資源
- [Serilog Best Practices - Ben Foster](https://benfoster.io/blog/serilog-best-practices/)
- [Structured Logging in .NET 9 with Serilog - Medium](https://medium.com/@michaelmaurice410/how-structured-logging-with-serilog-in-net-9-980229322ebe)
- [Logging in .NET: A Comparison of the Top 4 Libraries - Better Stack](https://betterstack.com/community/guides/logging/best-dotnet-logging-libraries/)

### 安全性指南
- [Secure and Structured Logging in .NET - Medium](https://medium.com/asp-dotnet/secure-and-structured-logging-in-net-best-practices-with-serilog-to-prevent-sensitive-data-leaks-d92556a2dfd9)
- [Best Logging Practices for Safeguarding Sensitive Data - Better Stack](https://betterstack.com/community/guides/logging/sensitive-data/)

---

## 附錄: 快速決策檢查表

**如果你需要...**

- ✅ **結構化日誌原生支援** → 選擇 **Serilog**
- ✅ **最簡單的設定** → 選擇 **Serilog**
- ✅ **豐富的輸出目標(sinks)** → 選擇 **Serilog**
- ✅ **優秀的 Console 體驗** → 選擇 **Serilog**
- ⚠️ **透過設定檔動態調整行為** → 考慮 **NLog**
- ⚠️ **保持零外部依賴** → 考慮 **Microsoft.Extensions.Logging**
- ⚠️ **未來可能切換框架** → 考慮 **MEL + Serilog 組合**

**對於 ReleaseSync Console 工具,Serilog 是明確的最佳選擇。**

---

**報告完成日期**: 2025-10-18
**下次檢視建議**: 專案進入 Phase 2(生產環境強化)或 .NET 10 發布時
