using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Infrastructure.DomainEvents;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class DomainEventsTestHost : IAsyncDisposable
{
    private const string ServiceName = "maintenance";

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;

    private DomainEventsTestHost(ServiceProvider provider)
    {
        _provider = provider;
    }

    public static async Task<DomainEventsTestHost> CreateAsync(SqlServerTestDatabase database, CancellationToken cancellationToken)
    {
        await using EventsTestDbContext schemaContext = await database.CreateDbContextAsync<EventsTestDbContext>(cancellationToken);
        ServiceCollection services = new();
        services.AddSingleton<IClock>(new FakeClock(Now));
        services.AddSingleton(new IntegrationEventSettings { ServiceName = ServiceName });
        services.AddSingleton<IntegrationEventEnvelopeFactory>();
        services.AddSingleton<OutboxMessageFactory>();
        services.AddDomainEventsToOutbox();
        services.AddScoped<IDomainEventHandler<ParentRegistered>, CreateChildOnParentRegisteredHandler>();
        services.AddScoped<IDomainEventHandler<LoopTriggered>, SpawnLoopOnLoopTriggeredHandler>();
        services.AddScoped<IIntegrationEventMapper<ParentRegistered>, ParentRegisteredMapper>();
        services.AddDbContext<EventsTestDbContext>((serviceProvider, options) => options
            .UseSqlServer(database.ConnectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<DomainEventsToOutboxInterceptor>()));
        return new DomainEventsTestHost(services.BuildServiceProvider());
    }

    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
