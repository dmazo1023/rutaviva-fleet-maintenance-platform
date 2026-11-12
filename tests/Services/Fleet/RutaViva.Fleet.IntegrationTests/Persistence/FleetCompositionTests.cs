using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Infrastructure.DependencyInjection;

namespace RutaViva.Fleet.IntegrationTests.Persistence;

[Trait("Category", "Integration")]
public sealed class FleetCompositionTests
{
    private const string ConnectionStringKey = "ConnectionStrings:RutaViva";
    private const string ConnectionStringValue = "Server=localhost;Database=RutaViva_Composition;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string HubUrlKey = "EventBus:HubUrl";
    private const string HubUrlValue = "http://localhost:5100/eventbus";

    [Fact]
    public void AddFleet_Should_ResolveRegisterVehicleHandler_When_ConfigurationIsProvided()
    {
        Dictionary<string, string?> values = new()
        {
            [ConnectionStringKey] = ConnectionStringValue,
            [HubUrlKey] = HubUrlValue
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFleet(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        ICommandHandler<RegisterVehicleCommand, Guid> handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RegisterVehicleCommand, Guid>>();

        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddFleet_Should_Throw_When_ConnectionStringIsMissing()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new();

        Action act = () => services.AddFleet(configuration);

        act.Should().Throw<InvalidOperationException>();
    }
}
