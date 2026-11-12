using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.Testing.Fixtures;
using RutaViva.Workshops.Infrastructure.DependencyInjection;
using RutaViva.Workshops.Infrastructure.Persistence;

namespace RutaViva.Workshops.IntegrationTests.Persistence;

public sealed class WorkshopsPersistenceHost : IAsyncDisposable
{
    private const string ConnectionStringKey = "ConnectionStrings:RutaViva";

    private readonly ServiceProvider _provider;

    private WorkshopsPersistenceHost(ServiceProvider provider)
    {
        _provider = provider;
    }

    public static async Task<WorkshopsPersistenceHost> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using WorkshopsDbContext schemaContext = await database.CreateDbContextAsync<WorkshopsDbContext>(cancellationToken);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConnectionStringKey] = database.ConnectionString })
            .Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddWorkshops(configuration);
        return new WorkshopsPersistenceHost(services.BuildServiceProvider());
    }

    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
