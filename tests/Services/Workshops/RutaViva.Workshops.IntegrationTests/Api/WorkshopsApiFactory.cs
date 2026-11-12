using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RutaViva.Testing.Fixtures;
using RutaViva.Workshops.Infrastructure.Persistence;

namespace RutaViva.Workshops.IntegrationTests.Api;

public sealed class WorkshopsApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringSetting = "ConnectionStrings:RutaViva";
    private const string HubUrlSetting = "EventBus:HubUrl";
    private const string LocalHubUrl = "http://localhost:1";

    private readonly string _connectionString;

    private WorkshopsApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<WorkshopsApiFactory> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using WorkshopsDbContext context = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);
        return new WorkshopsApiFactory(database.ConnectionString);
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
