namespace ReleaseSync.Console.Services;

/// <summary>
/// 命令列參數解析服務實作
/// </summary>
public class CommandLineParserService : ICommandLineParserService
{
    /// <inheritdoc />
    public void Parse(string platform, string token, string repository)
    {
        throw new NotImplementedException(
            "命令列參數解析服務尚未實作。將於後續階段實作平台驗證、Token 格式檢查等功能。");
    }
}
