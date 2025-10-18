namespace ReleaseSync.Console.Services;

/// <summary>
/// 資料拉取服務實作
/// </summary>
public class DataFetchingService : IDataFetchingService
{
    /// <inheritdoc />
    public Task FetchDataAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "資料拉取服務尚未實作。將於後續階段實作與 GitHub/GitLab API 的整合。");
    }
}
