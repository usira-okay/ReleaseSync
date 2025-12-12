// <copyright file="GoogleSheetApiClient.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using ReleaseSync.Application.Configuration;
using ReleaseSync.Application.Exceptions;
using ReleaseSync.Application.Models;
using ReleaseSync.Application.Services;

namespace ReleaseSync.Infrastructure.GoogleSheet;

/// <summary>
/// Google Sheets API 客戶端實作。
/// 使用 Service Account 憑證驗證並與 Google Sheets API 互動。
/// </summary>
public class GoogleSheetApiClient : IGoogleSheetApiClient, IDisposable
{
    private readonly GoogleSheetSettings _settings;
    private readonly IGoogleSheetRowParser _rowParser;
    private readonly ILogger<GoogleSheetApiClient> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private SheetsService? _sheetsService;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="GoogleSheetApiClient"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">Google Sheet 設定。</param>
    /// <param name="rowParser">Row 解析器。</param>
    /// <param name="logger">日誌記錄器。</param>
    public GoogleSheetApiClient(
        IOptions<GoogleSheetSettings> settings,
        IGoogleSheetRowParser rowParser,
        ILogger<GoogleSheetApiClient> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _rowParser = rowParser ?? throw new ArgumentNullException(nameof(rowParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 建立 Polly retry 策略
        _retryPolicy = Policy
            .Handle<Google.GoogleApiException>(ex => ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                _settings.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(_settings.RetryWaitSeconds),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Google Sheets API 速率限制，重試 {RetryCount}/{MaxRetries}，等待 {WaitSeconds} 秒",
                        retryCount,
                        _settings.RetryCount,
                        timeSpan.TotalSeconds);
                });
    }

    /// <inheritdoc/>
    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ServiceAccountCredentialPath))
        {
            throw new GoogleSheetConfigurationException("Service Account 憑證路徑未設定。請在 appsettings.json 或 User Secrets 中設定 GoogleSheet:ServiceAccountCredentialPath。");
        }

        if (!File.Exists(_settings.ServiceAccountCredentialPath))
        {
            throw new GoogleSheetCredentialNotFoundException(_settings.ServiceAccountCredentialPath);
        }

        try
        {
            _logger.LogInformation("正在驗證 Service Account 憑證...");

            GoogleCredential credential;
            using (var stream = new FileStream(_settings.ServiceAccountCredentialPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            _sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ReleaseSync",
            });

            _logger.LogInformation("Service Account 憑證驗證成功");
        }
        catch (Exception ex) when (ex is not GoogleSheetException)
        {
            throw new GoogleSheetAuthenticationException($"Service Account 憑證驗證失敗: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SpreadsheetExistsAsync(string spreadsheetId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = _sheetsService!.Spreadsheets.Get(spreadsheetId);
                await request.ExecuteAsync(cancellationToken);
            });

            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new GoogleSheetPermissionDeniedException(spreadsheetId);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SheetExistsAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            var spreadsheet = await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = _sheetsService!.Spreadsheets.Get(spreadsheetId);
                return await request.ExecuteAsync(cancellationToken);
            });

            return spreadsheet.Sheets.Any(s => s.Properties.Title == sheetName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IList<IList<object>>> ReadSheetDataAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        _logger.LogInformation("正在讀取工作表資料: {SheetName}", sheetName);

        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var range = $"{sheetName}!A:ZZ"; // 讀取所有欄位
            var request = _sheetsService!.Spreadsheets.Values.Get(spreadsheetId, range);
            return await request.ExecuteAsync(cancellationToken);
        });

        var result = response.Values ?? new List<IList<object>>();
        _logger.LogInformation("讀取工作表資料完成: {RowCount} rows", result.Count);

        return result;
    }

    /// <inheritdoc/>
    public async Task<int> BatchUpdateAsync(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<SheetSyncOperation> operations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (operations.Count == 0)
        {
            _logger.LogInformation("無同步操作需要執行");
            return 0;
        }

        _logger.LogInformation("正在執行批次更新: {OperationCount} 個操作", operations.Count);

        // 分離 Insert 和 Update 操作
        var insertOperations = operations.Where(op => op.OperationType == SheetOperationType.Insert)
                                          .OrderBy(op => op.TargetRowNumber) // 從小到大排序
                                          .ToList();
        var updateOperations = operations.Where(op => op.OperationType == SheetOperationType.Update).ToList();

        // 再執行更新操作
        if (updateOperations.Count > 0)
        {
            await UpdateExistingRowsAsync(spreadsheetId, sheetName, updateOperations, columnMapping, cancellationToken);
        }

        // 先執行插入操作（按行數由小到大，確保插入順序正確）
        if (insertOperations.Count > 0)
        {
            await InsertRowsAsync(spreadsheetId, sheetName, insertOperations, columnMapping, cancellationToken);
        }

        _logger.LogInformation("批次更新完成: {InsertCount} 新增, {UpdateCount} 更新", insertOperations.Count, updateOperations.Count);
        return operations.Count;
    }

    /// <summary>
    /// 插入新 rows（在指定位置插入並下推現有資料）。
    /// </summary>
    private async Task InsertRowsAsync(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<SheetSyncOperation> insertOperations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在批次插入 {Count} 個新 rows", insertOperations.Count);

        // 取得 SheetId (使用傳入的 sheetName)
        var sheetId = await GetSheetIdAsync(spreadsheetId, sheetName, cancellationToken);

        // 行數小的先插入
        var sortedOperations = insertOperations.OrderBy(op => op.TargetRowNumber).ToList();

        var requests = new List<Request>();

        // 1. 批次插入空白行（從後往前）
        foreach (var operation in sortedOperations)
        {
            requests.Add(new Request
            {
                InsertDimension = new InsertDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = operation.TargetRowNumber - 1, // 0-based index
                        EndIndex = operation.TargetRowNumber, // 插入 1 行
                    },
                    InheritFromBefore = false,
                },
            });
        }

        // 2. 批次更新插入的 rows 資料
        foreach (var operation in sortedOperations)
        {
            var rowValues = _rowParser.ToRowValues(operation.RowData, columnMapping);
            var cellDataList = new List<CellData>();

            for (int i = 0; i < rowValues.Count; i++)
            {
                // 檢查是否為 Feature 欄位且有 FeatureUrl
                if (IsFeatureColumn(i, columnMapping) &&
                    !string.IsNullOrWhiteSpace(operation.RowData.FeatureUrl))
                {
                    // 建立超連結儲存格
                    cellDataList.Add(CreateHyperlinkCell(
                        operation.RowData.Feature,
                        operation.RowData.FeatureUrl));
                }
                else if (IsAutoSyncColumn(i, columnMapping))
                {
                    // AutoSync 欄位使用布林值
                    cellDataList.Add(new CellData
                    {
                        UserEnteredValue = new ExtendedValue
                        {
                            BoolValue = operation.RowData.IsAutoSync,
                        },
                    });
                }
                else
                {
                    // 一般儲存格
                    cellDataList.Add(new CellData
                    {
                        UserEnteredValue = new ExtendedValue
                        {
                            StringValue = rowValues[i]?.ToString(),
                        },
                    });
                }
            }

            var rowData = new RowData
            {
                Values = cellDataList,
            };

            requests.Add(new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = operation.TargetRowNumber - 1, // 0-based index
                        EndRowIndex = operation.TargetRowNumber,
                        StartColumnIndex = 0,
                        EndColumnIndex = rowValues.Count,
                    },
                    Rows = new List<RowData> { rowData },
                    Fields = "userEnteredValue",
                },
            });
        }

        // 3. 執行批次更新（一次 API 呼叫完成所有插入與資料填充）
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            Requests = requests,
        };

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var request = _sheetsService!.Spreadsheets.BatchUpdate(batchUpdateRequest, spreadsheetId);
            await request.ExecuteAsync(cancellationToken);
        });

        _logger.LogInformation("批次插入 {Count} 個新 rows 完成", insertOperations.Count);
    }

    /// <summary>
    /// 更新現有 rows (更新 AuthorsColumn、PullRequestUrlsColumn 和 MergedAtColumn)。
    /// </summary>
    private async Task UpdateExistingRowsAsync(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<SheetSyncOperation> updateOperations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在更新 {Count} 個現有 rows (更新 Authors、PR URLs 和 MergedAt 欄位)", updateOperations.Count);

        // 取得 SheetId (使用傳入的 sheetName)
        var sheetId = await GetSheetIdAsync(spreadsheetId, sheetName, cancellationToken);
        var requests = new List<Request>();

        // 取得各欄位的索引
        var authorsColumnIndex = ColumnLetterToIndex(columnMapping.AuthorsColumn);
        var prUrlsColumnIndex = ColumnLetterToIndex(columnMapping.PullRequestUrlsColumn);
        var mergedAtColumnIndex = ColumnLetterToIndex(columnMapping.MergedAtColumn);

        foreach (var operation in updateOperations)
        {
            // 將 HashSet 轉換為換行分隔的字串
            var authorsString = string.Join("\n", operation.RowData.Authors.OrderBy(a => a));
            var prUrlsString = string.Join("\n", operation.RowData.PullRequestUrls.OrderBy(u => u));
            var mergedAtString = FormatDateTime(operation.RowData.MergedAt);

            // 更新 AuthorsColumn
            requests.Add(new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = operation.TargetRowNumber - 1, // 0-based index
                        EndRowIndex = operation.TargetRowNumber,
                        StartColumnIndex = authorsColumnIndex,
                        EndColumnIndex = authorsColumnIndex + 1,
                    },
                    Rows = new List<RowData>
                    {
                        new RowData
                        {
                            Values = new List<CellData>
                            {
                                new CellData
                                {
                                    UserEnteredValue = new ExtendedValue
                                    {
                                        StringValue = authorsString,
                                    },
                                },
                            },
                        },
                    },
                    Fields = "userEnteredValue",
                },
            });

            // 更新 PullRequestUrlsColumn
            requests.Add(new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = operation.TargetRowNumber - 1, // 0-based index
                        EndRowIndex = operation.TargetRowNumber,
                        StartColumnIndex = prUrlsColumnIndex,
                        EndColumnIndex = prUrlsColumnIndex + 1,
                    },
                    Rows = new List<RowData>
                    {
                        new RowData
                        {
                            Values = new List<CellData>
                            {
                                new CellData
                                {
                                    UserEnteredValue = new ExtendedValue
                                    {
                                        StringValue = prUrlsString,
                                    },
                                },
                            },
                        },
                    },
                    Fields = "userEnteredValue",
                },
            });

            // 更新 MergedAtColumn (僅當有值時更新)
            if (operation.RowData.MergedAt.HasValue)
            {
                requests.Add(new Request
                {
                    UpdateCells = new UpdateCellsRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = sheetId,
                            StartRowIndex = operation.TargetRowNumber - 1, // 0-based index
                            EndRowIndex = operation.TargetRowNumber,
                            StartColumnIndex = mergedAtColumnIndex,
                            EndColumnIndex = mergedAtColumnIndex + 1,
                        },
                        Rows = new List<RowData>
                        {
                            new RowData
                            {
                                Values = new List<CellData>
                                {
                                    new CellData
                                    {
                                        UserEnteredValue = new ExtendedValue
                                        {
                                            StringValue = mergedAtString,
                                        },
                                    },
                                },
                            },
                        },
                        Fields = "userEnteredValue",
                    },
                });
            }
        }

        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            Requests = requests,
        };

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var request = _sheetsService!.Spreadsheets.BatchUpdate(batchUpdateRequest, spreadsheetId);
            await request.ExecuteAsync(cancellationToken);
        });

        _logger.LogInformation("更新 {Count} 個現有 rows 完成 (Authors、PR URLs 和 MergedAt 欄位)", updateOperations.Count);
    }

    /// <summary>
    /// 取得 Sheet ID。
    /// </summary>
    private async Task<int> GetSheetIdAsync(string spreadsheetId, string sheetName, CancellationToken cancellationToken)
    {
        var spreadsheet = await _retryPolicy.ExecuteAsync(async () =>
        {
            var request = _sheetsService!.Spreadsheets.Get(spreadsheetId);
            return await request.ExecuteAsync(cancellationToken);
        });

        var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
        if (sheet == null)
        {
            throw new GoogleSheetNotFoundException($"找不到工作表: {sheetName}");
        }

        return sheet.Properties.SheetId ?? 0;
    }

    /// <summary>
    /// 判斷指定的欄位索引是否為 Feature 欄位。
    /// </summary>
    /// <param name="columnIndex">欄位索引 (0-based)。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>是否為 Feature 欄位。</returns>
    private static bool IsFeatureColumn(int columnIndex, GoogleSheetColumnMapping columnMapping)
    {
        var featureColumnIndex = ColumnLetterToIndex(columnMapping.FeatureColumn);
        return columnIndex == featureColumnIndex;
    }

    /// <summary>
    /// 判斷指定的欄位索引是否為 AutoSync 欄位。
    /// </summary>
    /// <param name="columnIndex">欄位索引 (0-based)。</param>
    /// <param name="columnMapping">欄位對應設定。</param>
    /// <returns>是否為 AutoSync 欄位。</returns>
    private static bool IsAutoSyncColumn(int columnIndex, GoogleSheetColumnMapping columnMapping)
    {
        var autoSyncColumnIndex = ColumnLetterToIndex(columnMapping.AutoSyncColumn);
        return columnIndex == autoSyncColumnIndex;
    }

    /// <summary>
    /// 建立包含超連結的儲存格資料。
    /// </summary>
    /// <param name="displayText">顯示文字。</param>
    /// <param name="url">超連結 URL。</param>
    /// <returns>包含超連結的 CellData。</returns>
    private static CellData CreateHyperlinkCell(string displayText, string url)
    {
        if (string.IsNullOrWhiteSpace(displayText))
        {
            throw new ArgumentException("顯示文字不可為空", nameof(displayText));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL 不可為空", nameof(url));
        }

        return new CellData
        {
            UserEnteredValue = new ExtendedValue
            {
                FormulaValue = $"=HYPERLINK(\"{url}\", \"{displayText}\")",
            },
        };
    }

    /// <summary>
    /// 將欄位字母轉換為索引 (0-based)。
    /// </summary>
    /// <param name="columnLetter">欄位字母 (如 "A", "B", "AA")。</param>
    /// <returns>0-based 索引。</returns>
    private static int ColumnLetterToIndex(string columnLetter)
    {
        if (string.IsNullOrWhiteSpace(columnLetter))
        {
            throw new ArgumentException("欄位字母不可為空", nameof(columnLetter));
        }

        var index = 0;
        foreach (var c in columnLetter.ToUpperInvariant())
        {
            if (!char.IsLetter(c))
            {
                throw new ArgumentException($"無效的欄位字母: {columnLetter}", nameof(columnLetter));
            }

            index = (index * 26) + (c - 'A' + 1);
        }

        return index - 1; // 轉換為 0-based
    }

    /// <summary>
    /// 格式化 DateTime 為字串，並轉換為指定時區。
    /// 使用格式: "yyyy-MM-dd (週) HH:mm"，例如 "2025-12-12 (五) 13:30"。
    /// </summary>
    /// <param name="dateTime">要格式化的日期時間 (UTC)。</param>
    /// <returns>格式化後的字串，若為 null 則返回空字串。</returns>
    private string FormatDateTime(DateTime? dateTime)
    {
        if (dateTime == null)
        {
            return string.Empty;
        }

        // 將 UTC 時間轉換為指定時區
        var timeZoneOffset = TimeSpan.FromHours(_settings.DisplayTimeZoneOffset);
        var localDateTime = dateTime.Value.Add(timeZoneOffset);

        var chineseWeekDay = GetChineseWeekDay(localDateTime.DayOfWeek);
        return $"{localDateTime:yyyy-MM-dd} ({chineseWeekDay}) {localDateTime:HH:mm}";
    }

    /// <summary>
    /// 取得中文星期幾的簡稱。
    /// </summary>
    /// <param name="dayOfWeek">星期幾。</param>
    /// <returns>中文簡稱 (日、一、二、三、四、五、六)。</returns>
    private static string GetChineseWeekDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "日",
            DayOfWeek.Monday => "一",
            DayOfWeek.Tuesday => "二",
            DayOfWeek.Wednesday => "三",
            DayOfWeek.Thursday => "四",
            DayOfWeek.Friday => "五",
            DayOfWeek.Saturday => "六",
            _ => string.Empty,
        };
    }

    /// <inheritdoc/>
    public string GenerateSpreadsheetUrl(string spreadsheetId)
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            throw new ArgumentException("Spreadsheet ID 不可為空", nameof(spreadsheetId));
        }

        return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/edit";
    }

    /// <inheritdoc/>
    public async Task<int> BatchReorderRowsAsync(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<SheetBlockReorderOperation> reorderOperations,
        GoogleSheetColumnMapping columnMapping,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (reorderOperations.Count == 0)
        {
            _logger.LogInformation("無重新排列操作需要執行");
            return 0;
        }

        // 驗證所有操作
        foreach (var operation in reorderOperations)
        {
            if (!operation.IsValid())
            {
                throw new ArgumentException(
                    $"重新排列操作無效: Repository={operation.RepositoryName}, StartRow={operation.StartRowNumber}, EndRow={operation.EndRowNumber}, SortedRowsCount={operation.SortedOriginalRowNumbers.Count}");
            }
        }

        _logger.LogInformation(
            "正在執行區塊排序: {BlockCount} 個區塊, 共 {TotalRows} 個 rows",
            reorderOperations.Count,
            reorderOperations.Sum(op => op.RowCount));

        // 取得 SheetId
        var sheetId = await GetSheetIdAsync(spreadsheetId, sheetName, cancellationToken);

        // 為每個區塊建立移動請求
        // 使用 MoveDimension API 來移動行，保留所有格式和公式
        var allRequests = new List<Request>();

        foreach (var operation in reorderOperations)
        {
            var moveRequests = GenerateMoveRequests(sheetId, operation);
            allRequests.AddRange(moveRequests);
        }

        if (allRequests.Count == 0)
        {
            _logger.LogInformation("無需移動任何行");
            return 0;
        }

        // 執行批次更新
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            Requests = allRequests,
        };

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var request = _sheetsService!.Spreadsheets.BatchUpdate(batchUpdateRequest, spreadsheetId);
            await request.ExecuteAsync(cancellationToken);
        });

        var totalRows = reorderOperations.Sum(op => op.RowCount);
        _logger.LogInformation("區塊排序完成: {BlockCount} 個區塊, 共 {TotalRows} 個 rows", reorderOperations.Count, totalRows);

        return totalRows;
    }

    /// <summary>
    /// 產生移動行的請求清單。
    /// 使用 selection sort 風格的移動：依序將正確的行移動到目標位置。
    /// </summary>
    /// <param name="sheetId">工作表 ID。</param>
    /// <param name="operation">重新排列操作。</param>
    /// <returns>移動請求清單。</returns>
    private static List<Request> GenerateMoveRequests(int sheetId, SheetBlockReorderOperation operation)
    {
        var requests = new List<Request>();

        // 建立目前行號到原始行號的映射
        // currentPositions[i] = 目前在位置 i 的原始行號
        var currentPositions = new List<int>();
        for (var i = operation.StartRowNumber; i <= operation.EndRowNumber; i++)
        {
            currentPositions.Add(i);
        }

        // 使用 selection sort 風格的移動
        for (var targetIndex = 0; targetIndex < operation.SortedOriginalRowNumbers.Count; targetIndex++)
        {
            var targetRowNumber = operation.StartRowNumber + targetIndex;
            var desiredOriginalRowNumber = operation.SortedOriginalRowNumbers[targetIndex];

            // 找到 desiredOriginalRowNumber 目前所在的位置
            var currentIndex = currentPositions.IndexOf(desiredOriginalRowNumber);

            if (currentIndex == targetIndex)
            {
                // 已經在正確位置，不需要移動
                continue;
            }

            // 計算實際的行號 (0-based for API)
            var sourceRowIndex = operation.StartRowNumber - 1 + currentIndex;
            var destinationRowIndex = operation.StartRowNumber - 1 + targetIndex;

            // 建立移動請求
            // MoveDimension: 將 sourceRowIndex 移動到 destinationRowIndex 之前
            // 注意：如果 source > destination，行會插入到 destination 之前
            //       如果 source < destination，行會插入到 destination 之後
            requests.Add(new Request
            {
                MoveDimension = new MoveDimensionRequest
                {
                    Source = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = sourceRowIndex,
                        EndIndex = sourceRowIndex + 1,
                    },
                    DestinationIndex = destinationRowIndex,
                },
            });

            // 更新 currentPositions 以反映移動後的狀態
            currentPositions.RemoveAt(currentIndex);
            currentPositions.Insert(targetIndex, desiredOriginalRowNumber);
        }

        return requests;
    }

    /// <summary>
    /// 確保已完成驗證。
    /// </summary>
    private void EnsureAuthenticated()
    {
        if (_sheetsService == null)
        {
            throw new InvalidOperationException("尚未完成驗證。請先呼叫 AuthenticateAsync。");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 釋放資源。
    /// </summary>
    /// <param name="disposing">是否正在釋放。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sheetsService?.Dispose();
                _sheetsService = null;
            }

            _disposed = true;
        }
    }
}
