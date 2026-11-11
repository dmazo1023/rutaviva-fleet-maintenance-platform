using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Infrastructure;
using RutaViva.EventBus.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.EventBus.Hub.IntegrationTests;

internal sealed class EventBusStoreHarness : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    private EventBusStoreHarness(ServiceProvider provider)
    {
        _provider = provider;
    }

    public IEventStore Store => _provider.GetRequiredService<IEventStore>();

    public IDbContextFactory<EventBusDbContext> ContextFactory => _provider.GetRequiredService<IDbContextFactory<EventBusDbContext>>();

    public static async Task<EventBusStoreHarness> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(database, cancellationToken);
        ServiceCollection services = new();
        services.AddEventBusInfrastructure(database.ConnectionString);
        return new EventBusStoreHarness(services.BuildServiceProvider());
    }

    public ValueTask DisposeAsync() => _provider.DisposeAsync();

    private static async Task EnsureSchemaAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using EventBusDbContext context = await database.CreateDbContextAsync<EventBusDbContext>(cancellationToken);
    }
}
