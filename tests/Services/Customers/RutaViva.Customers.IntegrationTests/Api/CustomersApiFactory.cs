using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RutaViva.Customers.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Customers.IntegrationTests.Api;

public sealed class CustomersApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringSetting = "ConnectionStrings:RutaViva";
    private const string HubUrlSetting = "EventBus:HubUrl";
    private const string LocalHubUrl = "http://localhost:1";

    private readonly string _connectionString;

    private CustomersApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<CustomersApiFactory> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using CustomersDbContext context = await database.CreateDbContextAsync<CustomersDbContext>(cancellationToken);
        return new CustomersApiFactory(database.ConnectionString);
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
