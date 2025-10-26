# 快速開始: 資料過濾機制設定指南

**Feature**: 003-filter-unmapped-data
**Date**: 2025-10-25
**Audience**: 開發者和系統管理員

## 概述

本指南說明如何設定和使用資料過濾機制,包括:
- 過濾未在 UserMapping 中定義的 PR/MR 作者
- 過濾未在 TeamMapping 中定義的 Work Item 團隊

## 前置需求

- ReleaseSync 已安裝並可正常執行
- 已設定 GitLab、BitBucket 或 Azure DevOps 存取權限
- 熟悉 JSON 配置檔格式

## 設定步驟

### 1. 設定 UserMapping 過濾 (選用)

如果您希望只收錄特定團隊成員的 PR/MR,請在 `appsettings.json` 中設定 UserMapping。

**範例: 設定三位團隊成員**

```json
{
  "UserMapping": [
    {
      "GitLabUserId": "john.doe",
      "BitBucketUserId": "jdoe",
      "DisplayName": "John Doe"
    },
    {
      "GitLabUserId": "jane.smith",
      "BitBucketUserId": "jsmith",
      "DisplayName": "Jane Smith"
    },
    {
      "GitLabUserId": "bob.chen",
      "BitBucketUserId": "bchen",
      "DisplayName": "Bob Chen"
    }
  ]
}
```

**說明**:
- `GitLabUserId`: GitLab 平台的使用者名稱
- `BitBucketUserId`: BitBucket 平台的使用者名稱
- `DisplayName`: 在報告中顯示的統一名稱

**注意事項**:
- 如果使用者只在一個平台上,另一個平台的 ID 可以省略或設為 `null`
- 使用者名稱的比對不區分大小寫
- **如果 UserMapping 為空 (或未設定),系統將收錄所有 PR/MR (向後相容)**

---

### 2. 設定 TeamMapping 過濾 (選用)

如果您希望只收錄特定團隊的 Work Item,請在 `appsettings.json` 的 `AzureDevOps` 區段中設定 TeamMapping。

**範例: 設定三個團隊**

```json
{
  "AzureDevOps": {
    "OrganizationUrl": "https://dev.azure.com/myorganization",
    "ProjectName": "MyProject",
    "WorkItemIdPatterns": [
      {
        "Name": "VSTS Pattern",
        "Regex": "vsts(\\d+)",
        "IgnoreCase": true,
        "CaptureGroup": 1
      }
    ],
    "ParsingBehavior": {
      "OnParseFailure": "LogWarningAndContinue",
      "StopOnFirstMatch": true
    },
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
}
```

**說明**:
- `OriginalTeamName`: Azure DevOps Work Item 中的原始團隊名稱 (來自 `System.AreaPath`)
- `DisplayName`: 在報告中顯示的團隊名稱

**如何確定 OriginalTeamName**:

1. 在 Azure DevOps 中開啟任一 Work Item
2. 查看 `Area Path` 欄位,格式通常為: `ProjectName\TeamName\SubArea`
3. 提取其中的 `TeamName` 部分作為 `OriginalTeamName`

**範例 Area Path**:
- `MyProject\MoneyLogistic\Backend` → `OriginalTeamName: "MoneyLogistic"`
- `MyProject\DailyResource` → `OriginalTeamName: "DailyResource"`

**注意事項**:
- 團隊名稱的比對不區分大小寫
- **如果 TeamMapping 為空 (或未設定),系統將收錄所有 Work Item (向後相容)**

---

### 3. 驗證設定

設定完成後,執行 ReleaseSync 並檢查日誌輸出:

```bash
dotnet run --project src/ReleaseSync.Console -- sync
```

**預期日誌範例**:

```
[Information] Filtered 5 PRs out of 20 due to UserMapping (15 retained)
[Information] Filtered 3 Work Items out of 12 due to TeamMapping (9 retained)
[Information] Sync completed successfully
```

如果看到以下警告,表示 Mapping 為空,過濾功能未啟用:

```
[Warning] UserMapping is empty, all PRs will be included (backward compatible mode)
[Warning] TeamMapping is empty, all Work Items will be included (backward compatible mode)
```

---

## 使用情境

### 情境 1: 只追蹤特定開發團隊的 PR/MR

**需求**: 您的 GitLab 專案有外部協作者和機器人帳號,但您只想追蹤內部團隊成員的 PR。

**解決方案**:
1. 在 UserMapping 中列出所有內部團隊成員
2. 執行 sync 命令
3. 系統只會收錄清單中成員的 PR,外部協作者和機器人的 PR 會被過濾掉

**優點**:
- 報告更清晰,只包含團隊成員的貢獻
- 避免外部 PR 干擾統計
- 機器人 PR (如 Dependabot) 不會出現在報告中

---

### 情境 2: 只追蹤特定團隊的 Work Item

**需求**: 您的 Azure DevOps 專案包含多個團隊,但您只負責其中 3 個團隊的發版管理。

**解決方案**:
1. 確認這 3 個團隊的 Area Path
2. 在 TeamMapping 中設定這 3 個團隊
3. 執行 sync 命令
4. 系統只會收錄這 3 個團隊的 Work Item

**優點**:
- 發版報告只包含您負責的團隊範圍
- 避免其他團隊的 Work Item 混入
- 團隊名稱顯示為易讀的中文名稱

---

### 情境 3: 混合過濾 (同時過濾 PR 和 Work Item)

**需求**: 您希望報告只包含特定團隊成員的 PR,並且只關聯特定團隊的 Work Item。

**解決方案**:
1. 設定 UserMapping 列出團隊成員
2. 設定 TeamMapping 列出關注的團隊
3. 執行 sync 命令

**結果**:
- 只收錄 UserMapping 中成員的 PR/MR
- 只收錄 TeamMapping 中團隊的 Work Item
- 如果某個 PR 關聯的 Work Item 被過濾掉,PR 仍會保留,但 Work Item 清單為空

---

## 關聯資料處理

### Work Item 被過濾時的行為

根據 FR-012,當 Work Item 因為團隊不在 TeamMapping 中而被過濾掉時:

- ✅ **PR/MR 仍然保留** (不會因為 Work Item 被過濾而消失)
- ✅ **Work Item 關聯顯示為空** (在 JSON 輸出中為空陣列 `[]`)
- ✅ **日誌會記錄警告** 說明有 Work Item 被過濾

**JSON 輸出範例**:

```json
{
  "pullRequests": [
    {
      "id": "123",
      "title": "Add new feature",
      "author": "John Doe",
      "workItems": [
        {
          "id": "5678",
          "title": "Implement feature X",
          "team": "金流團隊"
        }
      ]
    },
    {
      "id": "124",
      "title": "Fix bug",
      "author": "Jane Smith",
      "workItems": []  // Work Item 被 TeamMapping 過濾掉
    }
  ]
}
```

**日誌範例**:

```
[Warning] All Work Items for PR 124 were filtered out by TeamMapping (OriginalTeamName: 'OtherTeam' not in mapping)
```

---

## 疑難排解

### 問題 1: 設定後所有 PR 都被過濾掉

**可能原因**:
- UserMapping 中的使用者名稱與實際的 PR 作者不符
- 大小寫拼寫錯誤 (雖然比對不區分大小寫,但建議檢查)

**解決方法**:
1. 檢查日誌,確認被過濾的 PR 作者名稱
2. 對照 GitLab/BitBucket 上的實際使用者名稱
3. 更新 UserMapping 設定
4. 重新執行 sync 命令

---

### 問題 2: 設定 TeamMapping 後沒有效果

**可能原因**:
- `OriginalTeamName` 與 Work Item 的實際團隊名稱不符
- Area Path 格式與預期不同

**解決方法**:
1. 在 Azure DevOps 中開啟幾個 Work Item,記錄它們的 Area Path
2. 檢查日誌,確認系統提取的團隊名稱
3. 更新 TeamMapping 設定使其與實際 Area Path 一致
4. 如果 Area Path 格式特殊,請參考進階設定文件

---

### 問題 3: 不確定是否啟用過濾功能

**解決方法**:

檢查日誌輸出:

- **未啟用過濾** (向後相容模式):
  ```
  [Warning] UserMapping is empty, all PRs will be included
  [Warning] TeamMapping is empty, all Work Items will be included
  ```

- **已啟用過濾**:
  ```
  [Information] Filtered X PRs out of Y due to UserMapping
  [Information] Filtered X Work Items out of Y due to TeamMapping
  ```

---

## 進階設定

### 只設定 UserMapping (不過濾 Work Item)

如果您只想過濾 PR/MR 作者,不想過濾 Work Item:

```json
{
  "UserMapping": [
    { "GitLabUserId": "john", "DisplayName": "John Doe" }
  ],
  "AzureDevOps": {
    "TeamMapping": []  // 空陣列或完全省略此屬性
  }
}
```

### 只設定 TeamMapping (不過濾 PR/MR)

如果您只想過濾 Work Item,不想過濾 PR/MR:

```json
{
  "UserMapping": [],  // 空陣列或完全省略此屬性
  "AzureDevOps": {
    "TeamMapping": [
      { "OriginalTeamName": "TeamA", "DisplayName": "團隊 A" }
    ]
  }
}
```

---

## 效能考量

- **UserMapping**: 支援數百筆對應,使用 HashSet 快速查找 (O(1))
- **TeamMapping**: 支援數十筆對應,使用 HashSet 快速查找 (O(1))
- **效能影響**: 過濾邏輯對整體執行時間的影響 <5%
- **記憶體使用**: 100 筆 Mapping 約 <10KB 記憶體

---

## 相關文件

- [功能規格 (spec.md)](./spec.md) - 詳細的功能需求和接受標準
- [實作計畫 (plan.md)](./plan.md) - 技術架構和實作細節
- [資料模型 (data-model.md)](./data-model.md) - 資料結構定義
- [研究文件 (research.md)](./research.md) - 技術決策和最佳實踐

---

## 變更歷史

- **2025-10-25**: 初版發布,包含 UserMapping 和 TeamMapping 過濾功能
