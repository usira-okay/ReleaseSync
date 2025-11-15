# Quick Start: Repository-Based Export Format

**Feature**: Repository-Based Export Format
**Last Updated**: 2025-11-15
**Audience**: End Users, Developers, Data Analysts

## 概述

Repository-Based Export Format 是 ReleaseSync 的全新 JSON 匯出格式,以 **Repository 為主體** 組織 Pull Request 資料,取代原有的 Work Item-centric 格式。此格式特別針對 Google Sheets 整合最佳化,讓您能輕鬆分析各專案的開發活動。

### 主要優勢

- ✅ **Repository 分組**: Pull Requests 按專案分類,快速找到特定 repository 的變更
- ✅ **平台區分**: 相同名稱但不同平台的 repository 獨立顯示
- ✅ **扁平結構**: 適合試算表的行列邏輯,無複雜巢狀
- ✅ **Work Item 保留**: 仍可追蹤 PR 關聯的 Azure DevOps Work Item
- ✅ **簡化欄位**: 僅保留關鍵資訊,減少檔案大小與複雜度

---

## 快速開始

### 1. 執行同步命令

```bash
# 基本用法:同步 GitLab 與 BitBucket
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --bitbucket \
  -o output.json

# 包含 Azure DevOps (需要 Work Item 關聯)
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --bitbucket \
  --azdo \
  -o output.json

# 啟用 verbose 模式查看詳細日誌
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  -v \
  -o output.json
```

### 2. 檢視輸出檔案

命令執行完成後,`output.json` 將包含以下結構:

```json
{
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-01-31T23:59:59Z",
  "repositories": [
    {
      "repositoryName": "backend",
      "platform": "GitLab",
      "pullRequests": [
        {
          "workItem": {
            "workItemId": 12345,
            "workItemTitle": "Implement user authentication",
            "workItemTeam": "Backend Team",
            "workItemType": "User Story",
            "workItemUrl": "https://dev.azure.com/..."
          },
          "pullRequestTitle": "Add JWT authentication",
          "sourceBranch": "feature/user-auth",
          "targetBranch": "main",
          "mergedAt": "2025-01-15T10:30:00Z",
          "authorUserId": "12345",
          "authorDisplayName": "John Doe",
          "pullRequestUrl": "https://gitlab.com/..."
        }
      ]
    }
  ]
}
```

---

## 輸出格式說明

### 頂層結構

| 欄位 | 型別 | 說明 |
|------|------|------|
| `startDate` | string (ISO 8601) | 查詢開始日期 (UTC) |
| `endDate` | string (ISO 8601) | 查詢結束日期 (UTC) |
| `repositories` | array | Repository 分組清單 |

### Repository 物件

每個 repository 包含:

| 欄位 | 型別 | 說明 | 範例 |
|------|------|------|------|
| `repositoryName` | string | Repository 簡短名稱 (已從 `owner/repo` 提取為 `repo`) | `"backend"` |
| `platform` | string | 平台名稱 | `"GitLab"`, `"BitBucket"`, `"AzureDevOps"` |
| `pullRequests` | array | 該 repository 的所有 PR | `[...]` |

**注意**: 相同名稱但不同平台的 repository 會分開列出。例如:
- `{ "repositoryName": "backend", "platform": "GitLab" }`
- `{ "repositoryName": "backend", "platform": "BitBucket" }`

### Pull Request 物件

每個 PR 包含:

| 欄位 | 型別 | Nullable | 說明 |
|------|------|----------|------|
| `workItem` | object / null | ✅ | 關聯的 Work Item (無關聯時為 `null`) |
| `pullRequestTitle` | string | ❌ | PR 標題 |
| `sourceBranch` | string | ❌ | 來源分支 |
| `targetBranch` | string | ❌ | 目標分支 |
| `mergedAt` | string / null | ✅ | 合併時間 (UTC,未合併為 `null`) |
| `authorUserId` | string / null | ✅ | 作者平台 ID |
| `authorDisplayName` | string / null | ✅ | 作者顯示名稱 |
| `pullRequestUrl` | string / null | ✅ | PR URL |

### Work Item 物件

當 `workItem` 不為 `null` 時,包含:

| 欄位 | 型別 | Nullable | 說明 |
|------|------|----------|------|
| `workItemId` | number | ❌ | Work Item ID (Azure DevOps) |
| `workItemTitle` | string | ❌ | Work Item 標題 |
| `workItemTeam` | string / null | ✅ | 所屬團隊 (經 TeamMapping 轉換) |
| `workItemType` | string | ❌ | 類型 (Task, Bug, User Story, etc.) |
| `workItemUrl` | string / null | ✅ | Work Item URL |

---

## 範例輸出

### 範例 1: 單一 Repository,包含 Work Item 關聯

```json
{
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-01-31T23:59:59Z",
  "repositories": [
    {
      "repositoryName": "backend",
      "platform": "GitLab",
      "pullRequests": [
        {
          "workItem": {
            "workItemId": 12345,
            "workItemTitle": "Implement user authentication",
            "workItemTeam": "Backend Team",
            "workItemType": "User Story",
            "workItemUrl": "https://dev.azure.com/org/project/_workitems/edit/12345"
          },
          "pullRequestTitle": "Add JWT authentication",
          "sourceBranch": "feature/user-auth",
          "targetBranch": "main",
          "mergedAt": "2025-01-15T10:30:00Z",
          "authorUserId": "12345",
          "authorDisplayName": "John Doe",
          "pullRequestUrl": "https://gitlab.com/org/backend/-/merge_requests/123"
        }
      ]
    }
  ]
}
```

### 範例 2: 多 Repositories,包含無 Work Item 的 PR

```json
{
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-01-31T23:59:59Z",
  "repositories": [
    {
      "repositoryName": "backend",
      "platform": "GitLab",
      "pullRequests": [
        {
          "workItem": null,
          "pullRequestTitle": "Fix typo in README",
          "sourceBranch": "fix/readme-typo",
          "targetBranch": "main",
          "mergedAt": "2025-01-20T14:20:00Z",
          "authorUserId": "67890",
          "authorDisplayName": "Jane Smith",
          "pullRequestUrl": "https://gitlab.com/org/backend/-/merge_requests/124"
        }
      ]
    },
    {
      "repositoryName": "frontend",
      "platform": "BitBucket",
      "pullRequests": [
        {
          "workItem": {
            "workItemId": 54321,
            "workItemTitle": "Redesign user profile page",
            "workItemTeam": "Frontend Team",
            "workItemType": "Task",
            "workItemUrl": "https://dev.azure.com/org/project/_workitems/edit/54321"
          },
          "pullRequestTitle": "Update profile UI components",
          "sourceBranch": "feature/profile-redesign",
          "targetBranch": "develop",
          "mergedAt": "2025-01-25T16:45:00Z",
          "authorUserId": "{abc-123-def-456}",
          "authorDisplayName": "Alice Johnson",
          "pullRequestUrl": "https://bitbucket.org/org/frontend/pull-requests/42"
        }
      ]
    }
  ]
}
```

### 範例 3: 空資料集 (指定日期區間內無 PR)

```json
{
  "startDate": "2025-02-01T00:00:00Z",
  "endDate": "2025-02-28T23:59:59Z",
  "repositories": []
}
```

---

## Google Sheets 整合

### 方法 1: 手動匯入 JSON

1. **開啟 Google Sheets** 並建立新試算表
2. **使用 ImportJSON 外掛**:
   - 安裝 [ImportJSON](https://github.com/bradjasper/ImportJSON)
   - 或使用 Apps Script 自訂函式
3. **匯入資料**:
   ```
   =IMPORTJSON("file:///path/to/output.json", "/repositories", "noHeaders")
   ```

### 方法 2: 使用 Apps Script

建立 Google Apps Script 讀取 JSON 檔案:

```javascript
function importReleaseSync() {
  const jsonData = /* 貼上 JSON 內容 */;
  const sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();

  // 建立表頭
  sheet.appendRow([
    'Repository', 'Platform', 'PR Title', 'Source Branch', 'Target Branch',
    'Merged At', 'Author', 'Work Item ID', 'Work Item Title', 'Team'
  ]);

  // 展開資料
  jsonData.repositories.forEach(repo => {
    repo.pullRequests.forEach(pr => {
      sheet.appendRow([
        repo.repositoryName,
        repo.platform,
        pr.pullRequestTitle,
        pr.sourceBranch,
        pr.targetBranch,
        pr.mergedAt || '',
        pr.authorDisplayName || '',
        pr.workItem?.workItemId || '',
        pr.workItem?.workItemTitle || '',
        pr.workItem?.workItemTeam || ''
      ]);
    });
  });
}
```

### 方法 3: 使用 JSON to CSV 轉換器

1. 使用線上工具將 JSON 轉換為 CSV (如 [ConvertCSV](https://www.convertcsv.com/json-to-csv.htm))
2. 在 Google Sheets 中匯入 CSV 檔案
3. **注意**: 可能需要手動處理巢狀的 `workItem` 物件

### 建議試算表結構

| Repository | Platform | PR Title | Source Branch | Target Branch | Merged At | Author | Work Item ID | Work Item Title | Team |
|------------|----------|----------|---------------|---------------|-----------|--------|--------------|-----------------|------|
| backend    | GitLab   | Add JWT authentication | feature/user-auth | main | 2025-01-15 | John Doe | 12345 | Implement user authentication | Backend Team |
| backend    | GitLab   | Fix README typo | fix/readme-typo | main | 2025-01-20 | Jane Smith | (empty) | (empty) | (empty) |
| frontend   | BitBucket | Update profile UI | feature/profile-redesign | develop | 2025-01-25 | Alice Johnson | 54321 | Redesign user profile page | Frontend Team |

---

## 常見問題 (FAQ)

### Q1: 為什麼 `workItem` 有時是 `null`?

**A**: 當 Pull Request 無法從 branch 名稱解析出 Work Item ID,或是該 PR 未關聯任何 Azure DevOps Work Item 時,`workItem` 欄位會是 JSON `null`。這是正常行為,表示「無關聯」。

### Q2: Repository 名稱為什麼變短了?

**A**: 系統會自動從完整路徑提取 repository 名稱。例如:
- 原始: `"acme-org/backend-api"`
- 提取後: `"backend-api"`

這讓 Google Sheets 顯示更簡潔。若需要完整路徑,請參考原始 PR URL。

### Q3: 如何區分相同名稱但不同平台的 repository?

**A**: 透過 `platform` 欄位區分。例如:
- `{ "repositoryName": "shared-lib", "platform": "GitLab" }`
- `{ "repositoryName": "shared-lib", "platform": "BitBucket" }`

在 Google Sheets 中,建議將 `repositoryName` 與 `platform` 組合為複合鍵。

### Q4: 舊的 Work Item-centric 格式還能用嗎?

**A**: **不行**。新版本完全移除了舊格式。若您依賴舊格式,請:
1. 在更新前備份現有的 JSON 檔案
2. 調整下游系統以支援新格式
3. 參考本文件的 Google Sheets 整合範例

### Q5: JSON 檔案可以手動編輯嗎?

**A**: 技術上可以,但**不建議**。JSON 結構必須符合 [JSON Schema](./contracts/repository-based-output-schema.json) 定義,否則可能導致下游工具錯誤。若需修改資料,建議:
1. 調整 `appsettings.json` 的組態設定
2. 重新執行 `sync` 命令產生新的 JSON

### Q6: 如何只匯出特定 repository?

**A**: 目前不支援匯出時過濾 repository。建議方法:
1. 執行完整同步
2. 在 Google Sheets 中使用 `FILTER` 函式過濾
3. 或使用 `jq` 工具過濾 JSON:
   ```bash
   jq '.repositories |= map(select(.repositoryName == "backend"))' output.json
   ```

---

## 效能與限制

### 效能指標

| 指標 | 數值 |
|------|------|
| 最大建議 Repository 數量 | 100 |
| 最大建議 PR 總數 | 2,000 |
| 預期匯出時間 (2,000 PRs) | < 1 秒 |
| JSON 檔案大小 (2,000 PRs) | ~2 MB |

### 已知限制

1. **無統計資訊**: 不自動計算 PR 總數、作者數量等,需在 Google Sheets 中使用公式計算
2. **無排序選項**: Repository 與 PR 保持原始順序,無法指定排序方式
3. **無過濾功能**: 匯出包含所有 repositories,無法指定只匯出特定專案
4. **Work Item 扁平化**: 不保留 Parent Work Item 層級結構,僅顯示直接關聯的 Work Item

---

## 故障排除

### 問題: 匯出的 JSON 檔案為空 (`repositories: []`)

**可能原因**:
- 指定日期區間內無任何 merged PR
- 所有平台同步失敗

**解決方法**:
1. 檢查日誌確認是否有錯誤訊息
2. 使用 `--verbose` 參數查看詳細日誌
3. 確認 API Token 設定正確 (`dotnet user-secrets list`)
4. 調整日期區間 (`-s` 與 `-e` 參數)

### 問題: `workItem` 應該有值但顯示 `null`

**可能原因**:
- Branch 名稱格式不符合 Work Item ID 解析規則
- Azure DevOps API 回傳錯誤
- Work Item 已刪除或無存取權限

**解決方法**:
1. 檢查 branch 名稱是否包含數字 ID (如 `123-feature-name`)
2. 確認 Azure DevOps Token 有讀取 Work Item 的權限
3. 查看日誌中的 Work Item 同步狀態

### 問題: JSON 格式錯誤,無法在 Google Sheets 中解析

**可能原因**:
- JSON 檔案被手動編輯損壞
- 特殊字元未正確跳脫

**解決方法**:
1. 使用 JSON validator 工具驗證格式 (如 [JSONLint](https://jsonlint.com/))
2. 使用 [JSON Schema Validator](https://www.jsonschemavalidator.net/) 驗證是否符合 schema
3. 重新執行 `sync` 命令產生新的 JSON 檔案

---

## 進階用法

### 使用 jq 工具分析 JSON

```bash
# 列出所有 repository 名稱
jq '.repositories[].repositoryName' output.json

# 統計每個 repository 的 PR 數量
jq '.repositories[] | {name: .repositoryName, count: (.pullRequests | length)}' output.json

# 找出特定作者的所有 PR
jq '.repositories[].pullRequests[] | select(.authorDisplayName == "John Doe")' output.json

# 匯出僅包含已合併 PR 的資料
jq '.repositories[].pullRequests |= map(select(.mergedAt != null))' output.json > merged-only.json
```

### 使用 PowerShell 分析 JSON

```powershell
# 讀取 JSON
$data = Get-Content output.json | ConvertFrom-Json

# 統計總 PR 數量
$totalPRs = ($data.repositories | ForEach-Object { $_.pullRequests.Count }) | Measure-Object -Sum
Write-Host "Total PRs: $($totalPRs.Sum)"

# 找出最活躍的 repository
$data.repositories | Sort-Object { $_.pullRequests.Count } -Descending | Select-Object -First 5

# 匯出為 CSV (扁平化)
$data.repositories | ForEach-Object {
    $repo = $_
    $repo.pullRequests | ForEach-Object {
        [PSCustomObject]@{
            Repository = $repo.repositoryName
            Platform = $repo.platform
            PRTitle = $_.pullRequestTitle
            Author = $_.authorDisplayName
            MergedAt = $_.mergedAt
            WorkItemId = $_.workItem.workItemId
        }
    }
} | Export-Csv -Path output.csv -NoTypeInformation
```

---

## 相關資源

- [Feature Specification (spec.md)](./spec.md)
- [Implementation Plan (plan.md)](./plan.md)
- [Data Model (data-model.md)](./data-model.md)
- [JSON Schema Contract](./contracts/repository-based-output-schema.json)
- [專案 README](../../../README.md)

---

## 回饋與支援

如有問題或建議,請:
- 提交 GitHub Issue: [ReleaseSync Issues](https://github.com/your-org/ReleaseSync/issues)
- 查看專案文件: `/docs` 目錄
- 聯絡開發團隊: [team@example.com](mailto:team@example.com)

**Last Updated**: 2025-11-15
