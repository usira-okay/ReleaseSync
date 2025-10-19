# ReleaseSync 實作指南

## 專案狀態

### 已完成 (Phase 1 - Setup)

✅ **T001-T008**: 基礎架構設定
- Solution 與專案結構已建立
- .editorconfig 已設定 (C# 程式碼風格規範)
- .gitignore 已正確排除 appsettings.secure.json
- appsettings.json 與 appsettings.secure.example.json 已複製至 Console 專案
- 所有 NuGet 套件已安裝:
  - Infrastructure: NGitLab 9.3.0, Microsoft.TeamFoundationServer.Client 19.225.1
  - Console: System.CommandLine, Microsoft.Extensions.*
  - 測試專案: xUnit, FluentAssertions, NSubstitute

✅ **建置成功**: `dotnet build` 可正常編譯

✅ **初始 Domain 模型**:
- `DateRange.cs`: 時間範圍值物件
- `BranchName.cs`: 分支名稱值物件
- `WorkItemId.cs`: Work Item 識別碼值物件

---

## 剩餘實作任務 (T009-T075)

### Phase 2: Foundational (T009-T026) - Domain 層基礎

#### Domain Models (T009-T015)

**T012**: `src/ReleaseSync.Domain/Models/PlatformSyncStatus.cs`
```csharp
// 參考 specs/002-pr-aggregation-tool/data-model.md 第 228-293 行
public record PlatformSyncStatus { ... }
```

**T013**: `src/ReleaseSync.Domain/Models/PullRequestInfo.cs`
```csharp
// 參考 data-model.md 第 300-427 行
public class PullRequestInfo { ... }
```

**T014**: `src/ReleaseSync.Domain/Models/WorkItemInfo.cs`
```csharp
// 參考 data-model.md 第 440-535 行
public class WorkItemInfo { ... }
```

**T015**: `src/ReleaseSync.Domain/Models/SyncResult.cs` (Aggregate Root)
```csharp
// 參考 data-model.md 第 548-676 行
public class SyncResult { ... }
```

#### Domain Interfaces (T016-T018)

**T016**: `src/ReleaseSync.Domain/Services/IWorkItemIdParser.cs`
```csharp
// 參考 data-model.md 第 688-716 行
public interface IWorkItemIdParser { ... }
```

**T017**: `src/ReleaseSync.Domain/Repositories/IPullRequestRepository.cs`
```csharp
// 參考 data-model.md 第 721-749 行
public interface IPullRequestRepository { ... }
```

**T018**: `src/ReleaseSync.Domain/Repositories/IWorkItemRepository.cs`
```csharp
// 參考 data-model.md 第 754-789 行
public interface IWorkItemRepository { ... }
```

#### Infrastructure Configuration (T019-T022)

**T019**: `src/ReleaseSync.Infrastructure/Configuration/GitLabSettings.cs`
```csharp
// 參考 specs/002-pr-aggregation-tool/contracts/appsettings-schema.json
public class GitLabSettings
{
    public string ApiUrl { get; set; } = default!;
    public string PersonalAccessToken { get; set; } = default!;
    public List<ProjectConfig> Projects { get; set; } = new();

    public class ProjectConfig
    {
        public string ProjectPath { get; set; } = default!;
        public List<string> TargetBranches { get; set; } = new();
    }
}
```

**T020**: `src/ReleaseSync.Infrastructure/Configuration/BitBucketSettings.cs`
**T021**: `src/ReleaseSync.Infrastructure/Configuration/AzureDevOpsSettings.cs`
**T022**: `src/ReleaseSync.Infrastructure/Configuration/UserMappingSettings.cs`

#### Application Layer DTOs (T023-T026)

**T023**: `src/ReleaseSync.Application/DTOs/SyncRequest.cs`
**T024**: `src/ReleaseSync.Application/DTOs/SyncResult.cs`
**T025**: `src/ReleaseSync.Application/Services/ISyncOrchestrator.cs`
**T026**: `src/ReleaseSync.Application/Exporters/IResultExporter.cs`

---

### Phase 3: User Story 1 - GitLab/BitBucket 整合 (T027-T046)

#### 測試優先 (TDD)

**T027-T030**: Contract Tests & Unit Tests
- 參考 specs/002-pr-aggregation-tool/tasks.md 第 85-88 行
- 先寫測試,確認失敗後再實作

#### GitLab 平台實作 (T031-T034)

**T031**: `src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabApiClient.cs`
```csharp
using NGitLab;
using NGitLab.Models;

public class GitLabApiClient
{
    private readonly GitLabClient _client;

    public GitLabApiClient(string apiUrl, string accessToken)
    {
        _client = new GitLabClient(apiUrl, accessToken);
    }

    // 封裝 NGitLab API 呼叫
}
```

**T032**: `GitLabPullRequestRepository.cs` (實作 IPullRequestRepository)
**T033**: `GitLabService.cs` (協調 Repository)
**T034**: `GitLabServiceExtensions.cs` (DI 註冊)

#### BitBucket 平台實作 (T035-T038)

**T035**: `src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketApiClient.cs`
```csharp
using System.Net.Http;
using System.Text.Json;

public class BitBucketApiClient
{
    private readonly HttpClient _httpClient;

    public BitBucketApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BitBucket");
    }

    // 使用 HttpClient 直接呼叫 BitBucket REST API
    // 參考 research.md 第 103-136 行
}
```

**T036**: `BitBucketPullRequestRepository.cs`
**T037**: `BitBucketService.cs`
**T038**: `BitBucketServiceExtensions.cs`

#### Application Orchestration (T039-T043)

**T039**: `src/ReleaseSync.Application/Services/SyncOrchestrator.cs`
- 協調多平台資料抓取
- 處理部分失敗容錯

**T041**: `src/ReleaseSync.Console/Commands/SyncCommand.cs`
```csharp
using System.CommandLine;

public class SyncCommand
{
    public static Command Create()
    {
        var command = new Command("sync", "同步 PR/MR 資訊");

        // 參考 specs/002-pr-aggregation-tool/contracts/command-line-parameters.md
        var daysOption = new Option<int>(
            aliases: new[] { "--days", "-d" },
            description: "抓取最近 N 天的 PR/MR"
        );

        command.AddOption(daysOption);
        // ... 其他參數

        return command;
    }
}
```

**T042**: `SyncCommandHandler.cs`
**T043**: `Program.cs` (設定 DI 與命令列解析)

---

### Phase 4: User Story 2 - JSON 匯出 (T047-T053)

**T049**: `src/ReleaseSync.Application/Exporters/JsonFileExporter.cs`
```csharp
using System.Text.Json;

public class JsonFileExporter : IResultExporter
{
    public async Task ExportAsync(SyncResult result, string filePath, bool force = false)
    {
        if (File.Exists(filePath) && !force)
        {
            throw new InvalidOperationException($"檔案已存在: {filePath}");
        }

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(filePath, json);
    }
}
```

---

### Phase 5: User Story 3 - Azure DevOps 整合 (T054-T066)

**T057**: `src/ReleaseSync.Infrastructure/Parsers/RegexWorkItemIdParser.cs`
```csharp
using System.Text.RegularExpressions;

public class RegexWorkItemIdParser : IWorkItemIdParser
{
    private readonly IEnumerable<WorkItemIdPattern> _patterns;

    public WorkItemId? ParseWorkItemId(BranchName branchName)
    {
        foreach (var pattern in _patterns)
        {
            var regex = new Regex(
                pattern.Regex,
                pattern.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None
            );

            var match = regex.Match(branchName.Value);
            if (match.Success && int.TryParse(match.Groups[pattern.CaptureGroup].Value, out int id))
            {
                return new WorkItemId(id);
            }
        }

        return null;
    }
}
```

**T058-T061**: Azure DevOps 平台實作
- 使用 Microsoft.TeamFoundationServer.Client
- 參考 research.md 第 138-209 行

---

### Phase 6: Polish & Cross-Cutting Concerns (T067-T075)

**T067**: 加入 ILogger 結構化日誌
```csharp
_logger.LogInformation(
    "開始同步 {Platform} 專案 {ProjectName},時間範圍: {StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}",
    "GitLab", projectName, dateRange.StartDate, dateRange.EndDate
);
```

**T068**: 錯誤處理與使用者友善訊息
**T069**: 加入 --verbose 參數支援
**T070**: XML 文件註解檢查 (繁體中文)
**T075**: 建立 README.md

---

## 實作策略建議

### 1. TDD 流程 (嚴格遵守)

```bash
# 步驟 1: 先寫測試
code tests/ReleaseSync.Domain.UnitTests/Models/PullRequestInfoTests.cs

# 步驟 2: 執行測試,確認失敗
dotnet test --filter "FullyQualifiedName~PullRequestInfoTests"

# 步驟 3: 實作功能
code src/ReleaseSync.Domain/Models/PullRequestInfo.cs

# 步驟 4: 執行測試,確認通過
dotnet test --filter "FullyQualifiedName~PullRequestInfoTests"
```

### 2. 使用設計文件

所有類別結構已在以下文件定義:
- `specs/002-pr-aggregation-tool/data-model.md`: Domain 模型完整定義
- `specs/002-pr-aggregation-tool/contracts/`: API 契約與 Schema
- `specs/002-pr-aggregation-tool/tasks.md`: 任務列表與執行順序

### 3. 平行實作機會

以下任務可平行執行 (不同檔案,無依賴):
- T009-T012 (Value Objects)
- T019-T022 (Configuration Models)
- T031 (GitLab) || T035 (BitBucket)

### 4. 驗證檢查點

每個 Phase 完成後執行:
```bash
# 編譯檢查
dotnet build

# 測試檢查
dotnet test

# 程式碼風格檢查 (.editorconfig 已設定)
dotnet format --verify-no-changes
```

---

## 快速開始 (開發者)

### 1. 設定組態檔

```bash
cp src/ReleaseSync.Console/appsettings.secure.example.json src/ReleaseSync.Console/appsettings.secure.json
```

編輯 `appsettings.secure.json`,填入實際的 API Token。

### 2. 執行工具

```bash
cd src/ReleaseSync.Console
dotnet run -- sync --days 7
```

### 3. 執行測試

```bash
dotnet test
```

---

## 注意事項

### 憲章合規性

- ✅ **繁體中文**: 所有 XML 註解與業務邏輯註解使用繁體中文
- ✅ **SOLID 原則**: 每個類別單一職責,依賴抽象介面
- ✅ **KISS 原則**: 避免過度工程化,不使用 MediatR
- ✅ **TDD**: 所有公開 API 必須先撰寫測試

### 安全性

- ❌ 不要提交 appsettings.secure.json 至版控
- ❌ 不要在日誌中記錄 Access Token
- ✅ 使用 IHttpClientFactory 避免 socket exhaustion

### 效能目標

- 單一平台抓取 100 筆 PR/MR 應在 30 秒內完成 (不含網路 I/O)
- 所有 I/O 操作使用 async/await

---

## 下一步

1. **Phase 2** (T009-T026): 完成 Domain 層所有模型與介面
2. **Phase 3** (T027-T046): 實作 GitLab/BitBucket 整合
3. **Phase 4** (T047-T053): 實作 JSON 匯出功能
4. **Phase 5** (T054-T066): 實作 Azure DevOps 整合
5. **Phase 6** (T067-T075): 程式碼品質改善與文件

每個 Phase 完成後,更新 `specs/002-pr-aggregation-tool/tasks.md` 標記任務為 [X]。

---

**專案建置狀態**: ✅ 成功
**測試狀態**: ⚠️ 尚未實作測試
**下一個里程碑**: 完成 Phase 2 (Domain 層基礎)
