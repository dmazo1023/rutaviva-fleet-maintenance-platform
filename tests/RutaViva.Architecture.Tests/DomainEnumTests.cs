namespace RutaViva.Architecture.Tests;

public sealed class DomainEnumTests
{
    private const long FirstEnumValue = 1;

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Domain), MemberType = typeof(ArchitectureAssemblies))]
    public void Enums_Should_StartAtExplicitOne_When_DeclaredInDomainAssembly(string assemblyName)
    {
        string[] violations = ArchitectureAssemblies.Load(assemblyName)
            .GetTypes()
            .Where(type => type.IsEnum)
            .Where(type => !StartsAtOne(type))
            .Select(type => type.FullName ?? type.Name)
            .ToArray();

        violations.Should().BeEmpty("default(T) must not be a valid enum state");
    }

    private static bool StartsAtOne(Type enumType) =>
        Enum.GetValuesAsUnderlyingType(enumType).Cast<object>().Min(value => Convert.ToInt64(value)) == FirstEnumValue;
}
