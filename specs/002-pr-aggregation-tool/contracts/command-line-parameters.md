# Command Line Parameters Contract

**Feature**: 002-pr-aggregation-tool
**Date**: 2025-10-18

本文件定義 Console 應用程式的命令列參數結構,使用 `System.CommandLine` 實作。

---

## 命令結構

### 主命令: sync

**用途**: 同步指定時間範圍內的 PR/MR 資訊,並可選擇性地抓取關聯的 Work Item 資訊。

**語法**:
```bash
ReleaseSync.exe sync [options]
```

---

## 選項 (Options)

### --start-date (必填)

**說明**: 查詢起始日期 (包含)

**類型**: `DateTime`

**格式**: ISO 8601 (`yyyy-MM-dd` 或 `yyyy-MM-ddTHH:mm:ss`)

**範例**:
```bash
--start-date 2025-01-01
--start-date "2025-01-01T00:00:00"
```

**別名**: `-s`

---

### --end-date (必填)

**說明**: 查詢結束日期 (包含)

**類型**: `DateTime`

**格式**: ISO 8601 (`yyyy-MM-dd` 或 `yyyy-MM-ddTHH:mm:ss`)

**範例**:
```bash
--end-date 2025-01-31
--end-date "2025-01-31T23:59:59"
```

**別名**: `-e`

**驗證**: 必須 >= `--start-date`

---

### --enable-gitlab (選填)

**說明**: 啟用 GitLab 平台的 PR/MR 抓取功能

**類型**: `bool`

**預設值**: `false`

**範例**:
```bash
--enable-gitlab
--enable-gitlab true
--enable-gitlab false
```

**別名**: `--gitlab`

---

### --enable-bitbucket (選填)

**說明**: 啟用 BitBucket 平台的 PR/MR 抓取功能

**類型**: `bool`

**預設值**: `false`

**範例**:
```bash
--enable-bitbucket
--enable-bitbucket true
```

**別名**: `--bitbucket`

---

### --enable-azure-devops (選填)

**說明**: 啟用 Azure DevOps Work Item 整合功能

**類型**: `bool`

**預設值**: `false`

**範例**:
```bash
--enable-azure-devops
--enable-azure-devops true
```

**別名**: `--azure`, `--ado`

**注意**: 啟用後會從 Branch 名稱解析 Work Item ID 並抓取對應資訊

---

### --output-file (選填)

**說明**: JSON 匯出檔案路徑,若未指定則僅在 Console 顯示結果

**類型**: `string`

**預設值**: `null` (不匯出檔案)

**範例**:
```bash
--output-file ./output/sync-result.json
--output-file "C:\Reports\pr-sync.json"
```

**別名**: `-o`

**驗證**:
- 若路徑不存在,自動建立目錄
- 若檔案已存在,詢問是否覆蓋 (或使用 `--force` 強制覆蓋)

---

### --force (選填)

**說明**: 強制覆蓋已存在的輸出檔案,不詢問確認

**類型**: `bool`

**預設值**: `false`

**範例**:
```bash
--force
```

**別名**: `-f`

---

### --verbose (選填)

**說明**: 啟用詳細日誌輸出 (Debug 等級)

**類型**: `bool`

**預設值**: `false`

**範例**:
```bash
--verbose
```

**別名**: `-v`

---

## 使用範例

### 範例 1: 抓取 GitLab 單一平台資料

```bash
ReleaseSync.exe sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab \
  --output-file ./output/gitlab-sync.json
```

### 範例 2: 同時抓取 GitLab 與 BitBucket 資料

```bash
ReleaseSync.exe sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  --bitbucket \
  -o ./sync-result.json
```

### 範例 3: 啟用 Azure DevOps Work Item 整合

```bash
ReleaseSync.exe sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab \
  --enable-azure-devops \
  --output-file ./output/full-sync.json \
  --verbose
```

### 範例 4: 僅顯示結果而不匯出檔案

```bash
ReleaseSync.exe sync \
  --start-date 2025-01-01 \
  --end-date 2025-01-31 \
  --enable-gitlab
```

### 範例 5: 強制覆蓋已存在的檔案

```bash
ReleaseSync.exe sync \
  -s 2025-01-01 \
  -e 2025-01-31 \
  --gitlab \
  -o ./sync-result.json \
  --force
```

---

## 驗證規則

### 必要參數驗證

- `--start-date` 與 `--end-date` 為必填
- 至少須啟用一個平台 (`--enable-gitlab` 或 `--enable-bitbucket`)

### 日期驗證

- `--start-date` 必須 <= `--end-date`
- 日期格式必須符合 ISO 8601

### 平台驗證

- 若啟用 GitLab 但組態檔缺少 GitLab 設定,顯示錯誤訊息
- 若啟用 BitBucket 但組態檔缺少 BitBucket 設定,顯示錯誤訊息
- 若啟用 Azure DevOps 但組態檔缺少 Azure DevOps 設定,顯示錯誤訊息

### 檔案輸出驗證

- 若 `--output-file` 目錄不存在,自動建立
- 若檔案已存在且未使用 `--force`,詢問使用者確認

---

## 錯誤訊息範例

### 缺少必要參數

```
錯誤: 缺少必要參數 '--start-date'
使用方式: ReleaseSync.exe sync --start-date <date> --end-date <date> [options]
```

### 未啟用任何平台

```
錯誤: 至少須啟用一個平台 (--enable-gitlab 或 --enable-bitbucket)
```

### 日期範圍無效

```
錯誤: 起始日期 (2025-02-01) 不能晚於結束日期 (2025-01-31)
```

### 組態檔缺少設定

```
錯誤: 已啟用 GitLab 但 appsettings.json 中缺少 GitLab 設定
請檢查組態檔或停用此平台。
```

### 檔案已存在

```
警告: 檔案已存在: ./sync-result.json
是否覆蓋? [y/N]: _
```

---

## 實作參考 (C# System.CommandLine)

```csharp
using System.CommandLine;

var rootCommand = new RootCommand("ReleaseSync - PR/MR 變更資訊聚合工具");

var syncCommand = new Command("sync", "同步 PR/MR 變更資訊");

// 必填選項
var startDateOption = new Option<DateTime>(
    aliases: new[] { "--start-date", "-s" },
    description: "查詢起始日期 (包含)"
) { IsRequired = true };

var endDateOption = new Option<DateTime>(
    aliases: new[] { "--end-date", "-e" },
    description: "查詢結束日期 (包含)"
) { IsRequired = true };

// 選填選項
var enableGitLabOption = new Option<bool>(
    aliases: new[] { "--enable-gitlab", "--gitlab" },
    description: "啟用 GitLab 平台的 PR/MR 抓取功能",
    getDefaultValue: () => false
);

var enableBitBucketOption = new Option<bool>(
    aliases: new[] { "--enable-bitbucket", "--bitbucket" },
    description: "啟用 BitBucket 平台的 PR/MR 抓取功能",
    getDefaultValue: () => false
);

var enableAzureDevOpsOption = new Option<bool>(
    aliases: new[] { "--enable-azure-devops", "--azure", "--ado" },
    description: "啟用 Azure DevOps Work Item 整合功能",
    getDefaultValue: () => false
);

var outputFileOption = new Option<string>(
    aliases: new[] { "--output-file", "-o" },
    description: "JSON 匯出檔案路徑"
);

var forceOption = new Option<bool>(
    aliases: new[] { "--force", "-f" },
    description: "強制覆蓋已存在的輸出檔案",
    getDefaultValue: () => false
);

var verboseOption = new Option<bool>(
    aliases: new[] { "--verbose", "-v" },
    description: "啟用詳細日誌輸出",
    getDefaultValue: () => false
);

// 新增選項至命令
syncCommand.AddOption(startDateOption);
syncCommand.AddOption(endDateOption);
syncCommand.AddOption(enableGitLabOption);
syncCommand.AddOption(enableBitBucketOption);
syncCommand.AddOption(enableAzureDevOpsOption);
syncCommand.AddOption(outputFileOption);
syncCommand.AddOption(forceOption);
syncCommand.AddOption(verboseOption);

// 設定處理器
syncCommand.SetHandler(async (context) =>
{
    var startDate = context.ParseResult.GetValueForOption(startDateOption);
    var endDate = context.ParseResult.GetValueForOption(endDateOption);
    var enableGitLab = context.ParseResult.GetValueForOption(enableGitLabOption);
    // ... 取得其他選項

    // 驗證邏輯
    if (startDate > endDate)
    {
        Console.Error.WriteLine($"錯誤: 起始日期 ({startDate:yyyy-MM-dd}) 不能晚於結束日期 ({endDate:yyyy-MM-dd})");
        context.ExitCode = 1;
        return;
    }

    if (!enableGitLab && !enableBitBucket)
    {
        Console.Error.WriteLine("錯誤: 至少須啟用一個平台 (--enable-gitlab 或 --enable-bitbucket)");
        context.ExitCode = 1;
        return;
    }

    // 呼叫 Application Service
    // ...
});

rootCommand.AddCommand(syncCommand);
return await rootCommand.InvokeAsync(args);
```

---

## 版本資訊

**Version**: 1.0
**Last Updated**: 2025-10-18

---

**下一步**: 定義 appsettings.json 與 appsettings.secure.json 的 JSON Schema
