using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RutaViva.Fleet.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Api;

public sealed class FleetApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringSetting = "ConnectionStrings:RutaViva";
    private const string HubUrlSetting = "EventBus:HubUrl";
    private const string LocalHubUrl = "http://localhost:1";

    private readonly string _connectionString;

    private FleetApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<FleetApiFactory> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using FleetDbContext context = await database.CreateDbContextAsync<FleetDbContext>(cancellationToken);
        return new FleetApiFactory(database.ConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(ConnectionStringSetting, _connectionString);
        builder.UseSetting(HubUrlSetting, LocalHubUrl);
        builder.ConfigureTestServices(RemoveHostedServices);
    }

    private static void RemoveHostedServices(IServiceCollection services)
    {
        services.RemoveAll<IHostedService>();
    }
}
