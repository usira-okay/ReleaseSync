# Quickstart: Release Branch 差異比對功能

**Feature Branch**: `001-release-branch-diff`
**Date**: 2026-01-20

## 快速開始

本指南說明如何設定與使用 Release Branch 差異比對功能。

---

## 1. 配置設定

### 1.1 全域設定 (SyncOptions)

在 `appsettings.json` 根層級新增 `SyncOptions` 節點：

```json
{
  "SyncOptions": {
    "ReleaseBranch": "release/20260120",
    "StartDate": "2026-01-13",
    "EndDate": "2026-01-20",
    "DefaultFetchMode": "DateRange"
  }
}
```

| 屬性 | 說明 | 必填 |
|------|------|------|
| `ReleaseBranch` | 預設的 Release Branch 名稱 | 否 |
| `StartDate` | 預設的開始日期 | 否 |
| `EndDate` | 預設的結束日期 | 否 |
| `DefaultFetchMode` | 預設的抓取模式 (`DateRange` 或 `ReleaseBranch`) | 否 (預設 `DateRange`) |

### 1.2 GitLab 專案設定

```json
{
  "GitLab": {
    "ApiUrl": "https://gitlab.app.com/api/v4",
    "Projects": [
      {
        "ProjectPath": "payment/payment.adminapi",
        "TargetBranch": "master",
        "FetchMode": "ReleaseBranch",
        "ReleaseBranch": "release/20260118"
      },
      {
        "ProjectPath": "payment/payment.management",
        "TargetBranch": "master",
        "FetchMode": "DateRange",
        "StartDate": "2026-01-10",
        "EndDate": "2026-01-17"
      }
    ]
  }
}
```

### 1.3 BitBucket 專案設定

```json
{
  "BitBucket": {
    "ApiUrl": "https://api.bitbucket.org/2.0",
    "Projects": [
      {
        "WorkspaceAndRepo": "store/webstore.webmall",
        "TargetBranch": "develop",
        "FetchMode": "ReleaseBranch"
      }
    ]
  }
}
```

---

## 2. 配置說明

### 2.1 FetchMode 抓取模式

| 模式 | 說明 | 使用情境 |
|------|------|----------|
| `DateRange` | 使用時間範圍抓取 PR | 傳統模式，向後相容 |
| `ReleaseBranch` | 使用 Release Branch 比對 | 週更新開發週期 |

### 2.2 配置覆寫優先權

**Repository 層級 > 全域設定**

例如：
- 全域設定 `ReleaseBranch = "release/20260120"`
- 專案設定 `ReleaseBranch = "release/20260118"`
- 該專案將使用 `release/20260118`

### 2.3 Release Branch 比對邏輯

#### 情境 1：最新版 Release Branch

當設定的 Release Branch 為最新版本時：

```
比對對象：Release Branch ↔ TargetBranch
取得差異：TargetBranch 有，但 Release Branch 沒有的 commit
```

#### 情境 2：歷史版 Release Branch

當設定的 Release Branch 為舊版本時：

```
比對對象：當前 Release Branch ↔ 下一版 Release Branch
取得差異：下一版有，但當前版本沒有的 commit
```

---

## 3. 從舊版遷移

### 3.1 TargetBranches → TargetBranch

舊版配置：
```json
{
  "ProjectPath": "mygroup/myproject",
  "TargetBranches": ["master", "develop"]
}
```

新版配置：
```json
{
  "ProjectPath": "mygroup/myproject",
  "TargetBranch": "master"
}
```

> **注意**: 如果需要同時追蹤多個分支，請建立多個專案配置。

### 3.2 向後相容

- 舊版 `TargetBranches` 陣列暫時仍可使用（標記為 deprecated）
- 同時存在時，`TargetBranch` 優先於 `TargetBranches`
- 建議儘早遷移至新版配置

---

## 4. 使用範例

### 4.1 使用時間範圍抓取

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  -s 2026-01-13 \
  -e 2026-01-20 \
  --gitlab \
  -o output.json
```

### 4.2 使用 Release Branch 抓取

配置檔設定 `FetchMode: "ReleaseBranch"` 後：

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --gitlab \
  -o output.json
```

### 4.3 命令列覆寫 Release Branch

```bash
dotnet run --project src/ReleaseSync.Console -- sync \
  --release-branch release/20260120 \
  --gitlab \
  -o output.json
```

---

## 5. 錯誤處理

### 5.1 Release Branch 不存在

**錯誤訊息**:
```
Release branch 'release/20260120' not found in repository 'mygroup/myproject'.
Available release branches: release/20260113, release/20260106
```

**解決方式**:
- 確認 Release Branch 名稱正確
- 確認 Release Branch 已推送至遠端

### 5.2 配置驗證錯誤

**錯誤訊息**:
```
ReleaseBranch is required when FetchMode is ReleaseBranch
```

**解決方式**:
- 設定全域 `SyncOptions.ReleaseBranch`
- 或在專案層級設定 `ReleaseBranch`

### 5.3 Release Branch 格式錯誤

**錯誤訊息**:
```
Invalid release branch format. Expected 'release/yyyyMMdd', got 'release-20260120'
```

**解決方式**:
- 使用正確格式：`release/20260120`

---

## 6. 完整配置範例

```json
{
  "SyncOptions": {
    "ReleaseBranch": "release/20260120",
    "StartDate": "2026-01-13",
    "EndDate": "2026-01-20",
    "DefaultFetchMode": "DateRange"
  },
  "GitLab": {
    "ApiUrl": "https://gitlab.app.com/api/v4",
    "Projects": [
      {
        "ProjectPath": "payment/payment.adminapi",
        "TargetBranch": "master",
        "FetchMode": "ReleaseBranch",
        "ReleaseBranch": "release/20260118"
      },
      {
        "ProjectPath": "payment/payment.management",
        "TargetBranch": "master",
        "FetchMode": "DateRange",
        "StartDate": "2026-01-10",
        "EndDate": "2026-01-17"
      },
      {
        "ProjectPath": "payment/payment.api",
        "TargetBranch": "main"
      }
    ]
  },
  "BitBucket": {
    "ApiUrl": "https://api.bitbucket.org/2.0",
    "Projects": [
      {
        "WorkspaceAndRepo": "store/webstore.webmall",
        "TargetBranch": "develop",
        "FetchMode": "ReleaseBranch"
      }
    ]
  }
}
```

---

## 7. 常見問題

### Q: 如何判斷 Release Branch 是否為最新版？

A: 系統會自動列出所有 `release/*` 分支，解析日期後進行排序。如果設定的 Release Branch 日期為最大值，則視為最新版。

### Q: 如果找不到下一版 Release Branch 怎麼辦？

A: 系統會將其視為最新版，並比對與 TargetBranch 的差異。

### Q: 可以使用其他 Release Branch 命名格式嗎？

A: 目前僅支援 `release/yyyyMMdd` 格式。如有其他需求，請提出功能請求。
