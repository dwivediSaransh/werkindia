using IndiaWorks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace IndiaWorks.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddIndiaWorksApplication_registers_application_services()
    {
        // Arrange
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        // Act
        var provider = services.AddIndiaWorksApplication().BuildServiceProvider();

        // Assert - application layer registration completes without error
        Assert.NotNull(provider);
    }
}
