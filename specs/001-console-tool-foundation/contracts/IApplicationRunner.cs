namespace ReleaseSync.Console.Services;

/// <summary>
/// 應用程式主要邏輯執行服務介面
/// Orchestrates the main application workflow
/// </summary>
/// <remarks>
/// 負責協調應用程式的主要執行流程,包含:
/// - 呼叫參數解析服務
/// - 呼叫資料拉取服務
/// - 協調錯誤處理與日誌記錄
/// - 返回適當的退出碼
///
/// 遵循 Single Responsibility Principle,專注於流程協調,
/// 不包含具體的業務邏輯實作。
/// </remarks>
public interface IApplicationRunner
{
    /// <summary>
    /// 執行應用程式主要邏輯 (非同步方法)
    /// </summary>
    /// <param name="args">命令列參數</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>應用程式退出碼 (0 表示成功,非 0 表示錯誤)</returns>
    /// <exception cref="NotImplementedException">本階段功能尚未實作</exception>
    /// <remarks>
    /// 本方法目前拋出 NotImplementedException,表示功能尚未實作。
    ///
    /// 未來實作時,此方法將:
    /// 1. 使用 ICommandLineParserService 解析命令列參數
    /// 2. 驗證參數有效性
    /// 3. 呼叫 IDataFetchingService 拉取資料
    /// 4. 處理例外狀況並記錄錯誤日誌
    /// 5. 返回適當的退出碼:
    ///    - 0: 成功
    ///    - 1: 一般錯誤
    ///    - 2: 參數錯誤
    ///    - 3: 認證錯誤
    ///    - 4: 網路錯誤
    ///
    /// 設計考量:
    /// - 使用依賴注入取得服務實例
    /// - 遵循 CQRS 原則,未來可能改為發送 Command
    /// - 結構化日誌記錄所有關鍵事件
    /// </remarks>
    Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default);
}
