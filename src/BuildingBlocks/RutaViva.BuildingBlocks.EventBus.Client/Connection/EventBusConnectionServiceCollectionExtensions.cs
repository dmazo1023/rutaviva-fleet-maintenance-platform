using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RutaViva.BuildingBlocks.EventBus.Client.Connection;

public static class EventBusConnectionServiceCollectionExtensions
{
    public static IServiceCollection AddEventBusConnection(this IServiceCollection services, Action<EventBusClientOptions> configure)
    {
        services.Configure(configure);
        services.TryAddSingleton<IEventBusConnectionFactory, EventBusConnectionFactory>();
        return services;
    }
}
