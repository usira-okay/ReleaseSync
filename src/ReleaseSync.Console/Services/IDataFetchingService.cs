namespace ReleaseSync.Console.Services;

/// <summary>
/// 資料拉取服務介面
/// Fetches Pull Request / Merge Request data from version control platforms
/// </summary>
/// <remarks>
/// 本階段僅定義介面契約,實作將拋出 NotImplementedException。
/// 未來階段將實作完整的資料拉取邏輯,包含:
/// - 與 GitHub API 整合
/// - 與 GitLab API 整合
/// - 與 Azure DevOps API 整合
/// - API 呼叫重試機制
/// - Rate limiting 處理
/// - 錯誤處理與日誌記錄
/// </remarks>
public interface IDataFetchingService
{
    /// <summary>
    /// 拉取資料 (非同步方法)
    /// </summary>
    /// <param name="cancellationToken">取消權杖,用於取消非同步操作</param>
    /// <returns>非同步作業</returns>
    /// <exception cref="NotImplementedException">本階段功能尚未實作</exception>
    /// <exception cref="HttpRequestException">未來階段:當 API 呼叫失敗時拋出</exception>
    /// <exception cref="UnauthorizedAccessException">未來階段:當認證失敗時拋出</exception>
    /// <remarks>
    /// 本方法目前拋出 NotImplementedException,表示功能尚未實作。
    ///
    /// 未來實作時,此方法將:
    /// 1. 根據平台類型選擇對應的 API 客戶端 (GitHub/GitLab/Azure DevOps)
    /// 2. 呼叫版控平台 API 抓取 PR/MR 資訊
    /// 3. 解析 API 回應並轉換為領域模型
    /// 4. 處理分頁、重試、錯誤回應等
    /// 5. 記錄結構化日誌 (成功/失敗/效能指標)
    /// 6. 返回抓取的 PR/MR 物件
    ///
    /// 參數傳遞策略 (未來階段):
    /// - 選項 A: 透過建構子注入設定 (PlatformConfiguration, RepositoryInformation)
    /// - 選項 B: 透過方法參數傳遞 (更靈活,支援多個 repository)
    /// - 建議採用選項 B,提供更好的彈性
    /// </remarks>
    Task FetchDataAsync(CancellationToken cancellationToken = default);
}
