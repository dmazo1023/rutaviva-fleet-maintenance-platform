using System.Reflection;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Tests;

internal static class IntegrationEventCatalog
{
    private const string EventNameField = "EventName";

    public static IReadOnlyList<Type> EventTypes { get; } = Discover();

    public static string NameOf(Type eventType) =>
        (string)eventType.GetField(EventNameField, BindingFlags.Public | BindingFlags.Static)!.GetRawConstantValue()!;

    public static Type TypeOf(string eventName) => EventTypes.Single(type => NameOf(type) == eventName);

    private static Type[] Discover() =>
        typeof(IIntegrationEvent).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IIntegrationEvent).IsAssignableFrom(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
}
