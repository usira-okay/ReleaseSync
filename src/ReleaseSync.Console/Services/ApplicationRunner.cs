namespace ReleaseSync.Console.Services;

/// <summary>
/// 應用程式主要邏輯執行服務實作
/// </summary>
public class ApplicationRunner : IApplicationRunner
{
    /// <inheritdoc />
    public Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "應用程式執行服務尚未實作。將於後續階段實作完整的流程協調邏輯。");
    }
}
