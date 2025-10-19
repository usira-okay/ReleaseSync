# ReleaseSync MVP 版本

這是 ReleaseSync 的最小可行產品 (MVP) 版本,展示了基本的命令列介面和 JSON 匯出功能。

## 已完成功能

### Phase 3 & 4 功能 (部分)

- [X] **T041-T043**: Console CLI 基礎架構
  - 使用 System.CommandLine 實作命令列介面
  - 支援時間範圍查詢參數
  - 支援平台啟用選項 (GitLab, BitBucket)
  
- [X] **T049-T052**: JSON 匯出功能
  - 實作 JsonFileExporter
  - 支援 --output-file 參數
  - 支援 --force 強制覆蓋
  - 檔案已存在時的錯誤處理

### MVP 特點

- 完整的命令列參數解析
- 結構化的 JSON 輸出格式
- 清晰的錯誤處理和日誌記錄
- 遵循 Clean Architecture 原則
- 完整的 DI 容器設定

## 專案結構

```
src/
├── ReleaseSync.Domain/          # Domain 層 (值物件、實體、介面)
├── ReleaseSync.Application/     # Application 層 (服務、DTO、Orchestrator)
├── ReleaseSync.Infrastructure/  # Infrastructure 層 (Repository 實作、平台服務)
└── ReleaseSync.Console/         # Console 層 (CLI 命令、Handler)
```

## 如何執行

### 1. 建置專案

```bash
dotnet build
```

### 2. 顯示幫助訊息

```bash
cd src/ReleaseSync.Console
dotnet run -- sync --help
```

### 3. 執行範例命令

```bash
# 基本執行 (無匯出)
dotnet run -- sync -s 2025-01-01 -e 2025-01-31 --gitlab --bitbucket

# 匯出為 JSON 檔案
dotnet run -- sync -s 2025-01-01 -e 2025-01-31 --gitlab -o output.json

# 強制覆蓋已存在的檔案
dotnet run -- sync -s 2025-01-01 -e 2025-01-31 --gitlab -o output.json -f
```

### 4. 查看 JSON 輸出

生成的 JSON 檔案格式如下:

```json
{
  "syncStartedAt": "2025-10-18T13:34:06.9329571Z",
  "syncCompletedAt": "2025-10-18T13:34:06.9330146Z",
  "startDate": "2025-01-01T00:00:00",
  "endDate": "2025-01-31T00:00:00",
  "isFullySuccessful": false,
  "isPartiallySuccessful": false,
  "totalPullRequestCount": 0,
  "linkedWorkItemCount": 0,
  "pullRequests": [],
  "platformStatuses": []
}
```

## 命令列參數

| 參數 | 縮寫 | 必填 | 說明 |
|------|------|------|------|
| `--start-date` | `-s` | 是 | 查詢起始日期 (包含) |
| `--end-date` | `-e` | 是 | 查詢結束日期 (包含) |
| `--enable-gitlab` | `--gitlab` | 否 | 啟用 GitLab 平台 |
| `--enable-bitbucket` | `--bitbucket` | 否 | 啟用 BitBucket 平台 |
| `--output-file` | `-o` | 否 | JSON 匯出檔案路徑 |
| `--force` | `-f` | 否 | 強制覆蓋已存在的輸出檔案 |
| `--verbose` | `-v` | 否 | 啟用詳細日誌輸出 |

## MVP 限制

目前 MVP 版本的限制:

1. **無實際平台整合**: GitLab 和 BitBucket 服務為 stub 實作,不會真正抓取資料
2. **無 Azure DevOps 整合**: Work Item 功能尚未實作
3. **無單元測試**: 測試專案尚未完成
4. **無整合測試**: 整合測試尚未完成

## 下一步開發

依照 tasks.md 的規劃,接下來應實作:

1. **T027-T030**: 實作平台 API Contract Tests
2. **T031-T038**: 實作 GitLab 和 BitBucket 服務
3. **T039-T040**: 完善 SyncOrchestrator 實作
4. **T044-T046**: 實作整合測試
5. **T047-T048, T053**: 實作 JSON 匯出測試

## 技術堆疊

- .NET 8.0 / .NET 9.0
- System.CommandLine (CLI 框架)
- Serilog (日誌記錄)
- Microsoft.Extensions.DependencyInjection (DI 容器)
- System.Text.Json (JSON 序列化)

## 專案檔案位置

- 命令定義: `/src/ReleaseSync.Console/Commands/SyncCommand.cs`
- 命令處理器: `/src/ReleaseSync.Console/Handlers/SyncCommandHandler.cs`
- 程式進入點: `/src/ReleaseSync.Console/Program.cs`
- JSON 匯出器: `/src/ReleaseSync.Application/Exporters/JsonFileExporter.cs`
- 同步協調器: `/src/ReleaseSync.Application/Services/SyncOrchestrator.cs`
- DTO 定義: `/src/ReleaseSync.Application/DTOs/SyncResultDto.cs`

## 已修正問題

1. 修正專案間的相依性參照
2. 實作 SyncResultDto.FromDomain 轉換方法
3. 實作 IResultExporter.FileExists 方法
4. 修正 XML 文件註解缺失
5. 修正 System.CommandLine SetHandler 用法

---

**建立日期**: 2025-10-18
**版本**: MVP 0.1.0
**狀態**: 可運行,但功能有限 (Stub 實作)
