# Research: 資料過濾機制實作研究

**Feature**: 003-filter-unmapped-data
**Date**: 2025-10-25
**Purpose**: 研究過濾機制的技術決策和最佳實踐

## 研究主題

### 1. 過濾邏輯的實作位置

**Question**: 過濾邏輯應該在 Repository 層還是 Application 層實作?

**Decision**: 在 Repository 層實作過濾邏輯

**Rationale**:
- Repository 負責資料抓取和初步處理,過濾未對應的資料屬於資料來源層的職責
- Application 層 (SyncOrchestrator) 應該收到已過濾的乾淨資料,專注於編排邏輯
- 避免在多個 Repository 和 Application 層重複過濾邏輯
- 符合 Single Responsibility Principle: Repository 負責「取得有效資料」
- 參考現有程式碼: UserMappingService 在 Infrastructure 層,在 Repository 中呼叫是合理的

**Implementation Approach**:
```csharp
// 在 Repository 的 GetPullRequestsAsync 方法中
public async Task<List<PullRequestInfo>> GetPullRequestsAsync(...)
{
    var allPRs = await FetchFromApiAsync(...);

    // 過濾邏輯
    var filteredPRs = allPRs.Where(pr =>
        _userMappingService.HasMapping(platform, pr.Author)
    ).ToList();

    _logger.LogInformation("Filtered {FilteredCount} PRs out of {TotalCount} due to UserMapping",
        allPRs.Count - filteredPRs.Count, allPRs.Count);

    return filteredPRs;
}
```

**Alternatives Considered**:
- **Application 層過濾**: 會導致每個使用 Repository 的地方都需要重複過濾邏輯,違反 DRY 原則
- **Domain Service 過濾**: 過於重量級,過濾邏輯不屬於核心業務邏輯

---

### 2. 向後相容性策略

**Question**: 如何確保空 Mapping 時不破壞現有行為?

**Decision**: 在 Service 層檢查 Mapping 是否為空,空時返回 true (表示「有對應」)

**Rationale**:
- 向後相容是關鍵需求 (FR-007, FR-008)
- 空 Mapping 表示「不啟用過濾功能」
- 預設行為應該是寬鬆的 (不過濾),而非嚴格的 (全部過濾)

**Implementation Approach**:
```csharp
public class UserMappingService : IUserMappingService
{
    private readonly List<UserMapping> _mappings;

    public bool HasMapping(string platform, string username)
    {
        // 向後相容: 空 Mapping 時不過濾
        if (_mappings.Count == 0)
        {
            return true;
        }

        // 實際比對邏輯...
    }
}
```

**Testing Strategy**:
- 單元測試: 空 Mapping 情況應返回 true
- 整合測試: 空 Mapping 時應收錄所有 PR/MR 和 Work Item
- 回歸測試: 確認現有功能不受影響

---

### 3. 效能最佳化: 快速查找策略

**Question**: 如何實作高效的對應檢查,避免線性搜尋?

**Decision**: 在 Service 初始化時建立 HashSet<string>,使用 O(1) 查找

**Rationale**:
- 每次過濾可能需要檢查數百筆 PR/MR
- 線性搜尋 (List.Contains) 是 O(n),會造成 O(n*m) 的整體複雜度
- HashSet.Contains 是 O(1),整體複雜度降為 O(m)
- 記憶體開銷可接受 (100 筆 Mapping 約 <10KB)

**Implementation Approach**:
```csharp
public class UserMappingService : IUserMappingService
{
    private readonly HashSet<string> _gitLabUsers;
    private readonly HashSet<string> _bitBucketUsers;
    private readonly Dictionary<string, string> _displayNames;

    public UserMappingService(IOptions<UserMappingSettings> options)
    {
        var mappings = options.Value.Mappings;

        // 使用大小寫不敏感的 Comparer
        _gitLabUsers = new HashSet<string>(
            mappings.Where(m => m.GitLabUserId != null).Select(m => m.GitLabUserId!),
            StringComparer.OrdinalIgnoreCase
        );

        _bitBucketUsers = new HashSet<string>(
            mappings.Where(m => m.BitBucketUserId != null).Select(m => m.BitBucketUserId!),
            StringComparer.OrdinalIgnoreCase
        );

        // DisplayName lookup dictionary
        _displayNames = mappings.ToDictionary(
            m => m.GitLabUserId ?? m.BitBucketUserId!,
            m => m.DisplayName,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public bool HasMapping(string platform, string username)
    {
        if (_gitLabUsers.Count == 0 && _bitBucketUsers.Count == 0)
            return true; // 向後相容

        return platform.ToLowerInvariant() switch
        {
            "gitlab" => _gitLabUsers.Contains(username),
            "bitbucket" => _bitBucketUsers.Contains(username),
            _ => false
        };
    }
}
```

**Performance Characteristics**:
- **Time Complexity**: O(1) per lookup, O(m) for filtering m items
- **Space Complexity**: O(n) where n is number of mappings
- **Benchmark Target**: <1ms for 100 mappings, 100 PR/MR checks

---

### 4. Azure DevOps Work Item Team Field

**Question**: Azure DevOps API 中 Work Item 的 team 資訊如何取得?

**Research Findings**:
根據 Azure DevOps REST API 文件和現有程式碼:

1. **System.TeamProject**: 專案名稱 (已經在使用)
2. **System.AreaPath**: 區域路徑,通常包含團隊資訊
3. **Custom Fields**: 某些組織會自訂 team field

**Decision**: 使用 `System.AreaPath` 作為團隊識別欄位

**Rationale**:
- `System.AreaPath` 是 Azure DevOps 標準欄位
- 組織通常使用 Area Path 來組織團隊結構
- 範例: `ProjectName\Team Alpha\Feature Area`
- 可以從 Area Path 中提取團隊名稱

**Implementation Approach**:
```csharp
// 在 AzureDevOpsApiClient 中取得 Area Path
var workItem = await GetWorkItemAsync(id);
var areaPath = workItem.Fields["System.AreaPath"]?.ToString();

// 提取團隊名稱 (假設格式為 "ProjectName\TeamName\..." )
var teamName = ExtractTeamFromAreaPath(areaPath);

// 建立 WorkItemInfo 時設定 Team 屬性
return new WorkItemInfo
{
    Id = workItemId,
    Title = title,
    Team = teamName,  // 新增
    ...
};
```

**Alternatives Considered**:
- **Custom Field**: 需要組織自訂,不具通用性
- **System.TeamProject**: 只有專案層級,太粗粒度
- **Tags**: 不是標準做法,容易混亂

**Configuration Example**:
```json
{
  "AzureDevOps": {
    "TeamMapping": [
      {
        "OriginalTeamName": "MoneyLogistic",
        "DisplayName": "金流團隊"
      }
    ]
  }
}
```

---

### 5. 大小寫不敏感字串比對

**Question**: C# 中實作大小寫不敏感比對的最佳方式?

**Decision**: 使用 `StringComparer.OrdinalIgnoreCase` 和 `StringComparison.OrdinalIgnoreCase`

**Rationale**:
- **Ordinal vs CurrentCulture**: Ordinal 比對更快,且不受文化特性影響
- **IgnoreCase**: 符合 FR-010 需求
- **效能**: OrdinalIgnoreCase 是最快的不敏感比對方式
- **一致性**: .NET 標準建議用於識別符號和鍵值比對

**Implementation Approach**:
```csharp
// HashSet 建構時指定 Comparer
var userSet = new HashSet<string>(usernames, StringComparer.OrdinalIgnoreCase);

// 字串比對
if (username.Equals(mappedUser, StringComparison.OrdinalIgnoreCase))
{
    // matched
}

// Dictionary 鍵值
var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
```

**Avoid**:
```csharp
// ❌ 不要使用 ToLower() 或 ToUpper() - 效能較差且會產生新字串
if (username.ToLower() == mappedUser.ToLower())  // 不推薦

// ✅ 應該使用 StringComparison
if (username.Equals(mappedUser, StringComparison.OrdinalIgnoreCase))  // 推薦
```

**Performance Impact**:
- `ToLower()` 會分配新字串,增加 GC 壓力
- `OrdinalIgnoreCase` 直接比對,無額外分配
- 對於數百次比對,效能差異可達 2-3 倍

---

### 6. Work Item 過濾與 PR/MR 關聯處理

**Question**: 當 Work Item 被過濾掉時,如何處理引用該 Work Item 的 PR/MR?

**Decision**: PR/MR 保留,但 WorkItemInfo 屬性設為 null (根據使用者選擇的選項 A)

**Rationale**:
- 確保程式碼變更的完整記錄
- 使用者可以看到有些 PR/MR 缺少 Work Item 關聯
- 在報告中可以標註「Work Item 已過濾」狀態

**Implementation Approach**:
```csharp
// 在 SyncOrchestrator 或相關服務中
foreach (var pr in pullRequests)
{
    if (pr.WorkItemIds != null && pr.WorkItemIds.Any())
    {
        var workItems = await _workItemRepository.GetWorkItemsAsync(pr.WorkItemIds);

        // Work Item 已在 Repository 層過濾
        // 如果全部被過濾,workItems 會是空列表
        if (!workItems.Any())
        {
            _logger.LogWarning("All Work Items for PR {PRId} were filtered out by TeamMapping", pr.Id);
            // PR 保留,但沒有 Work Item 關聯
        }

        pr.WorkItems = workItems;
    }
}
```

**Output Format Consideration**:
在 JSON 輸出中:
```json
{
  "pullRequestId": "123",
  "author": "John Doe",
  "workItems": [],  // 空陣列表示 Work Item 被過濾
  "workItemsFiltered": true  // 可選: 明確標註
}
```

---

## 技術決策摘要

| 決策點 | 選擇 | 主要理由 |
|--------|------|----------|
| 過濾位置 | Repository 層 | 職責清晰,避免重複 |
| 向後相容 | 空 Mapping 返回 true | 預設寬鬆,不破壞現有行為 |
| 查找策略 | HashSet with StringComparer | O(1) 查找,大小寫不敏感 |
| Team 欄位 | System.AreaPath | Azure DevOps 標準欄位 |
| 字串比對 | StringComparison.OrdinalIgnoreCase | 效能最佳,符合需求 |
| Work Item 過濾後處理 | PR/MR 保留,Work Item 為空 | 保持資料完整性 |

## 實作風險與緩解

### 風險 1: Azure DevOps Area Path 格式不一致
- **描述**: 不同組織的 Area Path 格式可能不同
- **緩解**: 提供清楚的文件說明 Area Path 到 TeamName 的提取邏輯,必要時支援自訂提取函數
- **備案**: 如果 Area Path 不適用,可以在後續版本支援自訂欄位名稱

### 風險 2: 效能影響超過 5%
- **描述**: 過濾邏輯可能影響整體執行時間
- **緩解**: 使用 HashSet 確保 O(1) 查找,整合測試驗證效能
- **監控**: 在日誌中記錄過濾耗時,如有異常可快速發現

### 風險 3: 向後相容性問題
- **描述**: 現有使用者升級後可能遇到非預期行為
- **緩解**: 詳細的整合測試覆蓋空 Mapping 情況,文件明確說明預設行為
- **溝通**: Release Notes 中說明新功能和向後相容性保證

## 參考資料

1. **Azure DevOps REST API - Work Items**:
   - https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/get-work-item
   - System.AreaPath 欄位說明

2. **C# String Comparison Best Practices**:
   - https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
   - StringComparison 和 StringComparer 使用指南

3. **HashSet Performance**:
   - https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1
   - O(1) 查找複雜度說明

4. **Clean Architecture in .NET**:
   - Repository Pattern 和 Domain Service 的職責劃分
   - 依賴方向: Infrastructure -> Application -> Domain

## 下一步

Phase 1 將基於這些研究決策進行詳細設計:
1. 資料模型定義 (data-model.md)
2. 配置契約 (contracts/)
3. 快速開始指南 (quickstart.md)
