# Data Model: PR/MR 變更資訊聚合工具

**Feature**: 002-pr-aggregation-tool
**Date**: 2025-10-18
**Phase**: Phase 1 - Design Artifacts

本文件定義所有 Domain 實體與值物件的結構,包含驗證規則、不變條件與狀態轉換。

---

## Domain 模型架構

### Bounded Contexts

```
┌─────────────────────────────────────────────────────────────────┐
│ Version Control Context                                         │
│ - PullRequestInfo (Entity)                                      │
│ - BranchName (Value Object)                                     │
│ - DateRange (Value Object)                                      │
└─────────────────────────────────────────────────────────────────┘
           │
           │ 參照
           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Work Item Context                                               │
│ - WorkItemInfo (Entity)                                         │
│ - WorkItemId (Value Object)                                     │
└─────────────────────────────────────────────────────────────────┘
           │
           │ 聚合
           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Integration Context                                             │
│ - SyncResult (Aggregate Root)                                   │
│ - PlatformSyncStatus (Value Object)                             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Value Objects (值物件)

### 1. DateRange (時間範圍)

**職責**: 表示 PR/MR 查詢的時間範圍,確保起始日期不晚於結束日期。

```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// 時間範圍值物件
/// </summary>
/// <param name="StartDate">起始日期 (包含)</param>
/// <param name="EndDate">結束日期 (包含)</param>
public record DateRange(DateTime StartDate, DateTime EndDate)
{
    /// <summary>
    /// 起始日期 (包含)
    /// </summary>
    public DateTime StartDate { get; init; } = StartDate;

    /// <summary>
    /// 結束日期 (包含)
    /// </summary>
    public DateTime EndDate { get; init; } = EndDate;

    /// <summary>
    /// 驗證時間範圍是否有效
    /// </summary>
    public void Validate()
    {
        if (StartDate > EndDate)
        {
            throw new ArgumentException(
                $"起始日期 ({StartDate:yyyy-MM-dd}) 不能晚於結束日期 ({EndDate:yyyy-MM-dd})"
            );
        }
    }

    /// <summary>
    /// 檢查指定日期是否在此範圍內
    /// </summary>
    public bool Contains(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    /// <summary>
    /// 建立「最近 N 天」的時間範圍
    /// </summary>
    public static DateRange LastDays(int days)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);
        return new DateRange(startDate, endDate);
    }
}
```

**不變條件** (Invariants):
- `StartDate <= EndDate`

**驗證規則**:
- 建構時呼叫 `Validate()` 確保不變條件

---

### 2. BranchName (分支名稱)

**職責**: 表示 Git Branch 名稱,提供驗證與格式化方法。

```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// 分支名稱值物件
/// </summary>
/// <param name="Value">分支名稱</param>
public record BranchName(string Value)
{
    /// <summary>
    /// 分支名稱
    /// </summary>
    public string Value { get; init; } = Value ?? throw new ArgumentNullException(nameof(Value));

    /// <summary>
    /// 驗證分支名稱是否有效
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            throw new ArgumentException("分支名稱不能為空白");
        }

        // Git 分支命名規則驗證 (簡化版)
        if (Value.Contains("..") || Value.StartsWith('/') || Value.EndsWith('/'))
        {
            throw new ArgumentException($"無效的分支名稱格式: {Value}");
        }
    }

    /// <summary>
    /// 取得短名稱 (移除 refs/heads/ 前綴)
    /// </summary>
    public string ShortName =>
        Value.StartsWith("refs/heads/")
            ? Value.Substring("refs/heads/".Length)
            : Value;

    public override string ToString() => Value;
}
```

**不變條件**:
- `Value` 不為 null 或空白
- 符合基本 Git 分支命名規則

---

### 3. WorkItemId (Work Item 識別碼)

**職責**: 表示 Azure DevOps Work Item 的唯一識別碼。

```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// Work Item 識別碼值物件
/// </summary>
/// <param name="Value">Work Item ID (正整數)</param>
public record WorkItemId(int Value)
{
    /// <summary>
    /// Work Item ID (正整數)
    /// </summary>
    public int Value { get; init; } = Value;

    /// <summary>
    /// 驗證 Work Item ID 是否有效
    /// </summary>
    public void Validate()
    {
        if (Value <= 0)
        {
            throw new ArgumentException($"Work Item ID 必須為正整數: {Value}");
        }
    }

    /// <summary>
    /// 從字串解析 Work Item ID
    /// </summary>
    public static WorkItemId Parse(string value)
    {
        if (!int.TryParse(value, out var id))
        {
            throw new FormatException($"無法將 '{value}' 解析為 Work Item ID");
        }

        return new WorkItemId(id);
    }

    /// <summary>
    /// 嘗試從字串解析 Work Item ID
    /// </summary>
    public static bool TryParse(string value, out WorkItemId workItemId)
    {
        workItemId = null;
        if (!int.TryParse(value, out var id) || id <= 0)
        {
            return false;
        }

        workItemId = new WorkItemId(id);
        return true;
    }

    public override string ToString() => Value.ToString();
}
```

**不變條件**:
- `Value > 0` (必須為正整數)

---

### 4. PlatformSyncStatus (平台同步狀態)

**職責**: 記錄單一平台的同步執行狀態與結果。

```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// 平台同步狀態值物件
/// </summary>
public record PlatformSyncStatus
{
    /// <summary>
    /// 平台名稱 (例如: GitLab, BitBucket, AzureDevOps)
    /// </summary>
    public required string PlatformName { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 成功抓取的 PR/MR 數量
    /// </summary>
    public int PullRequestCount { get; init; }

    /// <summary>
    /// 錯誤訊息 (若失敗)
    /// </summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// 執行時間 (毫秒)
    /// </summary>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>
    /// 建立成功狀態
    /// </summary>
    public static PlatformSyncStatus Success(string platformName, int count, long elapsedMs)
    {
        return new PlatformSyncStatus
        {
            PlatformName = platformName,
            IsSuccess = true,
            PullRequestCount = count,
            ElapsedMilliseconds = elapsedMs
        };
    }

    /// <summary>
    /// 建立失敗狀態
    /// </summary>
    public static PlatformSyncStatus Failure(string platformName, string errorMessage, long elapsedMs)
    {
        return new PlatformSyncStatus
        {
            PlatformName = platformName,
            IsSuccess = false,
            PullRequestCount = 0,
            ErrorMessage = errorMessage,
            ElapsedMilliseconds = elapsedMs
        };
    }
}
```

---

## Entities (實體)

### 5. PullRequestInfo (PR/MR 資訊實體)

**職責**: 表示來自版控平台的 Pull Request 或 Merge Request,具有唯一識別碼與完整的變更資訊。

```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// Pull Request / Merge Request 資訊實體
/// </summary>
public class PullRequestInfo
{
    /// <summary>
    /// 平台類型 (GitLab, BitBucket)
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// PR/MR 在平台上的唯一識別碼
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// PR/MR 編號 (通常為數字)
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// 來源分支名稱
    /// </summary>
    public required BranchName SourceBranch { get; init; }

    /// <summary>
    /// 目標分支名稱
    /// </summary>
    public required BranchName TargetBranch { get; init; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 合併時間 (UTC),若未合併則為 null
    /// </summary>
    public DateTime? MergedAt { get; init; }

    /// <summary>
    /// 狀態 (Open, Merged, Declined, Closed)
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// 作者使用者名稱
    /// </summary>
    public required string AuthorUsername { get; init; }

    /// <summary>
    /// 作者顯示名稱
    /// </summary>
    public string AuthorDisplayName { get; init; }

    /// <summary>
    /// Repository 名稱 (例如: owner/repo)
    /// </summary>
    public required string RepositoryName { get; init; }

    /// <summary>
    /// 關聯的 Work Item (若有從 Branch 名稱解析出)
    /// </summary>
    public WorkItemInfo AssociatedWorkItem { get; set; }

    /// <summary>
    /// PR/MR 的 URL
    /// </summary>
    public string Url { get; init; }

    /// <summary>
    /// 驗證實體是否有效
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Platform))
            throw new ArgumentException("Platform 不能為空");

        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentException("Id 不能為空");

        if (Number <= 0)
            throw new ArgumentException($"Number 必須為正整數: {Number}");

        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title 不能為空");

        SourceBranch?.Validate();
        TargetBranch?.Validate();

        if (CreatedAt > DateTime.UtcNow)
            throw new ArgumentException($"CreatedAt 不能為未來時間: {CreatedAt}");

        if (MergedAt.HasValue && MergedAt.Value < CreatedAt)
            throw new ArgumentException("MergedAt 不能早於 CreatedAt");
    }

    /// <summary>
    /// 是否已合併
    /// </summary>
    public bool IsMerged => State.Equals("Merged", StringComparison.OrdinalIgnoreCase) && MergedAt.HasValue;

    /// <summary>
    /// 計算 PR/MR 存活時間 (從建立到合併,或建立到現在)
    /// </summary>
    public TimeSpan GetLifetime()
    {
        var endTime = MergedAt ?? DateTime.UtcNow;
        return endTime - CreatedAt;
    }
}
```

**識別碼**: `Platform + Id` 組合作為唯一識別 (跨平台的 PR/MR 可能有相同 Number)

**不變條件**:
- `Platform`, `Id`, `Title`, `RepositoryName`, `AuthorUsername` 不為空
- `Number > 0`
- `CreatedAt <= MergedAt` (若 MergedAt 有值)
- `CreatedAt <= DateTime.UtcNow`

---

### 6. WorkItemInfo (Work Item 資訊實體)

**職責**: 表示 Azure DevOps Work Item,包含 ID、類型、狀態與 Parent Work Item 參照。

```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// Work Item 資訊實體
/// </summary>
public class WorkItemInfo
{
    /// <summary>
    /// Work Item 識別碼
    /// </summary>
    public required WorkItemId Id { get; init; }

    /// <summary>
    /// Work Item 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Work Item 類型 (例如: User Story, Bug, Task, Epic)
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Work Item 狀態 (例如: New, Active, Resolved, Closed)
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Parent Work Item (若存在)
    /// </summary>
    public WorkItemInfo ParentWorkItem { get; set; }

    /// <summary>
    /// Work Item URL
    /// </summary>
    public string Url { get; init; }

    /// <summary>
    /// 指派給誰 (Assigned To)
    /// </summary>
    public string AssignedTo { get; init; }

    /// <summary>
    /// 建立時間 (UTC)
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最後更新時間 (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// 驗證實體是否有效
    /// </summary>
    public void Validate()
    {
        Id?.Validate();

        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title 不能為空");

        if (string.IsNullOrWhiteSpace(Type))
            throw new ArgumentException("Type 不能為空");

        if (string.IsNullOrWhiteSpace(State))
            throw new ArgumentException("State 不能為空");

        if (CreatedAt > DateTime.UtcNow)
            throw new ArgumentException($"CreatedAt 不能為未來時間: {CreatedAt}");

        if (UpdatedAt < CreatedAt)
            throw new ArgumentException("UpdatedAt 不能早於 CreatedAt");
    }

    /// <summary>
    /// 檢查是否有 Parent Work Item
    /// </summary>
    public bool HasParent => ParentWorkItem != null;

    /// <summary>
    /// 取得完整的 Work Item 階層路徑 (Parent > Child > GrandChild)
    /// </summary>
    public string GetHierarchyPath()
    {
        if (!HasParent)
            return $"{Id} - {Title}";

        return $"{ParentWorkItem.GetHierarchyPath()} > {Id} - {Title}";
    }
}
```

**識別碼**: `WorkItemId.Value`

**不變條件**:
- `Id` 有效 (正整數)
- `Title`, `Type`, `State` 不為空
- `CreatedAt <= UpdatedAt`
- `CreatedAt <= DateTime.UtcNow`

---

## Aggregate Root (聚合根)

### 7. SyncResult (同步結果聚合根)

**職責**: 聚合所有平台的同步結果,提供整體執行摘要與匯出功能。

```csharp
namespace ReleaseSync.Domain.Models;

/// <summary>
/// 同步結果聚合根
/// </summary>
public class SyncResult
{
    private readonly List<PullRequestInfo> _pullRequests = new();
    private readonly List<PlatformSyncStatus> _platformStatuses = new();

    /// <summary>
    /// 同步執行的時間範圍
    /// </summary>
    public required DateRange SyncDateRange { get; init; }

    /// <summary>
    /// 同步執行的開始時間 (UTC)
    /// </summary>
    public DateTime SyncStartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 同步執行的結束時間 (UTC)
    /// </summary>
    public DateTime? SyncCompletedAt { get; private set; }

    /// <summary>
    /// 所有抓取的 Pull Requests / Merge Requests
    /// </summary>
    public IReadOnlyList<PullRequestInfo> PullRequests => _pullRequests.AsReadOnly();

    /// <summary>
    /// 各平台的同步狀態
    /// </summary>
    public IReadOnlyList<PlatformSyncStatus> PlatformStatuses => _platformStatuses.AsReadOnly();

    /// <summary>
    /// 是否完全成功 (所有平台皆成功)
    /// </summary>
    public bool IsFullySuccessful =>
        _platformStatuses.Any() && _platformStatuses.All(s => s.IsSuccess);

    /// <summary>
    /// 是否部分成功 (至少一個平台成功)
    /// </summary>
    public bool IsPartiallySuccessful =>
        _platformStatuses.Any(s => s.IsSuccess);

    /// <summary>
    /// 總計抓取的 PR/MR 數量
    /// </summary>
    public int TotalPullRequestCount => _pullRequests.Count;

    /// <summary>
    /// 關聯到 Work Item 的 PR/MR 數量
    /// </summary>
    public int LinkedWorkItemCount =>
        _pullRequests.Count(pr => pr.AssociatedWorkItem != null);

    /// <summary>
    /// 新增 Pull Request
    /// </summary>
    public void AddPullRequest(PullRequestInfo pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);
        pullRequest.Validate();
        _pullRequests.Add(pullRequest);
    }

    /// <summary>
    /// 批次新增 Pull Requests
    /// </summary>
    public void AddPullRequests(IEnumerable<PullRequestInfo> pullRequests)
    {
        foreach (var pr in pullRequests)
        {
            AddPullRequest(pr);
        }
    }

    /// <summary>
    /// 記錄平台同步狀態
    /// </summary>
    public void RecordPlatformStatus(PlatformSyncStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _platformStatuses.Add(status);
    }

    /// <summary>
    /// 標記同步完成
    /// </summary>
    public void MarkAsCompleted()
    {
        SyncCompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 取得執行摘要
    /// </summary>
    public string GetSummary()
    {
        var successCount = _platformStatuses.Count(s => s.IsSuccess);
        var totalPlatforms = _platformStatuses.Count;
        var duration = SyncCompletedAt.HasValue
            ? (SyncCompletedAt.Value - SyncStartedAt).TotalSeconds
            : 0;

        return $"同步完成: {successCount}/{totalPlatforms} 平台成功, " +
               $"共抓取 {TotalPullRequestCount} 筆 PR/MR " +
               $"({LinkedWorkItemCount} 筆關聯到 Work Item), " +
               $"耗時 {duration:F2} 秒";
    }

    /// <summary>
    /// 取得失敗的平台清單
    /// </summary>
    public IEnumerable<PlatformSyncStatus> GetFailedPlatforms()
    {
        return _platformStatuses.Where(s => !s.IsSuccess);
    }
}
```

**聚合邊界**: 包含所有 `PullRequestInfo` 與 `PlatformSyncStatus`,對外僅透過 `SyncResult` 操作

**不變條件**:
- `SyncDateRange` 不為 null
- 所有 `PullRequestInfo` 必須通過驗證
- `SyncCompletedAt >= SyncStartedAt` (若已設定)

---

## 領域服務介面

### IWorkItemIdParser (Work Item ID 解析服務)

**職責**: 從 Branch 名稱解析 Work Item ID。

```csharp
namespace ReleaseSync.Domain.Services;

/// <summary>
/// Work Item ID 解析服務介面
/// </summary>
public interface IWorkItemIdParser
{
    /// <summary>
    /// 從 Branch 名稱解析 Work Item ID
    /// </summary>
    /// <param name="branchName">Branch 名稱</param>
    /// <returns>解析出的 Work Item ID,若無法解析則回傳 null</returns>
    WorkItemId ParseWorkItemId(BranchName branchName);

    /// <summary>
    /// 嘗試從 Branch 名稱解析 Work Item ID
    /// </summary>
    /// <param name="branchName">Branch 名稱</param>
    /// <param name="workItemId">解析出的 Work Item ID</param>
    /// <returns>是否成功解析</returns>
    bool TryParseWorkItemId(BranchName branchName, out WorkItemId workItemId);
}
```

---

## Repository 介面

### IPullRequestRepository (Pull Request Repository)

**職責**: 抽象各平台的 Pull Request 資料存取。

```csharp
namespace ReleaseSync.Domain.Repositories;

/// <summary>
/// Pull Request Repository 介面
/// </summary>
public interface IPullRequestRepository
{
    /// <summary>
    /// 查詢指定時間範圍內的 Pull Requests
    /// </summary>
    /// <param name="projectName">專案名稱 (例如: owner/repo)</param>
    /// <param name="dateRange">時間範圍</param>
    /// <param name="targetBranches">目標分支清單 (若為空則查詢所有分支)</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Pull Request 清單</returns>
    Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
        string projectName,
        DateRange dateRange,
        IEnumerable<string> targetBranches = null,
        CancellationToken cancellationToken = default
    );
}
```

### IWorkItemRepository (Work Item Repository)

**職責**: 抽象 Azure DevOps Work Item 資料存取。

```csharp
namespace ReleaseSync.Domain.Repositories;

/// <summary>
/// Work Item Repository 介面
/// </summary>
public interface IWorkItemRepository
{
    /// <summary>
    /// 根據 Work Item ID 查詢 Work Item
    /// </summary>
    /// <param name="workItemId">Work Item 識別碼</param>
    /// <param name="includeParent">是否包含 Parent Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊,若不存在則回傳 null</returns>
    Task<WorkItemInfo> GetWorkItemAsync(
        WorkItemId workItemId,
        bool includeParent = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 批次查詢多個 Work Items
    /// </summary>
    /// <param name="workItemIds">Work Item 識別碼清單</param>
    /// <param name="includeParent">是否包含 Parent Work Item</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Work Item 資訊清單</returns>
    Task<IEnumerable<WorkItemInfo>> GetWorkItemsAsync(
        IEnumerable<WorkItemId> workItemIds,
        bool includeParent = true,
        CancellationToken cancellationToken = default
    );
}
```

---

## 狀態轉換圖

### PullRequestInfo 狀態轉換

```
[Open]  ──merge──>  [Merged]
  │
  ├──decline──>  [Declined]
  │
  └──close──>  [Closed]
```

**說明**:
- `Open`: 新建立,尚未合併
- `Merged`: 已合併至目標分支
- `Declined`: 被拒絕 (BitBucket 用語)
- `Closed`: 關閉但未合併 (GitLab 用語)

---

## 驗證規則摘要

| Domain Model | 驗證規則 |
|--------------|----------|
| **DateRange** | `StartDate <= EndDate` |
| **BranchName** | 不為空白,符合 Git 命名規則 |
| **WorkItemId** | 必須為正整數 (`Value > 0`) |
| **PullRequestInfo** | Platform, Id, Title 不為空;<br/>`Number > 0`;<br/>`CreatedAt <= MergedAt`;<br/>`CreatedAt <= Now` |
| **WorkItemInfo** | Id 有效;<br/>Title, Type, State 不為空;<br/>`CreatedAt <= UpdatedAt`;<br/>`CreatedAt <= Now` |
| **SyncResult** | SyncDateRange 不為 null;<br/>所有 PullRequestInfo 必須通過驗證 |

---

## 下一步

- [x] 定義 Domain Models 結構
- [ ] 產生 API Contracts (contracts/ 目錄)
- [ ] 產生 quickstart.md
- [ ] 更新 agent context (CLAUDE.md)

**Phase 1 In Progress** 🚧
