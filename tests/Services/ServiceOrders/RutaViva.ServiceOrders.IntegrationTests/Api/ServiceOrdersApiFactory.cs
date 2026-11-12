using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RutaViva.ServiceOrders.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.ServiceOrders.IntegrationTests.Api;

public sealed class ServiceOrdersApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringSetting = "ConnectionStrings:RutaViva";
    private const string HubUrlSetting = "EventBus:HubUrl";
    private const string LocalHubUrl = "http://localhost:1";
    private const string AutomaticOrdersSinceSetting = "ServiceOrders:AutomaticOrdersSince";
    private const string AutomaticOrdersSince = "2026-01-01T00:00:00Z";

    private readonly string _connectionString;

    private ServiceOrdersApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<ServiceOrdersApiFactory> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using ServiceOrdersDbContext context = await database.CreateDbContextAsync<ServiceOrdersDbContext>(cancellationToken);
        return new ServiceOrdersApiFactory(database.ConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(ConnectionStringSetting, _connectionString);
        builder.UseSetting(HubUrlSetting, LocalHubUrl);
        builder.UseSetting(AutomaticOrdersSinceSetting, AutomaticOrdersSince);
        builder.ConfigureTestServices(RemoveHostedServices);
    }

    private static void RemoveHostedServices(IServiceCollection services)
    {
        services.RemoveAll<IHostedService>();
    }
}
