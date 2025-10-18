# Service Contracts - ReleaseSync Console 工具基礎架構

**Feature**: 001-console-tool-foundation
**Date**: 2025-10-18

## 概述

本目錄包含 ReleaseSync Console 應用程式基礎架構階段定義的服務介面契約。這些介面定義了各服務的職責邊界與方法簽章,遵循 **Interface Segregation Principle** 與 **Dependency Inversion Principle**。

---

## 服務介面清單

### 1. ICommandLineParserService

**檔案**: `ICommandLineParserService.cs`

**職責**: 解析命令列參數,驗證參數格式與有效性

**方法**:
- `void Parse(string platform, string token, string repository)`

**本階段行為**:
- 拋出 `NotImplementedException`

**未來實作重點**:
- 使用 System.CommandLine 解析參數
- 驗證平台類型 (github/gitlab/azuredevops)
- 驗證 token 格式
- 驗證 repository 格式 (owner/repo)

**測試要點**:
- 驗證拋出 NotImplementedException
- 驗證例外訊息清晰說明功能尚未實作

---

### 2. IDataFetchingService

**檔案**: `IDataFetchingService.cs`

**職責**: 從版控平台拉取 Pull Request / Merge Request 資料

**方法**:
- `Task FetchDataAsync(CancellationToken cancellationToken = default)`

**本階段行為**:
- 拋出 `NotImplementedException`

**未來實作重點**:
- 整合 GitHub API (使用 Octokit.NET 或 HttpClient)
- 整合 GitLab API
- 整合 Azure DevOps API
- 實作重試機制 (Polly)
- 處理 Rate Limiting
- 結構化日誌記錄

**測試要點**:
- 驗證拋出 NotImplementedException
- 驗證非同步方法簽章正確 (返回 Task)
- 驗證支援 CancellationToken

**設計決策**:
- 目前方法無參數,未來可能調整為傳入 PlatformConfiguration 與 RepositoryInformation
- 考慮使用 Builder Pattern 或 Options Pattern 傳遞複雜參數

---

### 3. IApplicationRunner

**檔案**: `IApplicationRunner.cs`

**職責**: 協調應用程式主要執行流程,整合各服務

**方法**:
- `Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)`

**本階段行為**:
- 拋出 `NotImplementedException`

**未來實作重點**:
- 呼叫 ICommandLineParserService 解析參數
- 呼叫 IDataFetchingService 拉取資料
- 協調錯誤處理與退出碼返回
- 記錄應用程式生命週期日誌

**測試要點**:
- 驗證拋出 NotImplementedException
- 驗證返回值為 int (退出碼)
- 驗證支援 CancellationToken

**退出碼規範** (未來實作):
- `0`: 成功
- `1`: 一般錯誤
- `2`: 參數錯誤
- `3`: 認證錯誤
- `4`: 網路錯誤

---

## 依賴注入註冊

所有服務介面將透過依賴注入容器註冊,建議在 `ServiceCollectionExtensions.cs` 中集中管理:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICommandLineParserService, CommandLineParserService>();
        services.AddScoped<IDataFetchingService, DataFetchingService>();
        services.AddScoped<IApplicationRunner, ApplicationRunner>();

        return services;
    }
}
```

**生命週期選擇**:
- `Scoped`: 適合有狀態的服務 (推薦用於 Console 應用程式,每次執行一個 scope)
- `Transient`: 適合無狀態、輕量級的服務
- `Singleton`: 適合全域共用、無狀態的服務 (如 Configuration)

**本階段建議**: 使用 `Scoped` 生命週期,保持靈活性

---

## 測試契約要求

根據專案憲章 **Principle III: Test Coverage & Types**,所有服務介面必須包含:

### 契約測試 (Contract Tests)

驗證介面契約不破壞,確保:
- 方法簽章正確
- 返回值類型正確
- 例外類型正確
- 非同步方法支援 CancellationToken

### 單元測試 (Unit Tests)

驗證服務實作行為,確保:
- 本階段正確拋出 NotImplementedException
- 例外訊息清晰描述功能尚未實作
- 未來實作時,驗證業務邏輯正確性

### 測試範例

```csharp
// ReleaseSync.Console.UnitTests/Services/CommandLineParserServiceTests.cs
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
```

---

## 設計原則遵循

### SOLID Principles

✅ **Single Responsibility Principle**:
- ICommandLineParserService: 僅負責參數解析
- IDataFetchingService: 僅負責資料拉取
- IApplicationRunner: 僅負責流程協調

✅ **Interface Segregation Principle**:
- 每個介面職責單一,不包含不必要的方法
- 客戶端僅依賴所需的介面

✅ **Dependency Inversion Principle**:
- Program.cs 依賴介面,不依賴具體實作
- 透過依賴注入容器解析依賴

### KISS Principle

✅ **保持簡單**:
- 本階段僅定義必要的三個服務介面
- 方法簽章簡單直接,避免過度設計
- 未來根據實際需求逐步擴展

### DDD Tactical Patterns

⚠️ **本階段暫不適用**:
- 無領域模型,因此不涉及 Entity、Value Object、Aggregate 等模式
- 服務介面屬於應用層 (Application Layer),不包含領域邏輯

---

## 未來擴展方向

### 可能新增的服務介面

1. **IConfigurationService**: 管理應用程式設定與敏感資料
2. **IPullRequestRepository**: 持久化 PR/MR 資料
3. **IExportService**: 匯出資料到不同格式 (JSON, CSV, Markdown)
4. **INotificationService**: 發送通知 (Email, Slack, Teams)

### 可能的重構

1. **IDataFetchingService 參數化**:
   ```csharp
   Task<PullRequest> FetchPullRequestAsync(
       PlatformType platform,
       RepositoryInformation repository,
       int pullRequestNumber,
       CancellationToken cancellationToken = default);
   ```

2. **使用 Result Pattern 替代例外**:
   ```csharp
   Task<Result<ParsedOptions>> ParseAsync(
       string[] args,
       CancellationToken cancellationToken = default);
   ```

3. **引入 CQRS**:
   ```csharp
   public record FetchPullRequestCommand(
       PlatformType Platform,
       string Repository,
       int Number) : IRequest<PullRequest>;
   ```

---

## 參考資源

- **System.CommandLine**: https://learn.microsoft.com/dotnet/standard/commandline/
- **Dependency Injection**: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection
- **SOLID Principles**: https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles
- **xUnit Testing**: https://xunit.net/

---

**Document Version**: 1.0
**Last Updated**: 2025-10-18
**Next Review**: 實作階段開始時
