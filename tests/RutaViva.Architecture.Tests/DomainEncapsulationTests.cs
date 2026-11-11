using System.Reflection;
using RutaViva.BuildingBlocks.Domain.Primitives;

namespace RutaViva.Architecture.Tests;

public sealed class DomainEncapsulationTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Domain), MemberType = typeof(ArchitectureAssemblies))]
    public void Entities_Should_NotExposePublicSetters_When_TypeInheritsEntityOrAggregateRoot(string assemblyName)
    {
        string[] violations = EntityTypes(assemblyName)
            .SelectMany(type => type.GetProperties(DeclaredPublicInstance))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToArray();

        violations.Should().BeEmpty("domain entities must not expose public setters");
    }

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Domain), MemberType = typeof(ArchitectureAssemblies))]
    public void Entities_Should_ExposeCollectionsAsReadOnly_When_TypeInheritsEntityOrAggregateRoot(string assemblyName)
    {
        string[] violations = EntityTypes(assemblyName)
            .SelectMany(type => type.GetProperties(DeclaredPublicInstance))
            .Where(property => IsMutableCollection(property.PropertyType))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToArray();

        violations.Should().BeEmpty("domain entities must expose collections as IReadOnlyCollection");
    }

    private static IEnumerable<Type> EntityTypes(string assemblyName) =>
        ArchitectureAssemblies.Load(assemblyName)
            .GetTypes()
            .Where(type => type.IsClass && InheritsFromEntity(type));

    private static bool InheritsFromEntity(Type type)
    {
        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMutableCollection(Type propertyType) =>
        propertyType != typeof(string)
        && (propertyType.IsArray || IsGenericMutableCollection(propertyType) || propertyType.GetInterfaces().Any(IsGenericMutableCollection));

    private static bool IsGenericMutableCollection(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);
}
