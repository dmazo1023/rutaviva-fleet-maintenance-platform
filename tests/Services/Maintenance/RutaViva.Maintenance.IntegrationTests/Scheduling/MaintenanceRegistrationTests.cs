using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Infrastructure.DependencyInjection;

namespace RutaViva.Maintenance.IntegrationTests.Scheduling;

public sealed class MaintenanceRegistrationTests
{
    private const string ConnectionStringKey = "ConnectionStrings:RutaViva";
    private const string ConnectionStringValue = "Server=localhost;Database=Maintenance;Trusted_Connection=True;TrustServerCertificate=True";
    private const string HubUrlKey = "EventBus:HubUrl";
    private const string HubUrlValue = "http://localhost:5000";

    [Fact]
    public void AddMaintenance_Should_ResolveCommandAndQueryHandlers_When_ScopesAreValidated()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        object command = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<IngestTelemetryReadingCommand, TelemetryReadingOutcome>>();
        object query = scope.ServiceProvider
            .GetRequiredService<IQueryHandler<GetMaintenancePlanQuery, MaintenancePlanDetailDto>>();

        command.Should().NotBeNull();
        query.Should().NotBeNull();
    }

    private static ServiceProvider BuildProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConnectionStringKey] = ConnectionStringValue,
                [HubUrlKey] = HubUrlValue
            })
            .Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddMaintenance(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
