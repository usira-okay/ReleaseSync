namespace ReleaseSync.Console.Services;

/// <summary>
/// 命令列參數解析服務介面
/// Parses command-line arguments for the ReleaseSync console application
/// </summary>
/// <remarks>
/// 本階段僅定義介面契約,實作將拋出 NotImplementedException。
/// 未來階段將實作完整的參數解析邏輯,包含:
/// - 平台類型驗證 (GitHub/GitLab/Azure DevOps)
/// - Token 格式檢查
/// - Repository 路徑解析
/// - 參數組合驗證
/// </remarks>
public interface ICommandLineParserService
{
    /// <summary>
    /// 解析命令列參數
    /// </summary>
    /// <param name="platform">版控平台類型 (例如: github, gitlab)</param>
    /// <param name="token">存取權杖</param>
    /// <param name="repository">Repository 路徑 (格式: owner/repo)</param>
    /// <exception cref="NotImplementedException">本階段功能尚未實作</exception>
    /// <exception cref="ArgumentException">未來階段:當參數格式無效時拋出</exception>
    /// <exception cref="ArgumentNullException">未來階段:當必要參數為 null 時拋出</exception>
    /// <remarks>
    /// 本方法目前拋出 NotImplementedException,表示功能尚未實作。
    ///
    /// 未來實作時,此方法將:
    /// 1. 驗證 platform 是否為支援的平台類型
    /// 2. 驗證 token 格式是否正確
    /// 3. 驗證 repository 格式是否符合 owner/repo 規範
    /// 4. 返回解析後的結構化資料 (或使用 System.CommandLine 內建機制)
    /// </remarks>
    void Parse(string platform, string token, string repository);
}
