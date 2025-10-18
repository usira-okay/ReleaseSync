using FluentAssertions;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.UnitTests.Services;

/// <summary>
/// DataFetchingService 服務測試
/// </summary>
public class DataFetchingServiceTests
{
    [Fact(DisplayName = "當服務被呼叫時,應拋出 NotImplementedException")]
    public async Task FetchDataAsync_ThrowsNotImplementedException_WhenCalled()
    {
        // Arrange
        var service = new DataFetchingService();

        // Act
        Func<Task> act = async () => await service.FetchDataAsync();

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*尚未實作*");
    }
}
