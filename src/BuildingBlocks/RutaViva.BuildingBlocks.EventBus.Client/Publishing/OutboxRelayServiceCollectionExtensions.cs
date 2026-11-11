using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;

namespace RutaViva.BuildingBlocks.EventBus.Client.Publishing;

public static class OutboxRelayServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxRelay<TContext>(this IServiceCollection services, Action<EventBusClientOptions> configure)
        where TContext : DbContext
    {
        services.AddEventBusConnection(configure);
        services.TryAddSingleton<IIntegrationEventTransport, EventBusPublisher>();
        services.AddScoped<OutboxBatchProcessor<TContext>>();
        services.AddHostedService<OutboxRelayService<TContext>>();
        return services;
    }
}
