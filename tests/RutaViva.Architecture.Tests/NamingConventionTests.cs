namespace RutaViva.Architecture.Tests;

public sealed class NamingConventionTests
{
    private const string ServicesFolder = "Services";
    private const string SourceFolder = "src";
    private const string ProjectPattern = "*.csproj";
    private const char SegmentSeparator = '.';

    private static readonly string[] ForbiddenSegments = ["Common", "Shared", "Utils", "Core"];

    private static readonly string[] ServiceLayers =
    [
        ArchitectureAssemblies.DomainLayer,
        ArchitectureAssemblies.ApplicationLayer,
        ArchitectureAssemblies.InfrastructureLayer,
        ArchitectureAssemblies.ApiLayer
    ];

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.All), MemberType = typeof(ArchitectureAssemblies))]
    public void AssemblyName_Should_NotContainGenericSegments_When_FollowingAdr0026(string assemblyName)
    {
        string[] offending = assemblyName.Split(SegmentSeparator)
            .Where(segment => ForbiddenSegments.Contains(segment))
            .ToArray();

        offending.Should().BeEmpty("ADR-0026 forbids Common, Shared, Utils and Core as name segments");
    }

    [Fact]
    public void ServiceProjects_Should_EndWithLayerName_When_LocatedUnderSrcServices()
    {
        string servicesPath = Path.Combine(RepositoryLocator.RootPath(), SourceFolder, ServicesFolder);

        string[] violations = ServiceProjectNames(servicesPath)
            .Where(name => !ServiceLayers.Contains(ArchitectureAssemblies.LayerOf(name)))
            .ToArray();

        violations.Should().BeEmpty("service assemblies must end in Domain, Application, Infrastructure or Api");
    }

    private static string[] ServiceProjectNames(string servicesPath)
    {
        if (!Directory.Exists(servicesPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(servicesPath, ProjectPattern, SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToArray();
    }
}
