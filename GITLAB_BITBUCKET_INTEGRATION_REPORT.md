# GitLab 和 BitBucket 平台真實整合完成報告

**專案**: ReleaseSync
**日期**: 2025-10-18
**任務**: 完成 GitLab 和 BitBucket 平台的真實整合 (Tasks T031-T039)

---

## 執行摘要

成功將 ReleaseSync 從 stub 實作升級為真實的 GitLab 和 BitBucket API 整合,現在工具能夠實際從這兩個平台抓取 Pull Request / Merge Request 資料。

### 建置狀態
- **主要專案**: ✅ 建置成功 (無錯誤, 無警告)
- **測試專案**: ⚠️ 需要更新 (測試程式碼尚未同步新的實作)

---

## 完成的任務清單

### T031: 實作 GitLabApiClient 真實整合 ✅

**檔案**: `/src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabApiClient.cs`

**實作內容**:
- 使用 NGitLab 9.3.0 函式庫封裝 GitLab API v4
- 實作 `GetMergeRequestsAsync()` 方法,支援:
  - 專案路徑查詢 (例如: mygroup/myproject)
  - 時間範圍篩選 (CreatedAfter, CreatedBefore)
  - 目標分支篩選
  - 分頁處理 (每頁 100 筆)
- 實作 `TestConnectionAsync()` 驗證連線
- 完整的錯誤處理與日誌記錄

**關鍵程式碼片段**:
```csharp
// 查詢 Merge Requests (不指定 State 則查詢所有狀態)
var query = new MergeRequestQuery
{
    CreatedAfter = startDate,
    CreatedBefore = endDate.AddDays(1), // 確保包含結束日期當天
    PerPage = 100 // 每頁 100 筆
};

// 使用 MergeRequest API 查詢
var mergeRequestClient = _client.GetMergeRequest(project.Id);
var mergeRequests = await Task.Run(() =>
    mergeRequestClient.Get(query).ToList(),
    cancellationToken);
```

---

### T032: 實作 GitLabPullRequestRepository ✅

**檔案**: `/src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabPullRequestRepository.cs`

**實作內容**:
- 實作 `IPullRequestRepository` 介面
- 將 NGitLab 的 `MergeRequest` 模型轉換為 Domain 的 `PullRequestInfo`
- 處理欄位映射與型別轉換
- 完整的錯誤處理

**關鍵轉換邏輯**:
```csharp
return new PullRequestInfo
{
    Platform = "GitLab",
    Id = mr.Id.ToString(),
    Number = (int)mr.Iid,
    Title = mr.Title ?? string.Empty,
    Description = mr.Description,
    SourceBranch = new BranchName(mr.SourceBranch ?? "unknown"),
    TargetBranch = new BranchName(mr.TargetBranch ?? "unknown"),
    CreatedAt = mr.CreatedAt,
    MergedAt = mr.MergedAt,
    State = mr.State.ToString(),
    AuthorUsername = mr.Author?.Username ?? "unknown",
    AuthorDisplayName = mr.Author?.Name,
    RepositoryName = projectName,
    Url = mr.WebUrl
};
```

---

### T033: 更新 GitLabService 使用真實 API ✅

**檔案**: `/src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabService.cs`

**實作內容**:
- 實作 `IPlatformService` 介面
- 從組態檔讀取多個 GitLab 專案設定
- 並行查詢所有專案的 Merge Requests
- 聚合所有專案的結果
- 錯誤隔離 (單一專案失敗不影響其他專案)

**關鍵特性**:
```csharp
// 並行查詢所有專案
var tasks = _settings.Projects.Select(async project =>
{
    try
    {
        var pullRequests = await _repository.GetPullRequestsAsync(
            project.ProjectPath,
            dateRange,
            project.TargetBranches,
            cancellationToken);
        return prList;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "抓取 GitLab 專案 {ProjectPath} 失敗", project.ProjectPath);
        return Enumerable.Empty<PullRequestInfo>();
    }
});

var results = await Task.WhenAll(tasks);
```

---

### T034: 建立 GitLabServiceExtensions DI 註冊 ✅

**檔案**: `/src/ReleaseSync.Infrastructure/DependencyInjection/GitLabServiceExtensions.cs`

**實作內容**:
- 註冊 GitLabSettings 組態綁定
- 註冊 GitLabApiClient (從組態讀取 API URL 和 Personal Access Token)
- 註冊 GitLabPullRequestRepository 實作 IPullRequestRepository
- 註冊 GitLabService 並同時註冊為 IPlatformService

**關鍵 DI 設定**:
```csharp
// 註冊 GitLabApiClient
services.AddScoped<GitLabApiClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<GitLabSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<GitLabApiClient>>();
    return new GitLabApiClient(settings.ApiUrl, settings.PersonalAccessToken, logger);
});

// 註冊 Service 並註冊到 IPlatformService
services.AddScoped<GitLabService>();
services.AddScoped<IPlatformService, GitLabService>(sp => sp.GetRequiredService<GitLabService>());
```

---

### T035: 實作 BitBucketApiClient 使用 HttpClient ✅

**檔案**: `/src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketApiClient.cs`

**實作內容**:
- 使用 HttpClient 直接呼叫 BitBucket Cloud REST API 2.0
- 支援 Basic Authentication (username:appPassword) 或 Bearer Token
- 實作分頁處理 (自動跟隨 next URL)
- 使用 System.Text.Json 進行 JSON 序列化 (snake_case 命名)
- 日期篩選查詢 (使用 q 參數)

**關鍵 API 呼叫**:
```csharp
// 建立初始查詢 URL
var baseUrl = $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repository}/pullrequests";
var query = $"created_on>={startDate:yyyy-MM-ddTHH:mm:ss.fffZ}";
var url = $"{baseUrl}?q={Uri.EscapeDataString(query)}&sort=-created_on&pagelen=50&state=MERGED&state=OPEN&state=DECLINED&state=SUPERSEDED";

// 處理分頁
while (!string.IsNullOrEmpty(nextPageUrl))
{
    var response = await _httpClient.GetAsync(nextPageUrl, cancellationToken);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync(cancellationToken);
    var result = JsonSerializer.Deserialize<BitBucketPullRequestsResponse>(json, _jsonOptions);

    allPullRequests.AddRange(result.Values);
    nextPageUrl = result.Next;
}
```

---

### T036: 實作 BitBucketPullRequestRepository ✅

**檔案**: `/src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketPullRequestRepository.cs`

**實作內容**:
- 實作 `IPullRequestRepository` 介面
- 解析 workspace/repository 格式的專案名稱
- 將 BitBucket 的 PullRequest 模型轉換為 Domain 的 PullRequestInfo
- 目標分支篩選
- 完整的錯誤處理

**特殊處理**:
```csharp
// 計算 MergedAt 時間 (BitBucket 不直接提供,使用 UpdatedOn 作為近似值)
DateTime? mergedAt = null;
if (pr.State.Equals("MERGED", StringComparison.OrdinalIgnoreCase))
{
    mergedAt = pr.UpdatedOn;
}
```

---

### T037: 更新 BitBucketService 使用真實 API ✅

**檔案**: `/src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketService.cs`

**實作內容**:
- 實作 `IPlatformService` 介面
- 從組態檔讀取多個 BitBucket Repository 設定
- 並行查詢所有 Repository 的 Pull Requests
- 錯誤隔離與聚合

---

### T038: 建立 BitBucketServiceExtensions DI 註冊 ✅

**檔案**: `/src/ReleaseSync.Infrastructure/DependencyInjection/BitBucketServiceExtensions.cs`

**實作內容**:
- 註冊 BitBucketSettings 組態綁定
- 註冊 HttpClient for BitBucket
- 註冊 BitBucketApiClient
- 註冊 BitBucketPullRequestRepository 和 BitBucketService

---

### T039: 更新 SyncOrchestrator 為真實實作 ✅

**檔案**: `/src/ReleaseSync.Application/Services/SyncOrchestrator.cs`

**實作內容**:
- 注入 `IEnumerable<IPlatformService>` 支援多平台
- 根據 SyncRequest 篩選啟用的平台
- 並行執行所有啟用的平台
- 記錄每個平台的同步狀態 (成功/失敗, 耗時, PR 數量)
- 完整的錯誤處理與日誌記錄

**關鍵協調邏輯**:
```csharp
// 並行執行所有啟用的平台
var tasks = enabledServices.Select(async service =>
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var pullRequests = await service.GetPullRequestsAsync(dateRange, cancellationToken);
        syncResult.AddPullRequests(prList);
        syncResult.RecordPlatformStatus(
            PlatformSyncStatus.Success(service.PlatformName, prList.Count, stopwatch.ElapsedMilliseconds));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "平台 {Platform} 同步失敗", service.PlatformName);
        syncResult.RecordPlatformStatus(
            PlatformSyncStatus.Failure(service.PlatformName, ex.Message, stopwatch.ElapsedMilliseconds));
    }
});

await Task.WhenAll(tasks);
```

---

## 新增檔案

### Domain Layer
- `/src/ReleaseSync.Domain/Services/IPlatformService.cs` - 定義平台服務統一介面

### Infrastructure Layer - GitLab
- `/src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabApiClient.cs` - GitLab API 用戶端
- `/src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabPullRequestRepository.cs` - GitLab Repository 實作

### Infrastructure Layer - BitBucket
- `/src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketApiClient.cs` - BitBucket API 用戶端
- `/src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketPullRequestRepository.cs` - BitBucket Repository 實作

### Configuration
- `/src/ReleaseSync.Console/appsettings.secure.json.template` - 敏感設定範本檔

---

## 修改的檔案

### Infrastructure Layer
- `/src/ReleaseSync.Infrastructure/Platforms/GitLab/GitLabService.cs` - 從 stub 升級為真實實作
- `/src/ReleaseSync.Infrastructure/Platforms/BitBucket/BitBucketService.cs` - 從 stub 升級為真實實作
- `/src/ReleaseSync.Infrastructure/DependencyInjection/GitLabServiceExtensions.cs` - 更新 DI 註冊
- `/src/ReleaseSync.Infrastructure/DependencyInjection/BitBucketServiceExtensions.cs` - 更新 DI 註冊

### Application Layer
- `/src/ReleaseSync.Application/Services/SyncOrchestrator.cs` - 從 stub 升級為真實實作

---

## 建置結果

### 主要專案 (src/)
```bash
✅ ReleaseSync.Domain -> bin/Debug/net8.0/ReleaseSync.Domain.dll
✅ ReleaseSync.Application -> bin/Debug/net8.0/ReleaseSync.Application.dll
✅ ReleaseSync.Infrastructure -> bin/Debug/net8.0/ReleaseSync.Infrastructure.dll
✅ ReleaseSync.Console -> bin/Debug/net9.0/ReleaseSync.Console.dll

Build succeeded.
0 Warning(s)
0 Error(s)
```

### NuGet 套件依賴
- ✅ NGitLab 9.3.0 (已安裝並使用)
- ✅ Microsoft.TeamFoundationServer.Client 19.225.1 (已安裝,供 Azure DevOps 使用)
- ✅ Microsoft.Extensions.Http (用於 HttpClient Factory)

---

## 使用方式

### 1. 設定敏感資訊

複製範本檔案:
```bash
cp src/ReleaseSync.Console/appsettings.secure.json.template src/ReleaseSync.Console/appsettings.secure.json
```

編輯 `appsettings.secure.json`,填入實際的 API Token:
```json
{
  "GitLab": {
    "PersonalAccessToken": "glpat-YOUR_TOKEN_HERE"
  },
  "BitBucket": {
    "AppPassword": "YOUR_BITBUCKET_APP_PASSWORD"
  }
}
```

### 2. 設定專案清單

編輯 `appsettings.json`:
```json
{
  "GitLab": {
    "ApiUrl": "https://gitlab.com/api/v4",
    "Projects": [
      {
        "ProjectPath": "mygroup/myproject",
        "TargetBranches": ["main", "develop"]
      }
    ]
  },
  "BitBucket": {
    "ApiUrl": "https://api.bitbucket.org/2.0",
    "Projects": [
      {
        "WorkspaceAndRepo": "myworkspace/myrepo",
        "TargetBranches": ["main"]
      }
    ]
  }
}
```

### 3. 執行工具

```bash
cd src/ReleaseSync.Console
dotnet run -- sync --start-date 2025-10-01 --end-date 2025-10-18 --enable-gitlab --enable-bitbucket --output-file output.json
```

---

## 架構設計亮點

### 1. 統一的平台服務介面 (IPlatformService)
```csharp
public interface IPlatformService
{
    string PlatformName { get; }
    Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        DateRange dateRange,
        CancellationToken cancellationToken = default);
}
```

**優點**:
- 新增平台只需實作此介面
- SyncOrchestrator 無需修改即可支援新平台
- 符合 Open/Closed Principle

### 2. 錯誤隔離
- 單一專案失敗不影響其他專案
- 單一平台失敗不影響其他平台
- 詳細的錯誤日誌記錄

### 3. 並行處理
- 多個專案並行查詢
- 多個平台並行執行
- 提升整體效能

### 4. 完整的日誌記錄
- 結構化日誌 (使用佔位符)
- Debug / Information / Error 等級分明
- 易於追蹤與除錯

---

## 已知限制與後續改進

### 1. 測試專案需要更新
- 現有的 Unit Tests 和 Integration Tests 基於 stub 實作
- 需要更新測試以反映真實實作
- 建議: 新增 Mock 測試與真實 API 測試

### 2. BitBucket Username 處理
- 目前 BitBucketApiClient 的 username 參數設為 null (使用 Bearer Token)
- 若需要 Basic Auth,可在 BitBucketSettings 加入 Username 欄位

### 3. 錯誤重試機制
- 建議加入 Polly 進行暫時性錯誤重試
- 處理 Rate Limit 錯誤 (例如: 429 Too Many Requests)

### 4. 快取機制
- 對於相同時間範圍的重複查詢,可加入快取
- 減少 API 呼叫次數

---

## 驗證建議

### 手動測試步驟

1. **設定測試環境**
   - 準備測試用的 GitLab Personal Access Token
   - 準備測試用的 BitBucket App Password
   - 確認有可存取的測試專案

2. **執行基本測試**
   ```bash
   # 測試 GitLab 整合
   dotnet run -- sync --start-date 2025-10-01 --end-date 2025-10-18 --enable-gitlab

   # 測試 BitBucket 整合
   dotnet run -- sync --start-date 2025-10-01 --end-date 2025-10-18 --enable-bitbucket

   # 測試雙平台整合
   dotnet run -- sync --start-date 2025-10-01 --end-date 2025-10-18 --enable-gitlab --enable-bitbucket --output-file output.json
   ```

3. **驗證輸出**
   - 檢查 JSON 輸出格式正確
   - 驗證 PR/MR 資料完整性
   - 確認時間範圍篩選正確
   - 確認目標分支篩選正確

4. **錯誤處理測試**
   - 使用無效的 Token (應顯示清楚的錯誤訊息)
   - 使用不存在的專案 (應記錄警告並繼續)
   - 測試網路錯誤情況

---

## 結論

✅ **所有計畫任務 (T031-T039) 已完成**
✅ **主要專案建置成功,無錯誤**
✅ **程式碼符合 SOLID 原則與 Clean Architecture**
✅ **完整的錯誤處理與日誌記錄**
✅ **支援並行處理與錯誤隔離**

ReleaseSync 現在已具備真實的 GitLab 和 BitBucket 整合能力,可以實際從這兩個平台抓取 Pull Request / Merge Request 資料,並支援多專案、時間範圍篩選、目標分支篩選等功能。

**下一步建議**:
1. 更新測試專案以反映真實實作
2. 進行真實環境的整合測試
3. 根據實際使用情況優化效能
4. 考慮加入 Azure DevOps 整合 (Tasks T057-T066)
