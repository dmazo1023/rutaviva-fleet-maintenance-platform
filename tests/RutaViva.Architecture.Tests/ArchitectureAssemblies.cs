using System.Reflection;

namespace RutaViva.Architecture.Tests;

public static class ArchitectureAssemblies
{
    public const string DomainLayer = "Domain";
    public const string ApplicationLayer = "Application";
    public const string InfrastructureLayer = "Infrastructure";
    public const string ApiLayer = "Api";
    public const string HubLayer = "Hub";

    private const char SegmentSeparator = '.';

    private static readonly string[] AssemblyNames =
    [
        "RutaViva.BuildingBlocks.Domain",
        "RutaViva.BuildingBlocks.Application",
        "RutaViva.BuildingBlocks.Infrastructure",
        "RutaViva.BuildingBlocks.Api",
        "RutaViva.BuildingBlocks.EventBus.Client",
        "RutaViva.EventBus.Application",
        "RutaViva.EventBus.Infrastructure",
        "RutaViva.EventBus.Hub",
        "RutaViva.IntegrationContracts"
    ];

    public static TheoryData<string> All => new(AssemblyNames);

    public static TheoryData<string> Layered => new(WithLayers(DomainLayer, ApplicationLayer, InfrastructureLayer));

    public static TheoryData<string> Domain => new(WithLayers(DomainLayer));

    public static TheoryData<string> Application => new(WithLayers(ApplicationLayer));

    public static Assembly Load(string assemblyName) => Assembly.Load(assemblyName);

    public static string LayerOf(string assemblyName) => assemblyName[(assemblyName.LastIndexOf(SegmentSeparator) + 1)..];

    public static string ComponentPrefixOf(string assemblyName) => assemblyName[..assemblyName.LastIndexOf(SegmentSeparator)];

    private static IEnumerable<string> WithLayers(params string[] layers) =>
        AssemblyNames.Where(name => layers.Contains(LayerOf(name)));
}
