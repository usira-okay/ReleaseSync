# Research: Repository-Based Export Format

**Feature**: Repository-Based Export Format
**Date**: 2025-11-15
**Status**: ✅ Complete

## Overview

此文件記錄 Repository-Based Export Format 功能的技術研究與決策過程。由於此功能為資料格式轉換,使用現有技術堆疊,無需引入新技術或框架,因此研究重點在於確認最佳實作方式。

## Research Questions & Decisions

### Q1: Repository 分組邏輯實作方式

**研究問題**: 如何最有效率地將 Pull Requests 依據 Repository 與 Platform 進行分組?

**評估選項**:

| 選項 | 描述 | 優點 | 缺點 | 結論 |
|------|------|------|------|------|
| LINQ GroupBy | 使用 `GroupBy(pr => new { pr.RepositoryName, pr.Platform })` | 簡潔易讀、符合 LINQ 慣例、效能足夠 | 需要產生匿名型別作為 Key | ✅ **採用** |
| Dictionary 手動分組 | 手動迭代並填入 `Dictionary<(string, string), List<PR>>` | 完全控制分組邏輯 | 程式碼較冗長、容易出錯 | ❌ 不採用 |
| Lookup 類別 | 使用 `ToLookup()` | 效能略優於 GroupBy | 不可變集合,較不直觀 | ❌ 不採用 |

**最終決策**: 使用 LINQ `GroupBy`

**理由**:
- 符合 KISS 原則,程式碼簡潔易讀
- LINQ 為 .NET 標準函式庫,無需額外依賴
- 效能足以應付預期規模 (100 repositories × 20 PRs = 2000 筆資料)
- 團隊熟悉 LINQ 語法,降低維護成本

**程式碼範例**:
```csharp
var repositoryGroups = syncResult.PullRequests
    .GroupBy(pr => new { pr.RepositoryName, pr.Platform })
    .Select(group => new RepositoryGroupDto
    {
        RepositoryName = ExtractRepositoryName(group.Key.RepositoryName),
        Platform = group.Key.Platform,
        PullRequests = group.Select(MapToPullRequestDto).ToList()
    })
    .ToList();
```

---

### Q2: Repository 名稱提取規則

**研究問題**: 如何從完整的 Repository 路徑 (如 `owner/repo`) 提取短名稱 (如 `repo`)?

**評估選項**:

| 選項 | 描述 | 優點 | 缺點 | 結論 |
|------|------|------|------|------|
| `String.Split('/')` + 取最後元素 | `repositoryName.Split('/')[^1]` | 簡單直觀、效能高 | 需處理無 `/` 的情況 | ✅ **採用** |
| 正規表達式 | 使用 Regex 匹配 `/([^/]+)$` | 彈性高 | 效能較差、過度設計 | ❌ 不採用 |
| `LastIndexOf` + `Substring` | 手動找最後一個 `/` 並擷取 | 效能最優 | 程式碼較不直觀 | ❌ 不採用 |

**最終決策**: 使用 `String.Split('/')` 並取最後元素

**理由**:
- 符合 KISS 原則,一行程式碼即可完成
- 使用 C# 9.0 的 Index from End (`[^1]`) 語法更簡潔
- 效能影響可忽略 (字串操作非瓶頸)
- Defensive programming: 若無 `/` 則 `Split` 返回單一元素陣列,仍可正確取得原始名稱

**程式碼範例**:
```csharp
private static string ExtractRepositoryName(string repositoryName)
{
    // 從完整路徑中提取 Repository 名稱
    // 例如: "owner/repo" -> "repo", "single" -> "single"
    var parts = repositoryName.Split('/');
    return parts[^1]; // C# 9.0 Index from End 語法
}
```

**邊界情況測試**:
- `"owner/repo"` → `"repo"` ✅
- `"org/team/project"` → `"project"` ✅
- `"standalone"` → `"standalone"` ✅
- `""` (空字串) → `""` ✅ (雖不太可能發生,但仍安全處理)

---

### Q3: Work Item null 處理策略

**研究問題**: 當 Pull Request 無關聯 Work Item 時,JSON 應如何表示?

**評估選項**:

| 選項 | 描述 | 優點 | 缺點 | 結論 |
|------|------|------|------|------|
| JSON `null` | `"workItem": null` | 明確表示「無資料」、符合使用者要求 | - | ✅ **採用** |
| 空物件 | `"workItem": {}` | 型別一致 | 語意不明確,不符合使用者要求 | ❌ 不採用 |
| 省略欄位 | 不輸出 `workItem` 欄位 | JSON 檔案較小 | 違反 schema 一致性,不符合使用者要求 | ❌ 不採用 |

**最終決策**: 使用 JSON `null`

**理由**:
- 符合使用者明確要求:「當無法抓到 azure devops 資料時 workItem 請給 null」
- `System.Text.Json` 預設行為:C# `null` 序列化為 JSON `null`
- 語意明確:區分「無 Work Item」與「Work Item 資料不完整」
- 符合 JSON Schema 的 `oneOf: [WorkItem, null]` 定義

**程式碼範例**:
```csharp
public class RepositoryPullRequestDto
{
    [JsonPropertyName("workItem")]
    public PullRequestWorkItemDto? WorkItem { get; init; } // Nullable reference type

    // ... 其他欄位
}

// 轉換邏輯
WorkItem = pr.AssociatedWorkItem != null
    ? MapToWorkItemDto(pr.AssociatedWorkItem)
    : null // 明確設為 null
```

**JSON 輸出範例**:
```json
{
  "pullRequestTitle": "Fix authentication bug",
  "workItem": null,  // ← 明確的 JSON null
  "sourceBranch": "fix/auth",
  // ...
}
```

---

### Q4: JSON Schema 版本選擇

**研究問題**: 使用哪個版本的 JSON Schema 規範?

**評估選項**:

| 選項 | 描述 | 優點 | 缺點 | 結論 |
|------|------|------|------|------|
| Draft 2020-12 (最新) | 使用最新 JSON Schema 規範 | 功能最完整、社群支援良好 | - | ✅ **採用** |
| Draft 07 (舊版) | 使用較舊的穩定版本 | 工具支援最廣 | 功能較少 | ❌ 不採用 |
| OpenAPI 3.1 | 使用 OpenAPI Schema | 整合 API 文件方便 | 本專案為 CLI,非 API | ❌ 不適用 |

**最終決策**: JSON Schema Draft 2020-12

**理由**:
- 2020-12 為當前最新穩定版本,社群支援良好
- 支援 `$defs` 關鍵字 (取代舊版 `definitions`),語意更明確
- 支援 `oneOf` 處理 nullable 型別 (如 `workItem: WorkItem | null`)
- 主流工具 (如 VS Code JSON Schema validator) 皆支援

**參考文件**: https://json-schema.org/draft/2020-12/json-schema-core

---

### Q5: 效能考量與最佳化策略

**研究問題**: 是否需要針對大型資料集進行效能最佳化?

**現況分析**:
- **預期規模**: 100 repositories × 20 PRs = 2000 筆資料
- **效能目標**: 匯出時間不超過 5 秒
- **主要操作**: LINQ GroupBy、字串分割、物件對映、JSON 序列化

**效能測試預估**:

| 操作 | 預估時間 (2000 筆資料) | 瓶頸風險 |
|------|------------------------|----------|
| LINQ GroupBy + Select | < 10ms | ❌ 極低 |
| Repository 名稱提取 (Split) | < 5ms | ❌ 極低 |
| 物件對映 (DTO 轉換) | < 50ms | ❌ 極低 |
| JSON 序列化 (System.Text.Json) | < 100ms | ❌ 低 |
| 檔案寫入 (本地 SSD) | < 50ms | ❌ 極低 |
| **總計** | **< 250ms** | ✅ 遠低於 5 秒目標 |

**最終決策**: 不需要額外最佳化

**理由**:
- 預估效能遠優於目標 (250ms vs 5000ms)
- `System.Text.Json` 已針對效能最佳化 (使用 Span<T> 與記憶體池)
- LINQ 在此規模下效能足夠,無需手動最佳化
- 符合 KISS 原則:避免過早最佳化
- 若未來需要處理更大規模 (10萬筆+),再考慮串流處理或批次寫入

**監控建議**:
- 在整合測試中加入效能基準測試 (benchmark)
- 若實際匯出時間超過 1 秒,記錄警告日誌
- 使用 `Stopwatch` 測量各階段耗時,協助未來最佳化

---

## Technology Stack Summary

### 核心技術

| 技術 | 版本 | 用途 | 理由 |
|------|------|------|------|
| C# | 9.0+ | 主要開發語言 | 專案既有技術,支援 Record Types、Index from End 等現代語法 |
| .NET | 8.0 / 9.0 | 執行環境 | 專案既有技術,.NET 8 為 LTS 版本,.NET 9 為最新版 |
| System.Text.Json | .NET 內建 | JSON 序列化 | 高效能、官方支援、專案既有技術 |
| LINQ | .NET 內建 | 資料查詢與轉換 | 簡潔易讀、效能足夠 |
| xUnit | 專案既有 | 單元測試框架 | 專案標準測試框架 |
| FluentAssertions | 專案既有 | 測試斷言 | 提升測試可讀性 |

### 無需引入的技術

以下技術經評估後**不需要**引入:

- ❌ **AutoMapper**: DTO 對映邏輯簡單,手動對映更直觀
- ❌ **Newtonsoft.Json**: `System.Text.Json` 已足夠,無需第三方函式庫
- ❌ **FluentValidation**: DTO 驗證邏輯簡單,使用 required 關鍵字即可
- ❌ **Benchmark.NET**: 目前規模不需要詳細效能分析
- ❌ **任何 ORM**: 無資料庫操作需求

---

## Best Practices

### DTO 設計最佳實踐

1. **使用 Record Types**:
   ```csharp
   public record RepositoryBasedOutputDto
   {
       public required DateTime StartDate { get; init; }
       // ...
   }
   ```
   - **理由**: 不可變性 (immutability)、value-based equality、簡潔語法

2. **Required 關鍵字**:
   ```csharp
   public required string RepositoryName { get; init; }
   ```
   - **理由**: 編譯時期檢查、避免 null 相關錯誤

3. **Nullable Reference Types**:
   ```csharp
   public PullRequestWorkItemDto? WorkItem { get; init; }
   ```
   - **理由**: 明確表達 nullable 語意、編譯器協助檢查

4. **JsonPropertyName 屬性**:
   ```csharp
   [JsonPropertyName("repositoryName")]
   public required string RepositoryName { get; init; }
   ```
   - **理由**: 明確控制 JSON 欄位名稱、符合 camelCase 慣例

### 測試策略

1. **測試金字塔**:
   - **單元測試 (90%)**: 測試 DTO 轉換邏輯、Repository 名稱提取
   - **整合測試 (10%)**: 測試完整 JSON 序列化 (JsonFileExporter)
   - **不需要 E2E 測試**: CLI 測試由現有測試涵蓋

2. **測試案例設計**:
   - ✅ 正常情境 (Happy path)
   - ✅ 邊界情況 (Empty data、單一 repository)
   - ✅ 異常情況 (null Work Item、特殊字元)
   - ✅ 效能測試 (2000 筆資料)

3. **測試命名慣例**:
   ```csharp
   [Fact]
   public void FromSyncResult_WithMultipleRepositories_GroupsByNameAndPlatform()
   {
       // Arrange, Act, Assert
   }
   ```
   - **格式**: `MethodName_Scenario_ExpectedBehavior`
   - **理由**: 清楚表達測試意圖、方便失敗診斷

---

## Risks & Mitigations

### 風險 1: 破壞性變更影響現有使用者

**風險等級**: 🔴 高

**描述**: 完全移除舊的 Work Item-centric 格式,可能影響依賴舊格式的下游系統

**緩解措施**:
- ✅ 在 Release Notes 明確標註破壞性變更
- ✅ 提供遷移指南 (quickstart.md)
- ✅ 建議使用者在更新前備份現有 JSON 檔案
- ⚠️ 考慮提供格式轉換工具 (Out of Scope,可作為後續功能)

---

### 風險 2: Repository 名稱提取邏輯錯誤

**風險等級**: 🟡 中

**描述**: 不同平台的 Repository 名稱格式可能有差異,導致提取錯誤

**緩解措施**:
- ✅ 使用 defensive programming (Split 無 `/` 時仍可正常運作)
- ✅ 撰寫完整的單元測試涵蓋各種格式
- ✅ 記錄 inline comment 說明提取規則
- ✅ 若未來發現新格式,僅需修改單一方法 (`ExtractRepositoryName`)

---

### 風險 3: JSON Schema 與實作不一致

**風險等級**: 🟡 中

**描述**: 手動維護 JSON Schema 可能與實際 DTO 實作產生差異

**緩解措施**:
- ✅ 在整合測試中驗證實際輸出符合 Schema
- ✅ 使用 JSON Schema validator 工具自動驗證
- ✅ 在 PR Review 時檢查 Schema 與 DTO 的一致性
- ⚠️ 考慮使用自動產生工具 (如 NJsonSchema) - 目前不需要,未來可評估

---

## References

### 官方文件

- [System.Text.Json 文件](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/)
- [JSON Schema Specification (Draft 2020-12)](https://json-schema.org/draft/2020-12/json-schema-core)
- [C# 9.0 Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [LINQ GroupBy](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.groupby)

### 專案內部文件

- [專案憲章 (.specify/memory/constitution.md)](../../.specify/memory/constitution.md)
- [Feature Specification (spec.md)](./spec.md)
- [Implementation Plan (plan.md)](./plan.md)

---

## Approval

- ✅ **技術決策已確認**: 使用現有技術堆疊,無需引入新依賴
- ✅ **效能分析已完成**: 預估效能遠優於目標
- ✅ **風險已識別並緩解**: 破壞性變更、名稱提取、Schema 一致性
- ✅ **準備進入 Phase 1**: 資料模型設計與契約定義

**簽核日期**: 2025-11-15
