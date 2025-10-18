using FluentAssertions;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.UnitTests.Services;

/// <summary>
/// CommandLineParserService 服務測試
/// </summary>
public class CommandLineParserServiceTests
{
    [Fact(DisplayName = "當服務被呼叫時,應拋出 NotImplementedException")]
    public void Parse_ThrowsNotImplementedException_WhenCalled()
    {
        // Arrange
        var service = new CommandLineParserService();

        // Act
        Action act = () => service.Parse("github", "token", "owner/repo");

        // Assert
        act.Should().Throw<NotImplementedException>()
            .WithMessage("*尚未實作*");
    }
}
