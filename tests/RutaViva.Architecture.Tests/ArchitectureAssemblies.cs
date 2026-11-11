using System.Reflection;

namespace RutaViva.Architecture.Tests;

public static class ArchitectureAssemblies
{
    public const string DomainLayer = "Domain";
    public const string ApplicationLayer = "Application";
    public const string InfrastructureLayer = "Infrastructure";
    public const string ApiLayer = "Api";
    public const string HubLayer = "Hub";

    public const string ApiGatewayLayer = "ApiGateway";
    public const string IntegrationContractsAssembly = "RutaViva.IntegrationContracts";

    private const char SegmentSeparator = '.';
    private const string SourceFolder = "src";
    private const string ProjectPattern = "*.csproj";
    private const string BinFolder = "bin";
    private const string ObjFolder = "obj";

    private static readonly string[] AssemblyNames = DiscoverAssemblyNames();

    public static IReadOnlyList<string> Names => AssemblyNames;

    public static TheoryData<string> All => new(AssemblyNames);

    public static TheoryData<string> Layered => new(WithLayers(DomainLayer, ApplicationLayer, InfrastructureLayer));

    public static TheoryData<string> Domain => new(WithLayers(DomainLayer));

    public static TheoryData<string> Application => new(WithLayers(ApplicationLayer));

    public static TheoryData<string> InnerLayers => new(AssemblyNames.Where(IsInnerLayer));

    public static TheoryData<string> Hosts => new(WithLayers(ApiLayer, HubLayer));

    public static Assembly Load(string assemblyName) => Assembly.Load(assemblyName);

    public static string LayerOf(string assemblyName) => assemblyName[(assemblyName.LastIndexOf(SegmentSeparator) + 1)..];

    public static string ComponentPrefixOf(string assemblyName) => assemblyName[..assemblyName.LastIndexOf(SegmentSeparator)];

    private static string[] DiscoverAssemblyNames()
    {
        string sourcePath = Path.Combine(RepositoryLocator.RootPath(), SourceFolder);

        return Directory.EnumerateFiles(sourcePath, ProjectPattern, SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(sourcePath, path))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsBuildOutput(string sourcePath, string projectPath) =>
        Path.GetRelativePath(sourcePath, projectPath)
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar])
            .Any(segment => segment == BinFolder || segment == ObjFolder);

    private static bool IsInnerLayer(string assemblyName) =>
        assemblyName == IntegrationContractsAssembly
        || LayerOf(assemblyName) is DomainLayer or ApplicationLayer;

    private static IEnumerable<string> WithLayers(params string[] layers) =>
        AssemblyNames.Where(name => layers.Contains(LayerOf(name)));
}
