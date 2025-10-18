# Quick Start Guide - ReleaseSync Console 工具基礎架構

**Feature**: 001-console-tool-foundation
**Target Audience**: 開發團隊成員
**Estimated Time**: 15-20 分鐘
**Last Updated**: 2025-10-18

---

## 概述

本指南協助您快速設置 ReleaseSync Console 應用程式的基礎架構專案。完成本指南後,您將能夠:

✅ 建立專案結構 (src/, tests/)
✅ 安裝必要的 NuGet 套件
✅ 設定編譯選項與程式碼品質工具
✅ 撰寫第一批測試並驗證基礎架構
✅ 執行應用程式並確認正常啟動

---

## 先決條件

確保您的開發環境已安裝以下工具:

- ✅ **.NET 9 SDK** (或更新版本)
  ```bash
  dotnet --version
  # 應顯示 9.0.x 或更高版本
  ```

- ✅ **Git** (用於版本控制)
  ```bash
  git --version
  ```

- ✅ **IDE/編輯器** (建議使用以下任一):
  - Visual Studio 2022 (17.8 或更新)
  - Visual Studio Code + C# Dev Kit
  - JetBrains Rider 2024.1 或更新

- ✅ **終端機/命令列工具**
  - Windows: PowerShell 7+ 或 Command Prompt
  - Linux/macOS: Bash 或 Zsh

---

## 步驟 1: 克隆 Repository (如果尚未完成)

```bash
# 如果您尚未克隆 repository
git clone <repository-url>
cd ReleaseSync

# 切換到功能分支
git checkout 001-console-tool-foundation
```

---

## 步驟 2: 建立專案結構

### 2.1 建立 Solution 與主專案

```bash
# 建立 Solution 檔案 (如果尚未存在)
dotnet new sln -n ReleaseSync

# 建立 src 與 tests 資料夾
mkdir -p src tests

# 建立主要的 Console 專案
dotnet new console -n ReleaseSync.Console -o src/ReleaseSync.Console -f net9.0

# 將專案加入 Solution
dotnet sln add src/ReleaseSync.Console/ReleaseSync.Console.csproj
```

### 2.2 建立測試專案

```bash
# 建立單元測試專案
dotnet new xunit -n ReleaseSync.Console.UnitTests -o tests/ReleaseSync.Console.UnitTests -f net9.0

# 將測試專案加入 Solution
dotnet sln add tests/ReleaseSync.Console.UnitTests/ReleaseSync.Console.UnitTests.csproj

# 建立測試專案對主專案的參考
dotnet add tests/ReleaseSync.Console.UnitTests/ReleaseSync.Console.UnitTests.csproj reference src/ReleaseSync.Console/ReleaseSync.Console.csproj
```

### 2.3 建立子目錄

```bash
# Console 專案子目錄
mkdir -p src/ReleaseSync.Console/Services
mkdir -p src/ReleaseSync.Console/Extensions

# 測試專案子目錄
mkdir -p tests/ReleaseSync.Console.UnitTests/Services
mkdir -p tests/ReleaseSync.Console.UnitTests/Fixtures
mkdir -p tests/ReleaseSync.Console.UnitTests/TestHelpers
```

---

## 步驟 3: 安裝必要的 NuGet 套件

### 3.1 主專案套件

```bash
cd src/ReleaseSync.Console

# 命令列參數解析
dotnet add package System.CommandLine --version 2.0.0-rc.2.25502.107

# 依賴注入
dotnet add package Microsoft.Extensions.DependencyInjection --version 9.0.0
dotnet add package Microsoft.Extensions.Hosting --version 9.0.0

# 設定管理
dotnet add package Microsoft.Extensions.Configuration --version 9.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 9.0.0

# 日誌框架
dotnet add package Serilog --version 4.2.0
dotnet add package Serilog.Sinks.Console --version 6.0.0
dotnet add package Serilog.Sinks.File --version 6.0.0

cd ../..
```

### 3.2 測試專案套件

```bash
cd tests/ReleaseSync.Console.UnitTests

# xUnit 測試框架 (應該已預裝)
# dotnet add package xunit --version 2.6.0
# dotnet add package xunit.runner.visualstudio --version 2.5.0
# dotnet add package Microsoft.NET.Test.SDK --version 17.9.0

# FluentAssertions 斷言函式庫
dotnet add package FluentAssertions --version 6.12.0

# Moq 模擬函式庫
dotnet add package Moq --version 4.20.0

cd ../..
```

---

## 步驟 4: 設定專案檔案 (.csproj)

### 4.1 編輯 `src/ReleaseSync.Console/ReleaseSync.Console.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- 產生 XML 文件檔案 -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- 將警告視為錯誤 -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- 應用程式版本 -->
    <Version>0.1.0</Version>
    <AssemblyVersion>0.1.0.0</AssemblyVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- NuGet 套件已透過 dotnet add package 安裝 -->
  </ItemGroup>

  <ItemGroup>
    <!-- 設定檔案複製到輸出目錄 -->
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="secure.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

### 4.2 編輯 `tests/ReleaseSync.Console.UnitTests/ReleaseSync.Console.UnitTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>

    <!-- 產生 XML 文件檔案 -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- 將警告視為錯誤 -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <!-- NuGet 套件已透過 dotnet add package 安裝 -->
  </ItemGroup>

  <ItemGroup>
    <!-- 專案參考已透過 dotnet add reference 建立 -->
  </ItemGroup>

</Project>
```

---

## 步驟 5: 建立設定檔案

### 5.1 建立 `src/ReleaseSync.Console/appsettings.json`

```json
{
  "Logging": {
    "MinimumLevel": "Information",
    "EnableFileLogging": false,
    "LogFilePath": "logs/"
  },
  "Repository": {
  }
}
```

### 5.2 建立 `src/ReleaseSync.Console/secure.json.example`

```json
{
  "GitHubToken": "YOUR_GITHUB_TOKEN_HERE",
  "GitLabToken": "YOUR_GITLAB_TOKEN_HERE",
  "AzureDevOpsToken": "YOUR_AZURE_DEVOPS_TOKEN_HERE"
}
```

### 5.3 建立空的 `src/ReleaseSync.Console/secure.json` 並加入 .gitignore

```bash
# 建立空的 secure.json
echo '{}' > src/ReleaseSync.Console/secure.json

# 確保 .gitignore 包含 secure.json
echo 'src/ReleaseSync.Console/secure.json' >> .gitignore
```

---

## 步驟 6: 建立 .editorconfig

在 Repository 根目錄建立 `.editorconfig`:

```ini
# ReleaseSync .editorconfig

root = true

# 所有檔案
[*]
charset = utf-8
indent_style = space
indent_size = 4
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

# C# 檔案
[*.cs]
indent_size = 4

# 命名規則
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# Async 方法應以 Async 結尾
dotnet_naming_rule.async_methods_should_end_with_async.severity = warning
dotnet_naming_rule.async_methods_should_end_with_async.symbols = async_methods
dotnet_naming_rule.async_methods_should_end_with_async.style = end_with_async

dotnet_naming_symbols.async_methods.applicable_kinds = method
dotnet_naming_symbols.async_methods.required_modifiers = async

dotnet_naming_style.end_with_async.required_suffix = Async
dotnet_naming_style.end_with_async.capitalization = pascal_case

# JSON 檔案
[*.json]
indent_size = 2
```

---

## 步驟 7: 建立服務介面與實作

### 7.1 建立服務介面

從 contracts/ 目錄複製介面定義到專案中:

```bash
cp specs/001-console-tool-foundation/contracts/ICommandLineParserService.cs src/ReleaseSync.Console/Services/
cp specs/001-console-tool-foundation/contracts/IDataFetchingService.cs src/ReleaseSync.Console/Services/
cp specs/001-console-tool-foundation/contracts/IApplicationRunner.cs src/ReleaseSync.Console/Services/
```

### 7.2 建立服務實作

**src/ReleaseSync.Console/Services/CommandLineParserService.cs**:

```csharp
namespace ReleaseSync.Console.Services;

/// <summary>
/// 命令列參數解析服務實作
/// </summary>
public class CommandLineParserService : ICommandLineParserService
{
    /// <inheritdoc />
    public void Parse(string platform, string token, string repository)
    {
        throw new NotImplementedException(
            "命令列參數解析服務尚未實作。將於後續階段實作平台驗證、Token 格式檢查等功能。");
    }
}
```

**src/ReleaseSync.Console/Services/DataFetchingService.cs**:

```csharp
namespace ReleaseSync.Console.Services;

/// <summary>
/// 資料拉取服務實作
/// </summary>
public class DataFetchingService : IDataFetchingService
{
    /// <inheritdoc />
    public Task FetchDataAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "資料拉取服務尚未實作。將於後續階段實作與 GitHub/GitLab API 的整合。");
    }
}
```

**src/ReleaseSync.Console/Services/ApplicationRunner.cs**:

```csharp
namespace ReleaseSync.Console.Services;

/// <summary>
/// 應用程式主要邏輯執行服務實作
/// </summary>
public class ApplicationRunner : IApplicationRunner
{
    /// <inheritdoc />
    public Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "應用程式執行服務尚未實作。將於後續階段實作完整的流程協調邏輯。");
    }
}
```

### 7.3 建立依賴注入擴充方法

**src/ReleaseSync.Console/Extensions/ServiceCollectionExtensions.cs**:

```csharp
using Microsoft.Extensions.DependencyInjection;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.Extensions;

/// <summary>
/// IServiceCollection 擴充方法,用於註冊應用程式服務
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 註冊 ReleaseSync 應用程式服務
    /// </summary>
    /// <param name="services">服務集合</param>
    /// <returns>服務集合 (支援鏈式呼叫)</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICommandLineParserService, CommandLineParserService>();
        services.AddScoped<IDataFetchingService, DataFetchingService>();
        services.AddScoped<IApplicationRunner, ApplicationRunner>();

        return services;
    }
}
```

---

## 步驟 8: 建立 Program.cs

**src/ReleaseSync.Console/Program.cs**:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReleaseSync.Console.Extensions;
using Serilog;

namespace ReleaseSync.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 設定 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("ReleaseSync Console Tool 啟動中...");

            // 建立 Host
            var host = CreateHostBuilder(args).Build();

            // 解析 ApplicationRunner 服務並執行
            var runner = host.Services.GetRequiredService<Services.IApplicationRunner>();
            var exitCode = await runner.RunAsync(args);

            Log.Information("ReleaseSync Console Tool 執行完畢,退出碼: {ExitCode}", exitCode);
            return exitCode;
        }
        catch (NotImplementedException ex)
        {
            Log.Warning("功能尚未實作: {Message}", ex.Message);
            return 0; // 本階段視為正常,返回 0
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "應用程式發生未預期的錯誤");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // 載入設定檔案
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("secure.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // 註冊應用程式服務
                services.AddApplicationServices();
            })
            .UseSerilog(); // 使用 Serilog 作為日誌提供者
}
```

---

## 步驟 9: 建立測試

**tests/ReleaseSync.Console.UnitTests/Services/CommandLineParserServiceTests.cs**:

```csharp
using FluentAssertions;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.UnitTests.Services;

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

**tests/ReleaseSync.Console.UnitTests/Services/DataFetchingServiceTests.cs**:

```csharp
using FluentAssertions;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.UnitTests.Services;

public class DataFetchingServiceTests
{
    [Fact(DisplayName = "當服務被呼叫時,應拋出 NotImplementedException")]
    public async Task FetchDataAsync_ThrowsNotImplementedException_WhenCalled()
    {
        // Arrange
        var service = new DataFetchingService();

        // Act
        Func<Task> act = async () => await service.FetchDataAsync();

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*尚未實作*");
    }
}
```

**tests/ReleaseSync.Console.UnitTests/Services/ApplicationRunnerTests.cs**:

```csharp
using FluentAssertions;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.UnitTests.Services;

public class ApplicationRunnerTests
{
    [Fact(DisplayName = "當服務被呼叫時,應拋出 NotImplementedException")]
    public async Task RunAsync_ThrowsNotImplementedException_WhenCalled()
    {
        // Arrange
        var runner = new ApplicationRunner();
        var args = Array.Empty<string>();

        // Act
        Func<Task<int>> act = async () => await runner.RunAsync(args);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*尚未實作*");
    }
}
```

---

## 步驟 10: 編譯與測試

### 10.1 編譯專案

```bash
# 還原套件
dotnet restore

# 編譯 Solution
dotnet build

# 確認無錯誤與警告
# 預期輸出: Build succeeded. 0 Warning(s), 0 Error(s)
```

### 10.2 執行測試

```bash
# 執行所有測試
dotnet test

# 預期輸出:
# Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3
```

### 10.3 執行應用程式

```bash
# 執行 Console 應用程式
dotnet run --project src/ReleaseSync.Console/ReleaseSync.Console.csproj

# 預期輸出:
# [timestamp INF] ReleaseSync Console Tool 啟動中...
# [timestamp WRN] 功能尚未實作: 應用程式執行服務尚未實作。將於後續階段實作完整的流程協調邏輯。
# [timestamp INF] ReleaseSync Console Tool 執行完畢,退出碼: 0
```

---

## 步驟 11: 驗證專案結構

執行以下命令確認專案結構正確:

```bash
# 列出 src 目錄結構
tree src/ReleaseSync.Console

# 預期輸出:
# src/ReleaseSync.Console/
# ├── Extensions/
# │   └── ServiceCollectionExtensions.cs
# ├── Services/
# │   ├── ApplicationRunner.cs
# │   ├── CommandLineParserService.cs
# │   ├── DataFetchingService.cs
# │   ├── IApplicationRunner.cs
# │   ├── ICommandLineParserService.cs
# │   └── IDataFetchingService.cs
# ├── Program.cs
# ├── appsettings.json
# ├── secure.json
# └── ReleaseSync.Console.csproj

# 列出 tests 目錄結構
tree tests/ReleaseSync.Console.UnitTests

# 預期輸出:
# tests/ReleaseSync.Console.UnitTests/
# ├── Services/
# │   ├── ApplicationRunnerTests.cs
# │   ├── CommandLineParserServiceTests.cs
# │   └── DataFetchingServiceTests.cs
# ├── Fixtures/
# ├── TestHelpers/
# └── ReleaseSync.Console.UnitTests.csproj
```

---

## 常見問題 (FAQ)

### Q1: 編譯時出現 "警告視為錯誤" 導致失敗

**問題**: `TreatWarningsAsErrors` 設定導致任何警告都會中斷編譯

**解決方案**:
1. 檢查警告訊息,修正程式碼問題
2. 確保所有公開方法都有 XML 文件註解 (`/// <summary>`)
3. 如果是第三方套件警告,可暫時關閉特定警告:
   ```xml
   <NoWarn>CS1591</NoWarn> <!-- 缺少 XML 註解警告 -->
   ```

### Q2: 無法解析 Serilog 或其他套件

**問題**: `dotnet build` 找不到套件參考

**解決方案**:
```bash
# 清理並還原套件
dotnet clean
dotnet restore
dotnet build
```

### Q3: 測試執行失敗

**問題**: `dotnet test` 回報測試失敗或找不到測試

**解決方案**:
1. 確認測試專案已參考主專案:
   ```bash
   dotnet list tests/ReleaseSync.Console.UnitTests/ReleaseSync.Console.UnitTests.csproj reference
   ```
2. 確認已安裝 xUnit 測試套件
3. 重新編譯並執行:
   ```bash
   dotnet clean
   dotnet build
   dotnet test --no-build
   ```

### Q4: secure.json 被提交到 Git

**問題**: 敏感資料意外提交到版本控制

**解決方案**:
```bash
# 從 Git 移除 secure.json (保留本機檔案)
git rm --cached src/ReleaseSync.Console/secure.json

# 確保 .gitignore 包含此檔案
echo 'src/ReleaseSync.Console/secure.json' >> .gitignore

# 提交變更
git add .gitignore
git commit -m "chore: 將 secure.json 加入 .gitignore"
```

---

## 下一步

恭喜!您已成功完成 ReleaseSync Console 工具基礎架構的設置。

### 建議後續行動:

1. **提交變更**:
   ```bash
   git add .
   git commit -m "feat: 建立 Console 工具基礎架構

   - 建立專案結構 (src/, tests/)
   - 安裝必要套件 (System.CommandLine, Serilog, xUnit)
   - 定義服務介面與實作 (NotImplementedException)
   - 建立依賴注入註冊邏輯
   - 撰寫單元測試驗證基礎架構

   🤖 Generated with [Claude Code](https://claude.com/claude-code)

   Co-Authored-By: Claude <noreply@anthropic.com>"
   ```

2. **推送到遠端**:
   ```bash
   git push origin 001-console-tool-foundation
   ```

3. **建立 Pull Request** (如果適用)

4. **開始下一階段開發**:
   - 實作命令列參數解析邏輯
   - 整合版控平台 API 客戶端
   - 實作資料拉取與同步邏輯

---

## 其他資源

- **專案規格**: `specs/001-console-tool-foundation/spec.md`
- **實作計畫**: `specs/001-console-tool-foundation/plan.md`
- **研究報告**: `specs/001-console-tool-foundation/research.md`
- **資料模型**: `specs/001-console-tool-foundation/data-model.md`
- **服務契約**: `specs/001-console-tool-foundation/contracts/README.md`

---

**Guide Version**: 1.0
**Last Updated**: 2025-10-18
**Feedback**: 如有問題或建議,請在團隊溝通頻道提出
