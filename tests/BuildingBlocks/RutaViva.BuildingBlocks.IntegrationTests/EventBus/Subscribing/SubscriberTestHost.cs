using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.BuildingBlocks.EventBus.Client.Subscribing;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class SubscriberTestHost : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;
    private readonly List<IHostedService> _hostedServices;

    private SubscriberTestHost(ServiceProvider provider, string subscriberName)
    {
        _provider = provider;
        SubscriberName = subscriberName;
        _hostedServices = [.. provider.GetServices<IHostedService>()];
    }

    public string SubscriberName { get; }

    public HubConnection? Connection => _provider.GetRequiredService<CapturingConnectionFactory>().Connection;

    public int Attempts => _provider.GetRequiredService<AttemptCounter>().Count;

    public static async Task<SubscriberTestHost> CreateAsync(
        EventBusTestServer server,
        SqlServerTestDatabase database,
        string subscriberName,
        Action<EventBusSubscriberBuilder> registerHandlers,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await using SubscribingTestDbContext schemaContext = await database.CreateDbContextAsync<SubscribingTestDbContext>(cancellationToken);
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IClock>(new FakeClock(Now));
        services.AddSingleton(timeProvider);
        services.AddSingleton(new AttemptCounter());
        services.AddDbContext<SubscribingTestDbContext>(options => options.UseSqlServer(database.ConnectionString));
        services.AddScoped<IEventCheckpointStore, EfEventCheckpointStore<SubscribingTestDbContext>>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork<SubscribingTestDbContext>>();
        services.AddSingleton(provider => new CapturingConnectionFactory(
            new EventBusConnectionFactory(provider.GetRequiredService<IOptions<EventBusClientOptions>>())));
        services.AddSingleton<IEventBusConnectionFactory>(provider => provider.GetRequiredService<CapturingConnectionFactory>());
        registerHandlers(services.AddEventBusSubscriber(subscriberName, StartPosition.Checkpoint, server.ConfigureClient));
        return new SubscriberTestHost(services.BuildServiceProvider(), subscriberName);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (IHostedService hostedService in _hostedServices)
        {
            await hostedService.StartAsync(cancellationToken);
        }
    }

    public Task<long> ReadCheckpointAsync(CancellationToken cancellationToken)
        => QueryAsync(
            (scope, token) => scope.ServiceProvider.GetRequiredService<IEventCheckpointStore>().GetLastOffsetAsync(SubscriberName, token),
            cancellationToken);

    public Task<bool> WaitForCheckpointAsync(long offset, TimeSpan timeout, CancellationToken cancellationToken)
        => Eventually.UntilAsync(async () => await ReadCheckpointAsync(cancellationToken) >= offset, timeout, cancellationToken);

    public Task<List<long>> ReadEffectOffsetsAsync(string subject, CancellationToken cancellationToken)
        => QueryAsync(
            (scope, token) => scope.ServiceProvider.GetRequiredService<SubscribingTestDbContext>().Effects
                .Where(effect => effect.Subject == subject)
                .OrderBy(effect => effect.Id)
                .Select(effect => effect.Offset)
                .ToListAsync(token),
            cancellationToken);

    public Task<List<string>> ReadEffectTypesAsync(string subject, CancellationToken cancellationToken)
        => QueryAsync(
            (scope, token) => scope.ServiceProvider.GetRequiredService<SubscribingTestDbContext>().Effects
                .Where(effect => effect.Subject == subject)
                .OrderBy(effect => effect.Id)
                .Select(effect => effect.EventType)
                .ToListAsync(token),
            cancellationToken);

    public async ValueTask DisposeAsync()
    {
        foreach (IHostedService hostedService in _hostedServices)
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        await _provider.DisposeAsync();
    }

    private async Task<TResult> QueryAsync<TResult>(Func<AsyncServiceScope, CancellationToken, Task<TResult>> query, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return await query(scope, cancellationToken);
    }
}
