using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace RutaViva.Architecture.Tests;

public sealed class LayerDependencyTests
{
    private const string EntityFrameworkNamespace = "Microsoft.EntityFrameworkCore";

    private static readonly IReadOnlyDictionary<string, string[]> ForbiddenLayers = new Dictionary<string, string[]>
    {
        [ArchitectureAssemblies.DomainLayer] =
        [
            ArchitectureAssemblies.ApplicationLayer,
            ArchitectureAssemblies.InfrastructureLayer,
            ArchitectureAssemblies.ApiLayer,
            ArchitectureAssemblies.HubLayer
        ],
        [ArchitectureAssemblies.ApplicationLayer] =
        [
            ArchitectureAssemblies.InfrastructureLayer,
            ArchitectureAssemblies.ApiLayer,
            ArchitectureAssemblies.HubLayer
        ],
        [ArchitectureAssemblies.InfrastructureLayer] =
        [
            ArchitectureAssemblies.ApiLayer,
            ArchitectureAssemblies.HubLayer
        ]
    };

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Layered), MemberType = typeof(ArchitectureAssemblies))]
    public void Layer_Should_NotDependOnOuterLayers_When_AssemblyFollowsCleanArchitecture(string assemblyName)
    {
        string prefix = ArchitectureAssemblies.ComponentPrefixOf(assemblyName);
        string[] forbidden = ForbiddenLayers[ArchitectureAssemblies.LayerOf(assemblyName)]
            .Select(layer => $"{prefix}.{layer}")
            .ToArray();

        TestResult result = Types.InAssembly(ArchitectureAssemblies.Load(assemblyName))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("outer layers are forbidden but these types depend on them: {0}", string.Join(", ", FailingNames(result)));
    }

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Domain), MemberType = typeof(ArchitectureAssemblies))]
    public void Domain_Should_NotDependOnEntityFramework_When_AssemblyIsDomain(string assemblyName)
    {
        TestResult result = Types.InAssembly(ArchitectureAssemblies.Load(assemblyName))
            .ShouldNot()
            .HaveDependencyOn(EntityFrameworkNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Domain must not use EF Core but these types do: {0}", string.Join(", ", FailingNames(result)));
    }

    private static IEnumerable<string> FailingNames(TestResult result) => result.FailingTypeNames ?? Array.Empty<string>();
}
