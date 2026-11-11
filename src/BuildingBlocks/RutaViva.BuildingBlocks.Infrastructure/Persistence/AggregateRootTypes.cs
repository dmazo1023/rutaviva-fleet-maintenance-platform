using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.BuildingBlocks.Infrastructure.Persistence;

internal static class AggregateRootTypes
{
    public static bool IsAggregateRoot(Type type) => FindClosedType(type) is not null;

    public static Type? FindClosedType(Type type)
    {
        Type? current = type;
        while (current is not null)
        {
            if (IsAggregateRootDefinition(current))
            {
                return current;
            }

            current = current.BaseType;
        }

        return null;
    }

    private static bool IsAggregateRootDefinition(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>);
}
