using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

public static class EventBusSubscriberServiceCollectionExtensions
{
    public static EventBusSubscriberBuilder AddEventBusSubscriber(
        this IServiceCollection services,
        string subscriberName,
        StartPosition startFrom,
        Action<EventBusClientOptions> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberName);
        EventHandlerRegistry registry = new();
        services.AddEventBusConnection(configure);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(registry);
        services.AddSingleton(new SubscriberOptions(subscriberName, startFrom));
        services.AddSingleton<IntegrationEventDispatcher>();
        services.AddHostedService<EventBusSubscriberHost>();
        AddLiveOnlyDefaults(services, startFrom);
        return new EventBusSubscriberBuilder(services, registry);
    }

    private static void AddLiveOnlyDefaults(IServiceCollection services, StartPosition startFrom)
    {
        if (startFrom != StartPosition.Latest)
        {
            return;
        }

        services.TryAddSingleton<IEventCheckpointStore, InMemoryEventCheckpointStore>();
        services.TryAddScoped<IUnitOfWork, NullUnitOfWork>();
    }
}
