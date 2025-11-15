# Data Model: Repository-Based Export Format

**Feature**: Repository-Based Export Format
**Date**: 2025-11-15
**Status**: Design Complete

## Overview

此文件定義 Repository-Based Export Format 的完整資料模型,包含所有 DTO 類別的欄位、型別、驗證規則與關聯關係。所有 DTO 設計遵循 Clean Architecture 原則,位於 Application Layer,不依賴基礎設施層。

## DTO Hierarchy

```text
RepositoryBasedOutputDto (頂層)
  │
  ├─ StartDate: DateTime
  ├─ EndDate: DateTime
  └─ Repositories: List<RepositoryGroupDto>
       │
       ├─ RepositoryName: string
       ├─ Platform: string
       └─ PullRequests: List<RepositoryPullRequestDto>
            │
            ├─ WorkItem: PullRequestWorkItemDto? (nullable)
            │    │
            │    ├─ WorkItemId: int
            │    ├─ WorkItemTitle: string
            │    ├─ WorkItemTeam: string?
            │    ├─ WorkItemType: string
            │    └─ WorkItemUrl: string?
            │
            ├─ PullRequestTitle: string
            ├─ SourceBranch: string
            ├─ TargetBranch: string
            ├─ MergedAt: DateTime?
            ├─ AuthorUserId: string?
            ├─ AuthorDisplayName: string?
            └─ PullRequestUrl: string?
```

---

## Entity Definitions

### 1. RepositoryBasedOutputDto

**Purpose**: 頂層輸出 DTO,以 Repository 為主體組織 Pull Request 資料,用於 JSON 匯出。

**Type**: Record (immutable)

**Namespace**: `ReleaseSync.Application.DTOs`

**Fields**:

| 欄位名稱 | C# 型別 | JSON 名稱 | Required | Nullable | 說明 |
|----------|---------|-----------|----------|----------|------|
| StartDate | DateTime | startDate | ✅ Yes | ❌ No | 查詢開始日期 (UTC) |
| EndDate | DateTime | endDate | ✅ Yes | ❌ No | 查詢結束日期 (UTC) |
| Repositories | List<RepositoryGroupDto> | repositories | ✅ Yes | ❌ No | Repository 分組清單 |

**Business Rules**:

1. **日期範圍驗證**:
   - `StartDate` 必須早於或等於 `EndDate`
   - 兩者皆為 UTC 時間
   - 日期格式: ISO 8601 (`YYYY-MM-DDTHH:mm:ss.fffZ`)

2. **Repositories 集合**:
   - 不得為 `null` (使用 `required` 關鍵字強制)
   - 可為空陣列 (`[]`),表示無資料
   - 元素不得為 `null`

**Relationships**:
- **1:N** with `RepositoryGroupDto`: 一個輸出包含多個 Repository 分組

**Static Methods**:

```csharp
public static RepositoryBasedOutputDto FromSyncResult(SyncResultDto syncResult)
```

**Purpose**: 從 `SyncResultDto` 轉換為 Repository-based 格式

**Conversion Logic**:
1. 將 `syncResult.PullRequests` 按 `(RepositoryName, Platform)` 分組
2. 對每個分組,提取 Repository 短名稱
3. 對映 Pull Request 資料到簡化 DTO
4. 建立頂層 DTO 包含所有分組

**Example**:

```csharp
var output = RepositoryBasedOutputDto.FromSyncResult(syncResult);
// Input:  syncResult with 10 PRs across 3 repositories
// Output: RepositoryBasedOutputDto with 3 RepositoryGroupDto entries
```

---

### 2. RepositoryGroupDto

**Purpose**: 代表單一 Repository 及其關聯的所有 Pull Requests。

**Type**: Record (immutable)

**Namespace**: `ReleaseSync.Application.DTOs`

**Fields**:

| 欄位名稱 | C# 型別 | JSON 名稱 | Required | Nullable | 說明 |
|----------|---------|-----------|----------|----------|------|
| RepositoryName | string | repositoryName | ✅ Yes | ❌ No | Repository 簡短名稱 (已提取) |
| Platform | string | platform | ✅ Yes | ❌ No | 平台名稱 |
| PullRequests | List<RepositoryPullRequestDto> | pullRequests | ✅ Yes | ❌ No | PR 清單 |

**Business Rules**:

1. **RepositoryName**:
   - 不得為空字串
   - 已從完整路徑提取 (如 `owner/repo` → `repo`)
   - 若原始名稱無 `/`,則保留原值
   - 範例: `"backend"`, `"frontend"`, `"shared-library"`

2. **Platform**:
   - 必須為有效平台名稱: `"GitLab"`, `"BitBucket"`, `"AzureDevOps"`
   - 大小寫敏感,使用 PascalCase
   - 對應 `PullRequestDto.Platform` 的值

3. **PullRequests**:
   - 不得為 `null`
   - 可為空陣列 (雖然正常情況不應出現)
   - 元素不得為 `null`
   - 排序: 保持原始順序 (依 merged 時間)

**Relationships**:
- **N:1** with `RepositoryBasedOutputDto`: 多個 Repository 屬於一個輸出
- **1:N** with `RepositoryPullRequestDto`: 一個 Repository 包含多個 PR

**Grouping Key**:
- 複合鍵: `(RepositoryName, Platform)`
- 相同名稱但不同平台視為不同 Repository
- 範例: `("backend", "GitLab")` ≠ `("backend", "BitBucket")`

---

### 3. RepositoryPullRequestDto

**Purpose**: 簡化的 Pull Request DTO,包含 Work Item 關聯,用於 Repository 分組輸出。

**Type**: Record (immutable)

**Namespace**: `ReleaseSync.Application.DTOs`

**Fields**:

| 欄位名稱 | C# 型別 | JSON 名稱 | Required | Nullable | 說明 |
|----------|---------|-----------|----------|----------|------|
| WorkItem | PullRequestWorkItemDto? | workItem | ❌ No | ✅ Yes | 關聯的 Work Item (可為 null) |
| PullRequestTitle | string | pullRequestTitle | ✅ Yes | ❌ No | PR 標題 |
| SourceBranch | string | sourceBranch | ✅ Yes | ❌ No | 來源分支名稱 |
| TargetBranch | string | targetBranch | ✅ Yes | ❌ No | 目標分支名稱 |
| MergedAt | DateTime? | mergedAt | ❌ No | ✅ Yes | 合併時間 (UTC) |
| AuthorUserId | string? | authorUserId | ❌ No | ✅ Yes | 作者平台 ID |
| AuthorDisplayName | string? | authorDisplayName | ❌ No | ✅ Yes | 作者顯示名稱 |
| PullRequestUrl | string? | pullRequestUrl | ❌ No | ✅ Yes | PR URL |

**Business Rules**:

1. **WorkItem**:
   - **Nullable**: 當 PR 無關聯 Work Item 時為 `null`
   - JSON 輸出: `"workItem": null` (明確的 JSON null,非省略欄位)
   - 對映來源: `PullRequestDto.AssociatedWorkItem`

2. **PullRequestTitle**:
   - 不得為空字串
   - 最大長度: 無限制 (由平台決定)
   - 範例: `"Fix authentication bug"`, `"Add user profile feature"`

3. **SourceBranch / TargetBranch**:
   - 不得為空字串
   - 對映來源: `PullRequestDto.SourceBranch` / `TargetBranch`
   - 範例: `"feature/user-auth"`, `"main"`, `"develop"`

4. **MergedAt**:
   - **Nullable**: 未合併的 PR 為 `null`
   - UTC 時間,ISO 8601 格式
   - 必須晚於 PR 建立時間 (由來源資料保證)

5. **AuthorUserId**:
   - **Nullable**: 部分平台可能無此資訊
   - 格式因平台而異:
     - GitLab: 數字 ID (如 `"12345"`)
     - BitBucket: UUID (如 `"{abc-123}"`)
     - Azure DevOps: GUID 或 Email

6. **AuthorDisplayName**:
   - **Nullable**: 作者資訊缺失時為 `null`
   - 範例: `"John Doe"`, `"張三"`

7. **PullRequestUrl**:
   - **Nullable**: 部分情況可能無 URL
   - 完整 URL,可直接在瀏覽器開啟
   - 範例: `"https://gitlab.com/org/repo/-/merge_requests/123"`

**Relationships**:
- **N:1** with `RepositoryGroupDto`: 多個 PR 屬於一個 Repository
- **0..1:1** with `PullRequestWorkItemDto`: 一個 PR 可選地關聯一個 Work Item

**Mapping from PullRequestDto**:

```csharp
new RepositoryPullRequestDto
{
    WorkItem = pr.AssociatedWorkItem != null
        ? PullRequestWorkItemDto.FromWorkItemDto(pr.AssociatedWorkItem)
        : null, // 明確設為 null
    PullRequestTitle = pr.Title,
    SourceBranch = pr.SourceBranch,
    TargetBranch = pr.TargetBranch,
    MergedAt = pr.MergedAt,
    AuthorUserId = pr.AuthorUserId,
    AuthorDisplayName = pr.AuthorDisplayName,
    PullRequestUrl = pr.Url
}
```

---

### 4. PullRequestWorkItemDto

**Purpose**: Work Item 基本資訊,用於 PR 關聯,簡化版本 (不含 Parent Work Item 層級結構)。

**Type**: Record (immutable)

**Namespace**: `ReleaseSync.Application.DTOs`

**Fields**:

| 欄位名稱 | C# 型別 | JSON 名稱 | Required | Nullable | 說明 |
|----------|---------|-----------|----------|----------|------|
| WorkItemId | int | workItemId | ✅ Yes | ❌ No | Work Item ID |
| WorkItemTitle | string | workItemTitle | ✅ Yes | ❌ No | Work Item 標題 |
| WorkItemTeam | string? | workItemTeam | ❌ No | ✅ Yes | 所屬團隊 |
| WorkItemType | string | workItemType | ✅ Yes | ❌ No | Work Item 類型 |
| WorkItemUrl | string? | workItemUrl | ❌ No | ✅ Yes | Work Item URL |

**Business Rules**:

1. **WorkItemId**:
   - 必須為正整數 (> 0)
   - 對應 Azure DevOps Work Item ID
   - 範例: `12345`, `67890`

2. **WorkItemTitle**:
   - 不得為空字串
   - 最大長度: 無限制 (由 Azure DevOps 決定)
   - 範例: `"Implement user authentication"`, `"修正登入錯誤"`

3. **WorkItemTeam**:
   - **Nullable**: 無團隊資訊時為 `null`
   - 已經過 TeamMapping 轉換 (由 `WorkItemDto.Team` 提供)
   - 範例: `"Backend Team"`, `"Frontend Team"`

4. **WorkItemType**:
   - 不得為空字串
   - 常見類型: `"Task"`, `"Bug"`, `"User Story"`, `"Feature"`
   - 大小寫保持原始值

5. **WorkItemUrl**:
   - **Nullable**: 部分情況可能無 URL
   - 完整 URL,可直接在瀏覽器開啟
   - 範例: `"https://dev.azure.com/org/project/_workitems/edit/12345"`

**Relationships**:
- **1:0..1** with `RepositoryPullRequestDto`: 一個 Work Item 可被多個 PR 關聯 (但在此 DTO 不追蹤反向關聯)

**Mapping from WorkItemDto**:

```csharp
public static PullRequestWorkItemDto FromWorkItemDto(WorkItemDto workItem)
{
    return new PullRequestWorkItemDto
    {
        WorkItemId = workItem.Id,
        WorkItemTitle = workItem.Title,
        WorkItemTeam = workItem.Team,
        WorkItemType = workItem.Type,
        WorkItemUrl = workItem.Url
    };
}
```

**與 WorkItemDto 的差異**:

| 欄位 | WorkItemDto | PullRequestWorkItemDto | 差異原因 |
|------|-------------|------------------------|----------|
| Id | ✅ | ✅ (as WorkItemId) | 重新命名以明確語意 |
| Title | ✅ | ✅ (as WorkItemTitle) | 重新命名以明確語意 |
| Type | ✅ | ✅ (as WorkItemType) | 重新命名以明確語意 |
| State | ✅ | ❌ | 簡化:不需要顯示 Work Item 狀態 |
| Url | ✅ | ✅ (as WorkItemUrl) | 重新命名以明確語意 |
| AssignedTo | ✅ | ❌ | 簡化:不需要顯示指派資訊 |
| Team | ✅ | ✅ (as WorkItemTeam) | 保留:團隊資訊對分析有價值 |
| ParentWorkItem | ✅ | ❌ | 簡化:避免巢狀結構複雜度 |

---

## Data Flow

### 轉換流程圖

```text
SyncResultDto (Application Layer)
    │
    ├─ StartDate ────────────────┐
    ├─ EndDate ──────────────────┤
    │                            ▼
    └─ PullRequests: List<PullRequestDto>
         │                   RepositoryBasedOutputDto
         │                      │
         ▼                      ├─ StartDate: DateTime
    GroupBy                     ├─ EndDate: DateTime
    (RepositoryName,            │
     Platform)                  └─ Repositories: List<RepositoryGroupDto>
         │                           │
         │                           ├─ [Group 1] RepositoryGroupDto
         ▼                           │    ├─ RepositoryName: "backend"
    For each group:                 │    ├─ Platform: "GitLab"
      │                             │    └─ PullRequests: [...]
      ├─ Extract repo name          │
      ├─ Map PRs to DTO             ├─ [Group 2] RepositoryGroupDto
      └─ Create RepositoryGroupDto  │    ├─ RepositoryName: "frontend"
                                    │    ├─ Platform: "BitBucket"
                                    │    └─ PullRequests: [...]
                                    │
                                    └─ [Group 3] ...
```

### 轉換虛擬碼

```csharp
public static RepositoryBasedOutputDto FromSyncResult(SyncResultDto syncResult)
{
    // Step 1: 依 Repository 與 Platform 分組
    var repositoryGroups = syncResult.PullRequests
        .GroupBy(pr => new { pr.RepositoryName, pr.Platform });

    // Step 2: 對每個分組建立 RepositoryGroupDto
    var repositories = repositoryGroups.Select(group =>
    {
        // Step 2.1: 提取 Repository 短名稱
        var shortName = ExtractRepositoryName(group.Key.RepositoryName);

        // Step 2.2: 對映 Pull Requests
        var pullRequests = group.Select(pr => new RepositoryPullRequestDto
        {
            WorkItem = pr.AssociatedWorkItem != null
                ? PullRequestWorkItemDto.FromWorkItemDto(pr.AssociatedWorkItem)
                : null,
            PullRequestTitle = pr.Title,
            SourceBranch = pr.SourceBranch,
            TargetBranch = pr.TargetBranch,
            MergedAt = pr.MergedAt,
            AuthorUserId = pr.AuthorUserId,
            AuthorDisplayName = pr.AuthorDisplayName,
            PullRequestUrl = pr.Url
        }).ToList();

        // Step 2.3: 建立 Repository 分組
        return new RepositoryGroupDto
        {
            RepositoryName = shortName,
            Platform = group.Key.Platform,
            PullRequests = pullRequests
        };
    }).ToList();

    // Step 3: 建立頂層 DTO
    return new RepositoryBasedOutputDto
    {
        StartDate = syncResult.StartDate,
        EndDate = syncResult.EndDate,
        Repositories = repositories
    };
}

private static string ExtractRepositoryName(string repositoryName)
{
    // 從完整路徑提取短名稱: "owner/repo" -> "repo"
    var parts = repositoryName.Split('/');
    return parts[^1]; // Index from End (C# 9.0)
}
```

---

## JSON Schema Alignment

### Schema 對應表

| DTO 類別 | JSON Schema `$defs` 鍵 | Schema 型別 |
|----------|------------------------|-------------|
| RepositoryBasedOutputDto | (root object) | `object` |
| RepositoryGroupDto | `RepositoryGroup` | `object` |
| RepositoryPullRequestDto | `PullRequest` | `object` |
| PullRequestWorkItemDto | `WorkItem` | `object` |

### 欄位型別對應

| C# 型別 | JSON Schema 型別 | 範例 JSON 值 |
|---------|------------------|--------------|
| `DateTime` | `string` (format: `date-time`) | `"2025-01-15T10:30:00Z"` |
| `DateTime?` | `string` or `null` | `"2025-01-15T10:30:00Z"` or `null` |
| `int` | `integer` | `12345` |
| `string` | `string` | `"example"` |
| `string?` | `["string", "null"]` | `"example"` or `null` |
| `List<T>` | `array` (items: `$ref`) | `[{...}, {...}]` |
| `PullRequestWorkItemDto?` | `oneOf: [WorkItem, null]` | `{...}` or `null` |

---

## Validation Rules Summary

### Compile-Time Validation (C#)

| 規則 | 機制 | 範例 |
|------|------|------|
| Required 欄位不得為 null | `required` 關鍵字 | `public required string RepositoryName { get; init; }` |
| Nullable 欄位明確標示 | `?` 修飾符 | `public DateTime? MergedAt { get; init; }` |
| 不可變性 | `record` + `init` | `public record RepositoryBasedOutputDto { ... }` |

### Runtime Validation (Business Logic)

| 規則 | 驗證時機 | 驗證方式 |
|------|----------|----------|
| StartDate ≤ EndDate | DTO 建立時 | 由 `SyncResultDto` 保證 (來源資料已驗證) |
| WorkItemId > 0 | DTO 建立時 | 由 `WorkItemDto` 保證 (來源資料已驗證) |
| RepositoryName 非空 | DTO 建立時 | 由 `PullRequestDto` 保證 (來源資料已驗證) |
| Platform 有效性 | DTO 建立時 | 由 `PullRequestDto` 保證 (來源資料已驗證) |

**設計決策**: 不在 DTO 層重複驗證,信任來源資料 (`SyncResultDto`) 已完成驗證,符合 DRY 原則。

---

## Testing Strategy

### 單元測試矩陣

| 測試案例 | 測試目標 | 輸入 | 預期輸出 |
|----------|----------|------|----------|
| `FromSyncResult_EmptyData_ReturnsEmptyRepositories` | 空資料處理 | 0 PRs | `Repositories: []` |
| `FromSyncResult_SingleRepository_GroupsCorrectly` | 單一 Repository | 3 PRs, 1 repo | 1 group with 3 PRs |
| `FromSyncResult_MultipleRepositories_GroupsByNameAndPlatform` | 多 Repository 分組 | 6 PRs, 2 repos × 2 platforms | 4 groups |
| `ExtractRepositoryName_WithSlash_ReturnsLastPart` | 名稱提取 (有 /) | `"owner/repo"` | `"repo"` |
| `ExtractRepositoryName_WithoutSlash_ReturnsOriginal` | 名稱提取 (無 /) | `"standalone"` | `"standalone"` |
| `ExtractRepositoryName_MultipleSlashes_ReturnsLastPart` | 名稱提取 (多 /) | `"org/team/project"` | `"project"` |
| `FromSyncResult_WorkItemNull_SetsWorkItemToNull` | Work Item null 處理 | PR without Work Item | `workItem: null` |
| `FromSyncResult_WorkItemExists_MapsCorrectly` | Work Item 對映 | PR with Work Item | `workItem: { workItemId: 123, ... }` |
| `FromSyncResult_PreservesDateRange` | 日期保留 | `StartDate`, `EndDate` | 相同日期 |
| `FromSyncResult_LargeDataset_PerformsWithin5Seconds` | 效能測試 | 2000 PRs | < 5 秒 |

### 測試資料建立器

```csharp
public class TestDataBuilder
{
    public static SyncResultDto CreateSyncResult(
        int prCount = 5,
        int repoCount = 2,
        bool includeWorkItem = true)
    {
        // ... 建立測試資料
    }

    public static PullRequestDto CreatePullRequest(
        string repoName = "test-repo",
        string platform = "GitLab",
        WorkItemDto? workItem = null)
    {
        // ... 建立測試資料
    }
}
```

---

## Performance Considerations

### 時間複雜度分析

| 操作 | 複雜度 | 說明 |
|------|--------|------|
| `GroupBy` | O(n) | n = PR 總數 |
| `Select` (outer) | O(g) | g = group 數量 (≤ n) |
| `Select` (inner, PR mapping) | O(m) | m = group 內 PR 數量 |
| `ExtractRepositoryName` | O(k) | k = 字串長度 (通常 < 50) |
| **Total** | **O(n)** | 線性時間複雜度 |

### 空間複雜度分析

| 資料結構 | 空間 | 說明 |
|----------|------|------|
| Input (`SyncResultDto.PullRequests`) | O(n) | 原始 PR 清單 |
| Output (`RepositoryBasedOutputDto`) | O(n) | 分組後 PR 清單 (總量相同) |
| Temporary (GroupBy key) | O(g) | g = group 數量 (≤ 100) |
| **Total** | **O(n)** | 線性空間複雜度 |

### 記憶體使用預估

| 項目 | 單筆大小 | 數量 | 總計 |
|------|----------|------|------|
| `RepositoryPullRequestDto` | ~500 bytes | 2000 | ~1 MB |
| `PullRequestWorkItemDto` | ~200 bytes | 1000 (50% 有 Work Item) | ~0.2 MB |
| `RepositoryGroupDto` | ~100 bytes | 100 | ~0.01 MB |
| JSON string (序列化後) | ~1.5× object size | - | ~1.8 MB |
| **Total (peak)** | - | - | **~3 MB** |

**結論**: 記憶體使用極低,無需特殊最佳化。

---

## Future Enhancements (Out of Scope)

以下為潛在的擴展方向,目前**不實作**:

1. **統計資訊**:
   - ✨ `TotalPullRequests`: Repository 的 PR 總數
   - ✨ `UniqueAuthors`: 不重複作者數量
   - ✨ `MergedCount`: 已合併 PR 數量
   - **理由**: 使用者未明確要求,可在 Google Sheets 中計算

2. **排序選項**:
   - ✨ 依 Repository 名稱排序
   - ✨ 依 PR 數量排序
   - ✨ 依最後合併時間排序
   - **理由**: 可在應用層或 Google Sheets 中處理

3. **過濾功能**:
   - ✨ 僅匯出特定 Platform
   - ✨ 僅匯出已合併 PR
   - ✨ 僅匯出特定日期區間的 PR
   - **理由**: 應在查詢階段處理,非匯出階段

4. **巢狀 Work Item 層級**:
   - ✨ 保留 `ParentWorkItem` 層級結構
   - **理由**: 增加複雜度,與扁平化目標衝突

---

## Approval & Sign-off

- ✅ **資料模型設計完成**: 4 個 DTO 類別定義完整
- ✅ **欄位定義明確**: 所有欄位型別、nullable、required 皆已標註
- ✅ **業務規則清楚**: 驗證規則與資料約束皆已文件化
- ✅ **轉換流程明確**: 提供虛擬碼與流程圖
- ✅ **效能分析完成**: 時間/空間複雜度皆為 O(n),符合目標
- ✅ **測試策略完整**: 10 個測試案例涵蓋所有邏輯分支

**簽核日期**: 2025-11-15
**下一步**: 建立 JSON Schema 契約定義 (`contracts/repository-based-output-schema.json`)
