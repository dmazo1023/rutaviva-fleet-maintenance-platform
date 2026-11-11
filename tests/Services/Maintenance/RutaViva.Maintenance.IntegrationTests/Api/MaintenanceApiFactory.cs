using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

public sealed class MaintenanceApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringSetting = "ConnectionStrings:RutaViva";
    private const string HubUrlSetting = "EventBus:HubUrl";
    private const string LocalHubUrl = "http://localhost:1";

    private readonly string _connectionString;

    private MaintenanceApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<MaintenanceApiFactory> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        return new MaintenanceApiFactory(database.ConnectionString);
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
