# Data Model: Release Branch 差異比對功能

**Feature Branch**: `001-release-branch-diff`
**Date**: 2026-01-20

## 概述

本文件定義 Release Branch 差異比對功能所需的資料模型，包括值物件、列舉、配置類別及其關係。

---

## 1. Domain Layer 模型

### 1.1 FetchMode 列舉

```csharp
/// <summary>
/// 定義 PR/MR 資料的抓取模式
/// </summary>
public enum FetchMode
{
    /// <summary>
    /// 使用時間範圍抓取（預設模式，向後相容）
    /// </summary>
    DateRange = 0,

    /// <summary>
    /// 使用 Release Branch 比對抓取
    /// </summary>
    ReleaseBranch = 1
}
```

**設計決策**:
- `DateRange` 為預設值（0），確保向後相容
- 使用明確的數值以便於 JSON 序列化

### 1.2 ReleaseBranchName 值物件

```csharp
/// <summary>
/// 代表 Release Branch 名稱的值物件
/// 封裝命名格式驗證與日期解析邏輯
/// </summary>
public sealed record ReleaseBranchName : IComparable<ReleaseBranchName>
{
    // 欄位
    private static readonly Regex Pattern = new(@"^release/(\d{8})$", RegexOptions.Compiled);

    // 屬性
    public string Value { get; }
    public DateOnly Date { get; }
    public string ShortName => Value.Replace("refs/heads/", "");

    // 建構函式
    private ReleaseBranchName(string value, DateOnly date);

    // 靜態方法
    public static ReleaseBranchName Parse(string value);
    public static bool TryParse(string? value, out ReleaseBranchName? result);
    public static ReleaseBranchName FromDate(DateOnly date);

    // 比較方法
    public int CompareTo(ReleaseBranchName? other);
    public static bool operator <(ReleaseBranchName left, ReleaseBranchName right);
    public static bool operator >(ReleaseBranchName left, ReleaseBranchName right);
    public static bool operator <=(ReleaseBranchName left, ReleaseBranchName right);
    public static bool operator >=(ReleaseBranchName left, ReleaseBranchName right);

    // 隱式轉換
    public static implicit operator string(ReleaseBranchName branchName);
}
```

**驗證規則**:
- 格式必須為 `release/yyyyMMdd`
- 日期必須是有效的日期（如 20260132 無效）
- 大小寫敏感

**使用範例**:
```csharp
var branch = ReleaseBranchName.Parse("release/20260120");
Console.WriteLine(branch.Date);  // 2026-01-20
Console.WriteLine(branch.Value); // release/20260120

// 比較
var older = ReleaseBranchName.Parse("release/20260113");
Console.WriteLine(branch > older); // true
```

---

## 2. Infrastructure Layer 配置模型

### 2.1 SyncOptionsSettings

```csharp
/// <summary>
/// 全域同步選項設定
/// </summary>
public class SyncOptionsSettings
{
    /// <summary>
    /// 預設的 Release Branch 名稱
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 預設的開始日期
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 預設的結束日期
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 預設的抓取模式
    /// </summary>
    public FetchMode DefaultFetchMode { get; init; } = FetchMode.DateRange;
}
```

### 2.2 修改後的 GitLabProjectSettings

```csharp
/// <summary>
/// GitLab 專案設定
/// </summary>
public class GitLabProjectSettings
{
    /// <summary>
    /// 專案路徑（格式：group/project）
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// 目標分支（單一值）
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 抓取模式（覆寫全域設定）
    /// </summary>
    public FetchMode? FetchMode { get; init; }

    /// <summary>
    /// Release Branch（覆寫全域設定）
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 開始日期（覆寫全域設定）
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 結束日期（覆寫全域設定）
    /// </summary>
    public DateTime? EndDate { get; init; }

    // 已棄用，保留向後相容
    [Obsolete("請使用 TargetBranch（單一值）")]
    public List<string>? TargetBranches { get; init; }
}
```

### 2.3 修改後的 BitBucketProjectSettings

```csharp
/// <summary>
/// BitBucket 專案設定
/// </summary>
public class BitBucketProjectSettings
{
    /// <summary>
    /// 工作區與 Repository（格式：workspace/repository）
    /// </summary>
    public required string WorkspaceAndRepo { get; init; }

    /// <summary>
    /// 目標分支（單一值）
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 抓取模式（覆寫全域設定）
    /// </summary>
    public FetchMode? FetchMode { get; init; }

    /// <summary>
    /// Release Branch（覆寫全域設定）
    /// </summary>
    public string? ReleaseBranch { get; init; }

    /// <summary>
    /// 開始日期（覆寫全域設定）
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 結束日期（覆寫全域設定）
    /// </summary>
    public DateTime? EndDate { get; init; }

    // 已棄用，保留向後相容
    [Obsolete("請使用 TargetBranch（單一值）")]
    public List<string>? TargetBranches { get; init; }
}
```

### 2.4 EffectiveProjectConfig（執行時解析）

```csharp
/// <summary>
/// 解析後的有效專案配置（合併全域與專案層級設定）
/// </summary>
public record EffectiveProjectConfig
{
    /// <summary>
    /// 專案識別碼
    /// </summary>
    public required string ProjectIdentifier { get; init; }

    /// <summary>
    /// 目標分支
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// 抓取模式
    /// </summary>
    public required FetchMode FetchMode { get; init; }

    /// <summary>
    /// Release Branch（僅 FetchMode = ReleaseBranch 時有效）
    /// </summary>
    public ReleaseBranchName? ReleaseBranch { get; init; }

    /// <summary>
    /// 開始日期（僅 FetchMode = DateRange 時有效）
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 結束日期（僅 FetchMode = DateRange 時有效）
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 從專案設定與全域設定解析有效配置
    /// </summary>
    public static EffectiveProjectConfig Resolve<TProjectSettings>(
        TProjectSettings project,
        SyncOptionsSettings global,
        Func<TProjectSettings, string> getIdentifier,
        Func<TProjectSettings, string> getTargetBranch,
        Func<TProjectSettings, FetchMode?> getFetchMode,
        Func<TProjectSettings, string?> getReleaseBranch,
        Func<TProjectSettings, DateTime?> getStartDate,
        Func<TProjectSettings, DateTime?> getEndDate);
}
```

---

## 3. API 回應模型

### 3.1 BranchInfo（分支資訊）

```csharp
/// <summary>
/// 分支資訊
/// </summary>
public record BranchInfo
{
    /// <summary>
    /// 分支名稱
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 最新 Commit SHA
    /// </summary>
    public required string CommitSha { get; init; }

    /// <summary>
    /// 最新 Commit 日期
    /// </summary>
    public DateTimeOffset? CommitDate { get; init; }
}
```

### 3.2 BranchCompareResult（分支比對結果）

```csharp
/// <summary>
/// 分支比對結果
/// </summary>
public record BranchCompareResult
{
    /// <summary>
    /// 起始分支
    /// </summary>
    public required string FromBranch { get; init; }

    /// <summary>
    /// 結束分支
    /// </summary>
    public required string ToBranch { get; init; }

    /// <summary>
    /// 差異的 Commit 清單
    /// </summary>
    public IReadOnlyList<CommitInfo> Commits { get; init; } = Array.Empty<CommitInfo>();

    /// <summary>
    /// 差異的 Commit SHA 清單（簡化版）
    /// </summary>
    public IEnumerable<string> CommitShas => Commits.Select(c => c.Sha);
}
```

### 3.3 CommitInfo（Commit 資訊）

```csharp
/// <summary>
/// Commit 資訊
/// </summary>
public record CommitInfo
{
    /// <summary>
    /// Commit SHA
    /// </summary>
    public required string Sha { get; init; }

    /// <summary>
    /// 簡短 SHA
    /// </summary>
    public string ShortSha => Sha.Length >= 8 ? Sha[..8] : Sha;

    /// <summary>
    /// Commit 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 作者名稱
    /// </summary>
    public string? AuthorName { get; init; }

    /// <summary>
    /// Commit 日期
    /// </summary>
    public DateTimeOffset? Date { get; init; }
}
```

---

## 4. 關係圖

```
┌─────────────────────────────────────────────────────────────┐
│                     Configuration Layer                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  SyncOptionsSettings                                         │
│  ├─ ReleaseBranch: string?                                  │
│  ├─ StartDate: DateTime?                                    │
│  ├─ EndDate: DateTime?                                      │
│  └─ DefaultFetchMode: FetchMode                             │
│          │                                                   │
│          │ (全域設定)                                         │
│          ▼                                                   │
│  ┌───────────────────┐    ┌───────────────────┐             │
│  │ GitLabProject     │    │ BitBucketProject  │             │
│  │ Settings          │    │ Settings          │             │
│  ├───────────────────┤    ├───────────────────┤             │
│  │ ProjectPath       │    │ WorkspaceAndRepo  │             │
│  │ TargetBranch      │    │ TargetBranch      │             │
│  │ FetchMode?        │    │ FetchMode?        │             │
│  │ ReleaseBranch?    │    │ ReleaseBranch?    │             │
│  │ StartDate?        │    │ StartDate?        │             │
│  │ EndDate?          │    │ EndDate?          │             │
│  └───────────────────┘    └───────────────────┘             │
│          │                        │                          │
│          │ (專案層級覆寫)          │                          │
│          └────────┬───────────────┘                          │
│                   ▼                                          │
│          EffectiveProjectConfig                              │
│          ├─ ProjectIdentifier                                │
│          ├─ TargetBranch                                     │
│          ├─ FetchMode                                        │
│          ├─ ReleaseBranch?                                   │
│          ├─ StartDate?                                       │
│          └─ EndDate?                                         │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  FetchMode (enum)                                            │
│  ├─ DateRange = 0                                           │
│  └─ ReleaseBranch = 1                                       │
│                                                              │
│  ReleaseBranchName (value object)                           │
│  ├─ Value: string          // "release/20260120"            │
│  ├─ Date: DateOnly         // 2026-01-20                    │
│  ├─ Parse(string)                                           │
│  ├─ TryParse(string)                                        │
│  └─ IComparable<ReleaseBranchName>                          │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    API Response Models                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  BranchInfo                                                  │
│  ├─ Name: string                                            │
│  ├─ CommitSha: string                                       │
│  └─ CommitDate: DateTimeOffset?                             │
│                                                              │
│  BranchCompareResult                                         │
│  ├─ FromBranch: string                                      │
│  ├─ ToBranch: string                                        │
│  └─ Commits: IReadOnlyList<CommitInfo>                      │
│                                                              │
│  CommitInfo                                                  │
│  ├─ Sha: string                                             │
│  ├─ Title: string                                           │
│  ├─ AuthorName: string?                                     │
│  └─ Date: DateTimeOffset?                                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. 狀態轉換

### 5.1 FetchMode 選擇流程

```
開始
  │
  ▼
專案設定有 FetchMode?
  │
  ├─ 是 → 使用專案設定的 FetchMode
  │
  └─ 否 → 使用全域 DefaultFetchMode
         │
         ▼
    FetchMode = ?
         │
         ├─ DateRange ─────────────────────┐
         │                                  │
         │  驗證 StartDate/EndDate 有效     │
         │          │                       │
         │          ▼                       │
         │  呼叫 GetByDateRangeAsync()      │
         │                                  │
         └─ ReleaseBranch ─────────────────┐
                    │                       │
                    ▼                       │
            驗證 ReleaseBranch 有效          │
                    │                       │
                    ▼                       │
            判斷是否為最新版                  │
                    │                       │
         ┌─────────┴─────────┐              │
         │                   │              │
         ▼                   ▼              │
    是最新版            不是最新版           │
         │                   │              │
         ▼                   ▼              │
比對 TargetBranch    比對下一版 Release      │
         │                   │              │
         └─────────┬─────────┘              │
                   │                        │
                   ▼                        │
            回傳 PR 清單 ◄──────────────────┘
```

---

## 6. 驗證規則

### 6.1 ReleaseBranchName

| 驗證項目 | 規則 | 錯誤訊息範例 |
|----------|------|--------------|
| 格式 | 必須符合 `release/yyyyMMdd` | "Invalid release branch format. Expected 'release/yyyyMMdd', got 'release-20260120'" |
| 日期有效性 | yyyyMMdd 必須是有效日期 | "Invalid date in release branch name. '20260132' is not a valid date" |
| 非空 | 不可為 null 或空字串 | "Release branch name cannot be null or empty" |

### 6.2 EffectiveProjectConfig

| 驗證項目 | 規則 | 錯誤訊息範例 |
|----------|------|--------------|
| DateRange 模式 | StartDate 和 EndDate 都必須有值 | "StartDate and EndDate are required when FetchMode is DateRange" |
| ReleaseBranch 模式 | ReleaseBranch 必須有值 | "ReleaseBranch is required when FetchMode is ReleaseBranch" |
| 日期範圍 | StartDate <= EndDate | "StartDate must be before or equal to EndDate" |
