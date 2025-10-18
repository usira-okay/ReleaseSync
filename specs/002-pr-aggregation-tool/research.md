# Research Report: PR/MR 變更資訊聚合工具

**Feature**: 002-pr-aggregation-tool
**Date**: 2025-10-18
**Status**: Phase 0 Complete

本文件記錄所有技術決策的研究結果,解決 Implementation Plan 中標註為 NEEDS CLARIFICATION 的問題。

---

## 研究項目 1: GitLab API v4 用戶端函式庫選擇

### 問題陳述
應使用官方 SDK 或第三方函式庫與 GitLab API v4 互動?

### 選項評估

#### 選項 1: NGitLab (Ubisoft) ⭐ 推薦
- **NuGet 套件**: `NGitLab`
- **最新版本**: 9.3.0 (2025-08)
- **維護狀態**: ✅ 活躍維護中
- **成熟度**:
  - GitHub Stars: 154
  - NuGet 下載量: 1.0M+
  - 平均每日下載: 243 次
- **API 涵蓋範圍**: ✅ 支援 Merge Request 查詢
- **.NET 8 相容性**: ✅ 完全支援 (.NET Standard 2.0 + .NET 8.0)
- **授權**: MIT License (商業友善)
- **優點**:
  - Ubisoft 維護,企業級支援背景
  - 無外部相依性 (zero dependencies)
  - GitLab 官方文件列為認可的第三方客戶端
- **缺點**:
  - 時間篩選功能支援度需實作時驗證

#### 選項 2: GitLabApiClient (nmklotas)
- **NuGet 套件**: `GitLabApiClient`
- **最新版本**: 1.8.0 (2021-08)
- **維護狀態**: ❌ 已停止維護 (3+ 年未更新)
- **成熟度**:
  - GitHub Stars: 274
  - NuGet 下載量: 3.1M+
  - 平均每日下載: 1,100 次
- **.NET 8 相容性**: ⚠️ 僅明確支援至 .NET 5.0
- **缺點**: 維護風險高,不建議新專案使用

#### 選項 3: 直接使用 HttpClient
- **適用情況**: NGitLab 無法滿足時間篩選需求時
- **優點**: 完全控制、無第三方依賴、立即支援新 API
- **缺點**: 需自行實作分頁、錯誤處理、型別轉換

### 決策

**採用 NGitLab**

**理由**:
1. 唯一仍在積極維護的選項 (2025-08 仍有更新)
2. 明確支援 .NET 8.0
3. 無外部相依,降低供應鏈風險
4. Ubisoft 背景提供長期維護保障

**後續驗證事項**:
- PoC 階段需驗證時間篩選功能 (`created_after`, `updated_after`)
- 若不支援,評估應用層過濾或改用 HttpClient

**安裝指令**:
```bash
dotnet add package NGitLab
```

---

## 研究項目 2: BitBucket API 用戶端函式庫選擇

### 問題陳述
BitBucket Cloud 與 Server API 不同,需確認目標平台與對應函式庫選擇。

### BitBucket Cloud vs Server 差異分析

| 項目 | BitBucket Cloud | BitBucket Server/Data Center |
|------|-----------------|------------------------------|
| **API 版本** | API 2.0 | API 1.0 |
| **基礎 URL** | `https://api.bitbucket.org/2.0/` | `https://<instance>/rest/api/1.0/` |
| **日期篩選** | ✅ 原生支援 (`q=created_on>=...`) | ❌ 不支援,需客戶端過濾 |
| **維護狀態** | 持續更新 | Server 已於 2024/2/15 終止支援 |

### 選項評估

#### 選項 1: SharpBucket
- **支援版本**: 僅 BitBucket Cloud
- **維護狀態**: ⚠️ 最後更新 2022-01 (3 年未更新)
- **成熟度**: 116.7K 下載量,每日 29 次下載
- **.NET 8 相容性**: ✅ 支援 (.NET Standard 2.0)

#### 選項 2: Bitbucket.Cloud.Net
- **支援版本**: 僅 BitBucket Cloud
- **維護狀態**: ❌ 最後更新 2020-12 (4 年未更新)
- **成熟度**: 227.9K 下載量,每日 132 次下載
- **缺點**: 文件不足

#### 選項 3: 直接使用 HttpClient ⭐ 推薦
- **適用情況**: 所有情境
- **優點**:
  - 無維護風險 (所有函式庫已停止維護)
  - 100% API 涵蓋,即時支援新功能
  - BitBucket Cloud 原生支援日期篩選
- **缺點**: 需自行實作 HTTP 層邏輯

### 決策

**採用 HttpClient 直接呼叫 BitBucket REST API**

**理由**:
1. **所有 .NET BitBucket 函式庫已停止維護 2-4 年**,存在技術債務風險
2. BitBucket Cloud API 文件完整,易於實作
3. 原生支援日期篩選,實作簡單
4. 長期維護性更佳,不受第三方函式庫影響

**實作要點**:
```csharp
// 使用 IHttpClientFactory 避免 socket exhaustion
var client = httpClientFactory.CreateClient("BitBucket");
client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", accessToken);

// 日期篩選範例 (BitBucket Cloud)
var url = "https://api.bitbucket.org/2.0/repositories/{workspace}/{repo}/pullrequests" +
          "?q=created_on>=2025-01-01T00:00:00.000Z";
```

**安全性建議**:
- 使用 `IHttpClientFactory` 管理 HttpClient 生命週期
- 實作 Polly 重試機制處理暫時性錯誤
- 處理 BitBucket API 速率限制 (rate limit)
- 使用 `System.Text.Json` 進行 JSON 序列化

---

## 研究項目 3: Azure DevOps REST API 用戶端函式庫選擇

### 問題陳述
應使用哪個官方 Azure DevOps 用戶端函式庫?

### 官方套件比較

#### Microsoft.TeamFoundationServer.Client ⭐ 推薦
- **最新版本**: 19.225.1 (2024-03-12)
- **維護狀態**: ✅ 持續維護中 (Microsoft 官方)
- **.NET 8 相容性**: ✅ 完全支援 (.NET Standard 2.0)
- **API 涵蓋範圍**:
  - ✅ Work Item Tracking (包含 `WorkItemTrackingHttpClient`)
  - ✅ Work Item Relations (Parent-Child 關係)
  - ✅ WIQL 查詢
  - ✅ Build, Git, Version Control 等服務
- **下載量**: 2,990 萬次總下載,平均每日 8,000 次
- **授權**: Microsoft 專屬授權 (可再散布,商業友善)

#### Microsoft.VisualStudio.Services.Client
- **用途**: 核心平台服務 (驗證、身分識別)
- **關係**: 作為 `Microsoft.TeamFoundationServer.Client` 的相依套件
- **建議**: 不需單獨安裝,安裝主套件會自動包含

#### Microsoft.TeamFoundation.WorkItemTracking.WebApi
- **狀態**: 命名空間包含於 `Microsoft.TeamFoundationServer.Client`
- **說明**: 非獨立套件,而是主套件的一部分

### 決策

**採用 Microsoft.TeamFoundationServer.Client 19.225.1**

**理由**:
1. Microsoft 官方維護,與 Azure DevOps 同步更新
2. 單一套件包含所有所需功能
3. .NET 8 完全相容
4. 官方範例與文件豐富

**安裝指令**:
```bash
dotnet add package Microsoft.TeamFoundationServer.Client --version 19.225.1
```

**核心程式碼範例**:
```csharp
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

// 使用 PAT 建立連線
var orgUrl = new Uri("https://dev.azure.com/yourorg");
var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
var connection = new VssConnection(orgUrl, credentials);
var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

// 查詢 Work Item
var workItem = await witClient.GetWorkItemAsync(
    id: workItemId,
    expand: WorkItemExpand.Relations
);

// 取得 Parent Work Item
const string ParentLinkType = "System.LinkTypes.Hierarchy-Reverse";
var parentRelation = workItem.Relations?.FirstOrDefault(r => r.Rel == ParentLinkType);
if (parentRelation != null)
{
    var parentId = int.Parse(parentRelation.Url.Split('/').Last());
    var parent = await witClient.GetWorkItemAsync(parentId);
}
```

---

## 研究項目 4: Branch 名稱解析 Regex 格式

### 問題陳述
使用者提供範例 `vsts(\d+)` (不區分大小寫),需確認是否需要可設定化。

### 待使用者確認事項

由於使用者選擇了"Other"自訂回答,以下問題仍需進一步確認:

1. **BitBucket 版本**: Cloud / Data Center / Server?
2. **Regex 設定方式**: 固定寫死或可設定化?
3. **多 Regex 支援**: 是否需要支援多個不同的命名規則?
4. **錯誤處理策略**: 無法解析時記錄警告或停止流程?

### 暫定設計方案

**採用可設定化方案 (保留彈性)**

**appsettings.json 設計**:
```json
{
  "AzureDevOps": {
    "WorkItemIdPatterns": [
      {
        "Name": "VSTS Pattern",
        "Regex": "vsts(\\d+)",
        "IgnoreCase": true,
        "CaptureGroup": 1
      },
      {
        "Name": "Feature Pattern",
        "Regex": "feature/(\\d+)-",
        "IgnoreCase": false,
        "CaptureGroup": 1
      }
    ],
    "ParsingBehavior": {
      "OnParseFailure": "LogWarningAndContinue", // 或 "ThrowException"
      "StopOnFirstMatch": true
    }
  }
}
```

**實作類別設計**:
```csharp
public interface IWorkItemIdParser
{
    /// <summary>
    /// 從 Branch 名稱解析 Work Item ID
    /// </summary>
    /// <param name="branchName">Branch 名稱</param>
    /// <returns>Work Item ID,若無法解析則回傳 null</returns>
    int? ParseWorkItemId(string branchName);
}

public class RegexWorkItemIdParser : IWorkItemIdParser
{
    private readonly IEnumerable<WorkItemIdPattern> _patterns;
    private readonly ILogger<RegexWorkItemIdParser> _logger;

    public int? ParseWorkItemId(string branchName)
    {
        foreach (var pattern in _patterns)
        {
            var regex = new Regex(
                pattern.Regex,
                pattern.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None
            );

            var match = regex.Match(branchName);
            if (match.Success)
            {
                var value = match.Groups[pattern.CaptureGroup].Value;
                if (int.TryParse(value, out var workItemId))
                {
                    _logger.LogDebug(
                        "成功解析 Work Item ID: {WorkItemId} from Branch: {BranchName} using pattern: {PatternName}",
                        workItemId, branchName, pattern.Name
                    );
                    return workItemId;
                }
            }
        }

        _logger.LogWarning(
            "無法從 Branch 名稱解析 Work Item ID: {BranchName}",
            branchName
        );
        return null;
    }
}
```

**優點**:
- 支援多種命名規則,彈性高
- 可透過組態檔調整,無需修改程式碼
- 清楚的錯誤處理策略設定

**待確認**: 向使用者確認實際需求後調整設計

---

## 研究項目 5: appsettings.secure.json 安全載入機制

### 問題陳述
如何確保 secure.json 不納入版控但能正確載入?

### 最佳實務設計

#### .gitignore 設定
```gitignore
# 排除敏感組態檔
appsettings.secure.json
appsettings.*.secure.json
```

#### 組態檔載入邏輯 (Program.cs)
```csharp
var builder = Host.CreateApplicationBuilder(args);

// 載入組態檔
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.secure.json", optional: true, reloadOnChange: true) // optional: true
    .AddEnvironmentVariables();
```

#### 範本檔案管理
建立 `appsettings.secure.json.template` (納入版控):
```json
{
  "GitLab": {
    "PersonalAccessToken": "<YOUR_GITLAB_PAT>"
  },
  "BitBucket": {
    "AccessToken": "<YOUR_BITBUCKET_TOKEN>",
    "Email": "<YOUR_EMAIL>"
  },
  "AzureDevOps": {
    "PersonalAccessToken": "<YOUR_AZURE_DEVOPS_PAT>"
  }
}
```

**使用者設定步驟**:
1. 複製範本: `cp appsettings.secure.json.template appsettings.secure.json`
2. 填入實際 Token
3. appsettings.secure.json 已在 .gitignore 中,不會意外提交

#### README 說明範例
```markdown
## 組態設定

1. 複製範本檔案:
   ```bash
   cp src/ReleaseSync.Console/appsettings.secure.json.template src/ReleaseSync.Console/appsettings.secure.json
   ```

2. 編輯 `appsettings.secure.json`,填入實際的 API Token:
   - GitLab Personal Access Token
   - BitBucket Access Token 與 Email
   - Azure DevOps Personal Access Token

3. 確認 `.gitignore` 已包含 `appsettings.secure.json`,避免意外提交敏感資訊
```

### 決策

**採用 appsettings.secure.json + optional: true + 範本檔案管理**

**理由**:
1. .NET Configuration Provider 原生支援,實作簡單
2. optional: true 確保在 CI/CD 環境可使用環境變數替代
3. 範本檔案提供清楚的設定指引
4. .gitignore 防止敏感資訊意外提交

---

## 研究項目 6: MediatR 是否必要?

### 問題陳述
Console 應用程式是否需要引入 MediatR 實作 CQRS Query Handler?

### 評估分析

#### 選項 1: 使用 MediatR
**優點**:
- 清楚的 CQRS 架構邊界
- 易於實作 Pipeline Behaviors (日誌、驗證、異常處理)
- 解耦命令處理邏輯

**缺點**:
- 增加專案複雜度 (額外學習曲線)
- Console 應用程式通常較簡單,可能過度工程化
- 額外的 NuGet 相依性

#### 選項 2: 直接呼叫 Application Service ⭐ 推薦
**優點**:
- **符合 KISS 原則** (Keep It Simple, Stupid)
- 減少抽象層,程式碼更直接易懂
- 無額外 NuGet 相依性
- Console 應用程式無需複雜的請求處理管線

**缺點**:
- 無法輕易共用 Pipeline Behaviors
- 跨領域關注點 (日誌、驗證) 需在應用層手動處理

### 決策

**不使用 MediatR,直接呼叫 Application Service**

**理由**:
1. **KISS 原則**: Console 工具無需複雜的請求管線
2. **簡化架構**: 減少抽象層,降低學習曲線
3. **維護性**: 程式碼更直接,易於理解與維護
4. **效能**: 減少一層間接呼叫

**替代方案**:
- 使用 Decorator Pattern 或 Middleware 處理跨領域關注點 (如需要)
- 在 Application Service 內部實作日誌與驗證邏輯

**程式碼範例**:
```csharp
// Program.cs - 直接呼叫 Application Service
var syncOrchestrator = serviceProvider.GetRequiredService<ISyncOrchestrator>();
var result = await syncOrchestrator.SyncAsync(syncRequest);
```

---

## 總結

### 技術決策摘要

| 項目 | 決策 | 理由 |
|------|------|------|
| **GitLab API 用戶端** | NGitLab | 唯一持續維護的函式庫,企業級支援 |
| **BitBucket API 用戶端** | HttpClient (直接呼叫) | 所有函式庫已停止維護,自建方案更穩健 |
| **Azure DevOps API 用戶端** | Microsoft.TeamFoundationServer.Client | 官方維護,功能完整,.NET 8 相容 |
| **Regex 設定** | 可設定化多 Regex 支援 | 保留彈性,待使用者確認後調整 |
| **secure.json 管理** | optional: true + 範本檔案 | .NET 原生支援,易於使用 |
| **MediatR** | 不使用 | 符合 KISS 原則,降低複雜度 |

### NuGet 套件清單

**確定需要**:
```xml
<PackageReference Include="NGitLab" Version="9.3.0" />
<PackageReference Include="Microsoft.TeamFoundationServer.Client" Version="19.225.1" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
```

**不需要**:
- ❌ BitBucket 第三方函式庫 (使用 HttpClient)
- ❌ MediatR (遵循 KISS 原則)

### 待使用者確認事項

1. **BitBucket 版本**: Cloud / Data Center / Server?
2. **Regex 設定需求**: 固定或可設定?是否需要多 Regex 支援?
3. **錯誤處理策略**: 無法解析 Work Item ID 時的行為?

**建議**: 在進入 Phase 1 (Design Artifacts) 前向使用者確認上述問題,以便產生精確的 data-model.md 與 contracts/。

---

**Phase 0 Complete** ✅
**下一步**: Phase 1 - 產生 data-model.md, contracts/, quickstart.md
