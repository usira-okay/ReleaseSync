using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ReleaseSync.Console.Extensions;
using ReleaseSync.Console.Services;

namespace ReleaseSync.Console.UnitTests.Extensions;

/// <summary>
/// ServiceCollectionExtensions 測試
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "AddApplicationServices 應正確註冊所有服務")]
    public void AddApplicationServices_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ICommandLineParserService>().Should().NotBeNull();
        provider.GetService<IDataFetchingService>().Should().NotBeNull();
        provider.GetService<IApplicationRunner>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddApplicationServices 應註冊為 Scoped 生命週期")]
    public void AddApplicationServices_RegistersServicesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();

        // Act & Assert
        var descriptor = services.First(s => s.ServiceType == typeof(ICommandLineParserService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
