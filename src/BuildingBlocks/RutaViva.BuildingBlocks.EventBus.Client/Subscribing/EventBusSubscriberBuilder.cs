using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

public sealed class EventBusSubscriberBuilder
{
    private readonly IServiceCollection _services;
    private readonly EventHandlerRegistry _registry;

    internal EventBusSubscriberBuilder(IServiceCollection services, EventHandlerRegistry registry)
    {
        _services = services;
        _registry = registry;
    }

    public EventBusSubscriberBuilder Handle<TEvent, THandler>(string stream)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        _services.TryAddScoped<THandler>();
        _registry.Register(EventNames.FromEventType(typeof(TEvent)), stream, new HandlerDispatchStrategy<TEvent, THandler>());
        return this;
    }

    public EventBusSubscriberBuilder HandleFamily<TMarker, THandler>(string stream)
        where TMarker : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TMarker>
    {
        _services.TryAddScoped<THandler>();
        foreach (Type eventType in EventFamilyScanner.ConcreteEventTypes(typeof(TMarker)))
        {
            _registry.Register(EventNames.FromEventType(eventType), stream, CreateStrategy(eventType, typeof(THandler)));
        }

        return this;
    }

    private static IEventDispatchStrategy CreateStrategy(Type eventType, Type handlerType)
        => (IEventDispatchStrategy)Activator.CreateInstance(typeof(HandlerDispatchStrategy<,>).MakeGenericType(eventType, handlerType))!;
}
