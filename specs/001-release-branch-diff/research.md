# Research: Release Branch 差異比對功能

**Feature Branch**: `001-release-branch-diff`
**Date**: 2026-01-20
**Status**: Complete

## 研究摘要

本研究報告整合了現有程式碼架構分析與外部 API 文件研究結果，為 Release Branch 差異比對功能提供技術決策依據。

---

## 1. 現有架構分析

### 1.1 配置結構

**Decision**: 延續現有 `IOptions<T>` 模式，新增 `SyncOptionsSettings` 類別

**Rationale**:
- 現有的 `GitLabSettings`、`BitBucketSettings` 已使用 `IOptions<T>` 模式
- 保持一致性便於維護
- 支援 appsettings.json 的階層配置

**Alternatives considered**:
- 使用自訂配置載入器：過度設計，不符合 KISS 原則
- 直接讀取 JSON 檔案：失去型別安全與驗證功能

### 1.2 TargetBranches 變更

**Decision**: 將 `TargetBranches` (List) 改為 `TargetBranch` (string)

**Rationale**:
- 需求明確指出每個專案只有一個主要目標分支
- 簡化配置與程式邏輯
- 原有多分支支援可透過多個專案配置實現

**Alternatives considered**:
- 保留陣列並取第一個元素：造成配置混淆
- 同時支援兩種格式：增加複雜度，違反 KISS

**向後相容考量**:
- 短期內可考慮同時讀取 `TargetBranch` 和 `TargetBranches`，優先使用 `TargetBranch`
- 在 appsettings.json 範例和文件中標示為 deprecated

### 1.3 Value Object 設計

**Decision**: 建立 `ReleaseBranchName` Value Object，參考現有 `BranchName` 實作

**Rationale**:
- 現有 `BranchName` 提供良好的設計範例
- Value Object 確保不可變性與自我驗證
- 封裝命名格式驗證與日期解析邏輯

**實作細節**:
```csharp
public record ReleaseBranchName
{
    private static readonly Regex Pattern = new(@"^release/(\d{8})$", RegexOptions.Compiled);

    public string Value { get; }
    public DateOnly Date { get; }

    public ReleaseBranchName(string value)
    {
        // 驗證格式並解析日期
    }

    public static bool TryParse(string value, out ReleaseBranchName? result);
    public static ReleaseBranchName Parse(string value);
}
```

---

## 2. GitLab API 研究

### 2.1 列出分支

**端點**: `GET /projects/:id/repository/branches`

**關鍵參數**:
| 參數 | 值 | 說明 |
|------|-----|------|
| `search` | `release` | 篩選 release 開頭的分支 |
| `per_page` | `100` | 最大分頁大小 |
| `sort` | `name` | 按名稱排序便於版本比對 |

**NGitLab 函式庫支援**:
- `IGitLabClient.GetRepository(projectId).Branches` 可列出分支
- 需確認是否支援 `search` 參數

### 2.2 分支比對

**端點**: `GET /projects/:id/repository/compare`

**必要參數**:
| 參數 | 說明 |
|------|------|
| `from` | 起始分支（如 `release/20260113`）|
| `to` | 結束分支（如 `release/20260120` 或 `master`）|

**回應結構**:
```json
{
  "commits": [
    {
      "id": "commit-sha",
      "title": "commit message",
      "author_name": "...",
      "created_at": "2026-01-20T10:30:00Z"
    }
  ],
  "diffs": [...],
  "compare_same_ref": false
}
```

**NGitLab 函式庫支援**:
- 經查 NGitLab 函式庫，`IRepositoryClient` 提供 `Compare` 方法
- 可直接使用，無需手動呼叫 HTTP API

---

## 3. BitBucket API 研究

### 3.1 列出分支

**端點**: `GET /2.0/repositories/{workspace}/{repo_slug}/refs/branches`

**關鍵參數**:
| 參數 | 值 | 說明 |
|------|-----|------|
| `q` | `name ~ "release"` | BitBucket 查詢語法篩選 |
| `pagelen` | `100` | 最大分頁大小 |
| `sort` | `-name` | 降序排列（最新版本在前）|

**實作方式**: 使用現有的 `HttpClient` 擴展 `BitBucketApiClient`

### 3.2 分支比對

**端點**: `GET /2.0/repositories/{workspace}/{repo_slug}/diffstat/{spec}`

**Spec 格式**: `{from}..{to}`（如 `release/20260113..release/20260120`）

**注意事項**:
- 分支名稱中的 `/` 需要 URL 編碼為 `%2F`
- 例如：`release%2F20260113..release%2F20260120`

**回應結構**:
```json
{
  "values": [
    {
      "status": "modified",
      "lines_added": 10,
      "lines_removed": 5,
      "old": { "path": "..." },
      "new": { "path": "..." }
    }
  ]
}
```

---

## 4. FetchMode 策略實作

### 4.1 策略選擇邏輯

**Decision**: 使用簡單的條件判斷，不引入完整的 Strategy Pattern 框架

**Rationale**:
- 只有兩種模式（DateRange、ReleaseBranch），不需要複雜的策略註冊機制
- 現有的 `BasePullRequestRepository` 已提供足夠的擴展點
- 符合 KISS 原則

**實作方式**:
```csharp
// 在 BasePlatformService 或 SyncOrchestrator 中
public async Task<IEnumerable<PullRequestInfo>> GetPullRequestsAsync(
    ProjectConfig config, CancellationToken ct)
{
    return config.FetchMode switch
    {
        FetchMode.DateRange => await GetByDateRangeAsync(config, ct),
        FetchMode.ReleaseBranch => await GetByReleaseBranchAsync(config, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(config.FetchMode))
    };
}
```

### 4.2 Release Branch 版本判斷

**Decision**: 透過列出所有 release 分支並比對日期來判斷是否為最新版

**Rationale**:
- 避免額外的 API 呼叫來檢查「是否存在更新版本」
- 一次取得所有 release 分支後，在本地進行比對
- 快取分支清單可減少重複查詢

**實作邏輯**:
```
1. 列出所有 release/* 分支
2. 解析每個分支的日期（yyyyMMdd）
3. 按日期排序
4. 判斷設定的 ReleaseBranch 是否為最新
   - 是：比對與 TargetBranch 的差異
   - 否：找到下一版 release，比對兩者差異
```

---

## 5. 配置覆寫優先權

**Decision**: Repository 層級 > 全域設定

**實作方式**:
```csharp
public record EffectiveProjectConfig
{
    public FetchMode FetchMode { get; }
    public string? ReleaseBranch { get; }
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }

    public static EffectiveProjectConfig Resolve(
        ProjectSettings project,
        SyncOptionsSettings global)
    {
        return new EffectiveProjectConfig
        {
            FetchMode = project.FetchMode ?? global.DefaultFetchMode,
            ReleaseBranch = project.ReleaseBranch ?? global.ReleaseBranch,
            StartDate = project.StartDate ?? global.StartDate,
            EndDate = project.EndDate ?? global.EndDate
        };
    }
}
```

---

## 6. 錯誤處理策略

### 6.1 Release Branch 不存在

**Decision**: 拋出自訂例外 `ReleaseBranchNotFoundException`

**Rationale**:
- 這是可預期的業務錯誤，需要明確的錯誤類型
- 讓呼叫端能夠決定是否繼續處理其他專案

**例外訊息範例**:
```
Release branch 'release/20260120' not found in repository 'mygroup/myproject'.
Available release branches: release/20260113, release/20260106
```

### 6.2 配置驗證錯誤

**Decision**: 在應用程式啟動時進行配置驗證

**實作方式**:
- 使用 `IOptions<T>.Validate()` 或自訂驗證邏輯
- 在 DI 容器建置階段失敗，而非執行階段

---

## 7. 測試策略

### 7.1 單元測試

| 測試對象 | 測試項目 |
|----------|----------|
| `ReleaseBranchName` | 格式驗證、日期解析、比較運算 |
| `FetchMode` | 列舉值正確性 |
| 配置覆寫邏輯 | 各種覆寫情境 |

### 7.2 整合測試

| 測試對象 | 測試項目 |
|----------|----------|
| `GitLabApiClient` | 分支列出、分支比對（需真實 Token）|
| `BitBucketApiClient` | 分支列出、分支比對（需真實 Token）|

---

## 8. 結論

所有技術決策已完成，無需進一步澄清。可進入 Phase 1 設計階段。
