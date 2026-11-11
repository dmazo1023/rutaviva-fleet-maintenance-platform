using System.Reflection;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal static class EventFamilyScanner
{
    public static IReadOnlyList<Type> ConcreteEventTypes(Type markerType)
        => [.. CandidateAssemblies(markerType)
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => IsConcreteEventOf(type, markerType))];

    private static IEnumerable<Assembly> CandidateAssemblies(Type markerType)
        => new[] { typeof(IIntegrationEvent).Assembly, markerType.Assembly }.Distinct();

    private static bool IsConcreteEventOf(Type type, Type markerType)
        => type is { IsClass: true, IsAbstract: false }
            && markerType.IsAssignableFrom(type)
            && EventNames.IsDeclaredBy(type);
}
