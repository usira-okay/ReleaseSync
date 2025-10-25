# Data Model: 資料過濾機制

**Feature**: 003-filter-unmapped-data
**Date**: 2025-10-25
**Status**: Design Phase

## Overview

本文件定義資料過濾機制涉及的資料模型,包括配置結構、領域實體的變更,以及服務介面。

## Configuration Models (配置模型)

### TeamMapping (團隊對應配置)

**Purpose**: 定義 Azure DevOps Work Item 的團隊欄位與顯示名稱的對應關係

**Location**: `ReleaseSync.Infrastructure/Configuration/TeamMappingSettings.cs`

```csharp
namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// 團隊對應設定
/// </summary>
public class TeamMappingSettings
{
    /// <summary>
    /// 團隊對應清單
    /// </summary>
    public List<TeamMapping> Mappings { get; init; } = new();
}

/// <summary>
/// 團隊對應
/// </summary>
public class TeamMapping
{
    /// <summary>
    /// Azure DevOps Work Item 的原始團隊名稱 (來自 System.AreaPath)
    /// </summary>
    public required string OriginalTeamName { get; init; }

    /// <summary>
    /// 顯示名稱 (用於報表)
    /// </summary>
    public required string DisplayName { get; init; }
}
```

**Validation Rules**:
- `OriginalTeamName`: 必填,不可為空字串
- `DisplayName`: 必填,不可為空字串
- 大小寫不敏感比對 (使用 StringComparer.OrdinalIgnoreCase)

**JSON Example**:
```json
{
  "TeamMapping": [
    {
      "OriginalTeamName": "MoneyLogistic",
      "DisplayName": "金流團隊"
    },
    {
      "OriginalTeamName": "DailyResource",
      "DisplayName": "日常資源團隊"
    },
    {
      "OriginalTeamName": "Commerce",
      "DisplayName": "商務團隊"
    }
  ]
}
```

---

### AzureDevOpsSettings (擴展)

**Purpose**: 在現有的 Azure DevOps 配置中加入 TeamMapping 支援

**Location**: `ReleaseSync.Infrastructure/Configuration/AzureDevOpsSettings.cs`

**Changes**:
```csharp
namespace ReleaseSync.Infrastructure.Configuration;

/// <summary>
/// Azure DevOps 組態
/// </summary>
public class AzureDevOpsSettings
{
    // ... 現有屬性 ...

    /// <summary>
    /// 團隊對應設定 (用於過濾 Work Item)
    /// </summary>
    public List<TeamMapping> TeamMapping { get; init; } = new();  // 新增
}
```

**JSON Example**:
```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/myorg",
    "ProjectName": "MyProject",
    "WorkItemIdPatterns": [ ... ],
    "ParsingBehavior": { ... },
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

## Domain Models (領域模型)

### WorkItemInfo (擴展)

**Purpose**: 加入 Team 屬性用於團隊過濾和顯示

**Location**: `ReleaseSync.Domain/Models/WorkItemInfo.cs`

**Changes**:
```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// Work Item 資訊實體
/// </summary>
public class WorkItemInfo
{
    // ... 現有屬性 ...

    /// <summary>
    /// 團隊名稱 (來自 Azure DevOps Area Path 或自訂欄位)
    /// </summary>
    /// <remarks>
    /// 用於團隊過濾和報告顯示。
    /// 如果 Work Item 未包含團隊資訊或被過濾掉,此屬性可能為 null。
    /// </remarks>
    public string? Team { get; init; }  // 新增
}
```

**Field Details**:
- **Name**: Team
- **Type**: `string?` (nullable)
- **Source**: 從 Azure DevOps API 的 `System.AreaPath` 欄位提取
- **Usage**:
  - 過濾依據: 判斷是否在 TeamMapping 中
  - 顯示用途: 輸出到報告時使用對應的 DisplayName
- **Nullable**: 允許為 null,因為:
  - Work Item 可能沒有團隊資訊
  - Work Item 可能被過濾掉

**Validation**:
- 不修改現有的 `Validate()` 方法
- Team 屬性為 optional,不影響實體有效性

---

## Service Interfaces (服務介面)

### ITeamMappingService

**Purpose**: 提供團隊對應查詢和驗證服務

**Location**: `ReleaseSync.Domain/Services/ITeamMappingService.cs` (新增)

```csharp
namespace ReleaseSync.Domain.Services;

/// <summary>
/// 團隊對應服務介面
/// </summary>
public interface ITeamMappingService
{
    /// <summary>
    /// 檢查指定的團隊名稱是否在對應清單中
    /// </summary>
    /// <param name="originalTeamName">原始團隊名稱 (來自 Azure DevOps)</param>
    /// <returns>
    /// 如果團隊在對應清單中返回 true;
    /// 如果團隊不在清單中返回 false;
    /// 如果對應清單為空 (未啟用過濾) 返回 true (向後相容)
    /// </returns>
    bool HasMapping(string? originalTeamName);

    /// <summary>
    /// 根據原始團隊名稱取得對應的顯示名稱
    /// </summary>
    /// <param name="originalTeamName">原始團隊名稱</param>
    /// <returns>
    /// 對應的顯示名稱;如果無對應則返回原始名稱
    /// </returns>
    string GetDisplayName(string? originalTeamName);

    /// <summary>
    /// 檢查是否啟用團隊過濾 (是否有配置 TeamMapping)
    /// </summary>
    /// <returns>如果 TeamMapping 非空返回 true</returns>
    bool IsFilteringEnabled();
}
```

**Implementation Notes**:
- 大小寫不敏感比對
- 向後相容: 空 Mapping 時 `HasMapping` 返回 true
- 使用 HashSet 快速查找 (O(1))

---

### IUserMappingService (既有,參考)

**Purpose**: 提供使用者對應查詢服務 (既有介面,供參考)

**Location**: `ReleaseSync.Application/Services/IUserMappingService.cs`

```csharp
namespace ReleaseSync.Application.Services;

/// <summary>
/// 使用者映射服務介面
/// </summary>
public interface IUserMappingService
{
    /// <summary>
    /// 根據平台和使用者名稱取得對應的顯示名稱
    /// </summary>
    /// <param name="platform">平台名稱 (GitLab 或 BitBucket)</param>
    /// <param name="username">使用者名稱</param>
    /// <param name="defaultDisplayName">預設顯示名稱(當無映射時使用)</param>
    /// <returns>映射後的顯示名稱,若無映射則返回預設值</returns>
    string GetDisplayName(string platform, string username, string? defaultDisplayName = null);
}
```

**Enhancement Needed** (新增方法):
```csharp
/// <summary>
/// 檢查指定平台的使用者是否在對應清單中
/// </summary>
/// <param name="platform">平台名稱 (GitLab 或 BitBucket)</param>
/// <param name="username">使用者名稱</param>
/// <returns>
/// 如果使用者在對應清單中返回 true;
/// 如果使用者不在清單中返回 false;
/// 如果對應清單為空 (未啟用過濾) 返回 true (向後相容)
/// </returns>
bool HasMapping(string platform, string? username);

/// <summary>
/// 檢查是否啟用使用者過濾 (是否有配置 UserMapping)
/// </summary>
/// <returns>如果 UserMapping 非空返回 true</returns>
bool IsFilteringEnabled();
```

---

## Repository Interfaces (倉儲介面變更)

### IPullRequestRepository

**Location**: `ReleaseSync.Domain/Repositories/IPullRequestRepository.cs`

**No Changes Required**: 過濾邏輯在實作內部處理,介面不變

**Implementation Note**: Repository 實作將注入 `IUserMappingService` 並在內部過濾

---

### IWorkItemRepository

**Location**: `ReleaseSync.Domain/Repositories/IWorkItemRepository.cs`

**No Changes Required**: 過濾邏輯在實作內部處理,介面不變

**Implementation Note**: Repository 實作將注入 `ITeamMappingService` 並在內部過濾

---

## Data Flow (資料流程)

### PR/MR 過濾流程

```
1. GitLabPullRequestRepository.GetPullRequestsAsync()
   ↓
2. 從 GitLab API 抓取所有 PR
   ↓
3. 呼叫 IUserMappingService.HasMapping("gitlab", pr.Author)
   ↓
4. 過濾掉 HasMapping = false 的 PR
   ↓
5. 記錄過濾統計到日誌
   ↓
6. 返回過濾後的 PR 清單給 Application 層
```

### Work Item 過濾流程

```
1. AzureDevOpsWorkItemRepository.GetWorkItemAsync(id)
   ↓
2. 從 Azure DevOps API 抓取 Work Item
   ↓
3. 從 System.AreaPath 提取團隊名稱
   ↓
4. 呼叫 ITeamMappingService.HasMapping(team)
   ↓
5. 如果 HasMapping = false,不返回該 Work Item (或返回 null)
   ↓
6. 如果 HasMapping = true,返回 Work Item 並使用 GetDisplayName 填充 Team 屬性
   ↓
7. 記錄過濾統計到日誌
```

### Team DisplayName 轉換流程

```
1. Work Item 從 API 取得,包含 System.AreaPath
   ↓
2. 提取 OriginalTeamName (例如: "MoneyLogistic")
   ↓
3. 呼叫 ITeamMappingService.GetDisplayName("MoneyLogistic")
   ↓
4. 返回 DisplayName (例如: "金流團隊")
   ↓
5. WorkItemInfo.Team = "金流團隊"
   ↓
6. 輸出到報告中顯示為 "金流團隊"
```

---

## State Transitions (狀態轉換)

### Work Item 過濾狀態

```
Work Item (從 API)
    ↓
[檢查 TeamMapping]
    ├─→ Team 在 Mapping 中 → 保留,Team = DisplayName
    ├─→ Team 不在 Mapping 中 → 過濾掉 (不返回)
    └─→ TeamMapping 為空 → 保留所有 (向後相容)
```

### PR/MR 與 Work Item 關聯狀態

```
PR/MR (包含 Work Item IDs)
    ↓
[抓取 Work Item]
    ├─→ 所有 Work Item 都被過濾 → PR/MR 保留,WorkItems = [] (選項 A)
    ├─→ 部分 Work Item 被過濾 → PR/MR 保留,WorkItems = 未被過濾的清單
    └─→ 所有 Work Item 都保留 → PR/MR 保留,WorkItems = 完整清單
```

---

## Entity Relationships (實體關係)

```
┌─────────────────────────┐
│   AzureDevOpsSettings   │
│  (Configuration)        │
├─────────────────────────┤
│ + OrganizationUrl       │
│ + ProjectName           │
│ + TeamMapping: List     │ ──┐
└─────────────────────────┘   │
                               │ contains
                               ↓
                    ┌─────────────────────────┐
                    │     TeamMapping         │
                    │  (Configuration)        │
                    ├─────────────────────────┤
                    │ + OriginalTeamName      │
                    │ + DisplayName           │
                    └─────────────────────────┘
                               │
                               │ maps to
                               ↓
┌─────────────────────────┐
│     WorkItemInfo        │
│  (Domain Entity)        │
├─────────────────────────┤
│ + Id                    │
│ + Title                 │
│ + Team: string?  [NEW]  │ ←── Filtered using TeamMapping
│ + State                 │
│ + ...                   │
└─────────────────────────┘
         ↑
         │ referenced by
         │
┌─────────────────────────┐
│   PullRequestInfo       │
│  (Domain Entity)        │
├─────────────────────────┤
│ + Id                    │
│ + Author                │ ←── Filtered using UserMapping
│ + WorkItems: List       │
│ + ...                   │
└─────────────────────────┘
```

---

## Validation Rules Summary (驗證規則摘要)

| Entity | Field | Rules |
|--------|-------|-------|
| TeamMapping | OriginalTeamName | Required, non-empty, 大小寫不敏感 |
| TeamMapping | DisplayName | Required, non-empty |
| WorkItemInfo | Team | Optional (nullable), 來自 AreaPath 提取 |
| ITeamMappingService | HasMapping | 空 Mapping 時返回 true (向後相容) |
| IUserMappingService | HasMapping | 空 Mapping 時返回 true (向後相容) |

---

## Performance Considerations (效能考量)

1. **HashSet Lookup**: O(1) 時間複雜度
2. **StringComparer.OrdinalIgnoreCase**: 避免字串分配
3. **早期過濾**: 在 Repository 層過濾,減少後續處理負擔
4. **批次處理**: Work Item 過濾在批次抓取後統一進行

---

## Migration & Compatibility (遷移與相容性)

### 既有資料相容性

- **WorkItemInfo.Team**: 新增 nullable 屬性,不影響既有序列化/反序列化
- **AzureDevOpsSettings.TeamMapping**: 新增屬性,預設為空清單,不影響既有配置

### 配置遷移

**既有配置 (相容)**:
```json
{
  "AzureDevOps": {
    "OrganizationUrl": "...",
    "ProjectName": "..."
  }
}
```

**新配置 (可選)**:
```json
{
  "AzureDevOps": {
    "OrganizationUrl": "...",
    "ProjectName": "...",
    "TeamMapping": [...]  // 可選,不加則不啟用過濾
  }
}
```

---

## Next Steps

1. ✅ 資料模型設計完成
2. ⏭️ 產生配置契約 (contracts/)
3. ⏭️ 撰寫快速開始指南 (quickstart.md)
4. ⏭️ 更新 agent context
