namespace RutaViva.Architecture.Tests;

public sealed class ArchitectureAssembliesDiscoveryTests
{
    private static readonly string[] ExpectedAssemblies =
    [
        "RutaViva.BuildingBlocks.Domain",
        "RutaViva.BuildingBlocks.Application",
        "RutaViva.BuildingBlocks.Infrastructure",
        "RutaViva.BuildingBlocks.Api",
        "RutaViva.BuildingBlocks.EventBus.Client",
        "RutaViva.EventBus.Application",
        "RutaViva.EventBus.Infrastructure",
        "RutaViva.EventBus.Hub",
        "RutaViva.IntegrationContracts",
        "RutaViva.ServiceDefaults"
    ];

    [Fact]
    public void Discovery_Should_FindCurrentSourceAssemblies_When_ScanningSrcProjects()
    {
        ArchitectureAssemblies.Names.Should().Contain(ExpectedAssemblies);
    }

    [Fact]
    public void Discovery_Should_NotRepeatAssemblies_When_ScanningSrcProjects()
    {
        ArchitectureAssemblies.Names.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.All), MemberType = typeof(ArchitectureAssemblies))]
    public void Discovery_Should_LoadAssembly_When_ProjectIsUnderSrc(string assemblyName)
    {
        ArchitectureAssemblies.Load(assemblyName).GetName().Name.Should().Be(assemblyName);
    }
}
