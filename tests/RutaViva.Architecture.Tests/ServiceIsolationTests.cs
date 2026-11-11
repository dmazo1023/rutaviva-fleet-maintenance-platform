namespace RutaViva.Architecture.Tests;

public sealed class ServiceIsolationTests
{
    private const string RootNamespace = "RutaViva.";
    private const string IntegrationContractsAssembly = "RutaViva.IntegrationContracts";
    private const char SegmentSeparator = '.';

    private static readonly string[] SharedComponents = ["BuildingBlocks", "IntegrationContracts", "ServiceDefaults"];

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.All), MemberType = typeof(ArchitectureAssemblies))]
    public void Assembly_Should_OnlyReferenceOwnOrSharedComponents_When_ItReferencesRutaVivaAssemblies(string assemblyName)
    {
        string component = ComponentOf(assemblyName);

        string[] violations = RutaVivaReferences(assemblyName)
            .Where(reference => !IsAllowed(component, reference))
            .ToArray();

        violations.Should().BeEmpty("only BuildingBlocks, IntegrationContracts and ServiceDefaults are shared between components");
    }

    [Fact]
    public void IntegrationContracts_Should_NotReferenceAnyRutaVivaAssembly_When_Inspected()
    {
        string[] references = RutaVivaReferences(IntegrationContractsAssembly);

        references.Should().BeEmpty();
    }

    private static string[] RutaVivaReferences(string assemblyName) =>
        ArchitectureAssemblies.Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith(RootNamespace, StringComparison.Ordinal))
            .ToArray();

    private static bool IsAllowed(string component, string reference)
    {
        string referencedComponent = ComponentOf(reference);
        return referencedComponent == component || SharedComponents.Contains(referencedComponent);
    }

    private static string ComponentOf(string assemblyName) => assemblyName[RootNamespace.Length..].Split(SegmentSeparator)[0];
}
