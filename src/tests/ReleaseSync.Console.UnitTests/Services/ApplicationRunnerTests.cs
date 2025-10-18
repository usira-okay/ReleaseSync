using FluentAssertions;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.UnitTests.Services;

/// <summary>
/// ApplicationRunner 服務測試
/// </summary>
public class ApplicationRunnerTests
{
    [Fact(DisplayName = "當服務被呼叫時,應拋出 NotImplementedException")]
    public async Task RunAsync_ThrowsNotImplementedException_WhenCalled()
    {
        // Arrange
        var runner = new ApplicationRunner();
        var args = Array.Empty<string>();

        // Act
        Func<Task<int>> act = async () => await runner.RunAsync(args);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*尚未實作*");
    }
}
