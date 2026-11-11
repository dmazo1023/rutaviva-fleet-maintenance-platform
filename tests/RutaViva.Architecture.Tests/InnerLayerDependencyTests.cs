namespace RutaViva.Architecture.Tests;

public sealed class InnerLayerDependencyTests
{
    private const string RootNamespace = "RutaViva.";
    private const string EventBusClientAssembly = "RutaViva.BuildingBlocks.EventBus.Client";
    private const string OuterDependencyReason = "inner layers must not depend on infrastructure, api, hub, gateway or the event bus client";
    private const string ApplicationDependencyReason = "domain must not depend on any application layer";

    private static readonly string[] OuterLayers =
    [
        ArchitectureAssemblies.InfrastructureLayer,
        ArchitectureAssemblies.ApiLayer,
        ArchitectureAssemblies.HubLayer,
        ArchitectureAssemblies.ApiGatewayLayer
    ];

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.InnerLayers), MemberType = typeof(ArchitectureAssemblies))]
    public void InnerLayer_Should_NotReferenceOuterAssemblies_When_AssemblyIsDomainApplicationOrContracts(string assemblyName)
    {
        string[] violations = RutaVivaReferences(assemblyName)
            .Where(IsOuterAssembly)
            .ToArray();

        violations.Should().BeEmpty(OuterDependencyReason);
    }

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Domain), MemberType = typeof(ArchitectureAssemblies))]
    public void Domain_Should_NotReferenceApplicationAssemblies_When_AssemblyIsDomain(string assemblyName)
    {
        string[] violations = RutaVivaReferences(assemblyName)
            .Where(IsApplicationAssembly)
            .ToArray();

        violations.Should().BeEmpty(ApplicationDependencyReason);
    }

    private static string[] RutaVivaReferences(string assemblyName) =>
        ArchitectureAssemblies.Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith(RootNamespace, StringComparison.Ordinal))
            .ToArray();

    private static bool IsOuterAssembly(string assemblyName) =>
        assemblyName == EventBusClientAssembly
        || OuterLayers.Contains(ArchitectureAssemblies.LayerOf(assemblyName));

    private static bool IsApplicationAssembly(string assemblyName) =>
        ArchitectureAssemblies.LayerOf(assemblyName) == ArchitectureAssemblies.ApplicationLayer;
}
