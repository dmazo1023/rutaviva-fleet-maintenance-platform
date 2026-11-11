using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace RutaViva.Architecture.Tests;

public sealed class HostPurityTests
{
    private const string EntityFrameworkNamespace = "Microsoft.EntityFrameworkCore";
    private const string PersistenceSegment = "Persistence";
    private const string HostNamespacePattern = @"\.(Hub\.Hubs|Api\.Endpoints)(\.|$)";
    private const string CompositionRootPattern = "^Program$|CompositionExtensions$";
    private const string FailingTypesReason = "hubs and endpoints must not touch persistence but these types do: {0}";
    private const string TypeSeparator = ", ";
    private const string PersistenceNamespaceFormat = "{0}.{1}";

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Hosts), MemberType = typeof(ArchitectureAssemblies))]
    public void HostTypes_Should_NotDependOnPersistence_When_DeclaredInHubsOrEndpointsNamespaces(string assemblyName)
    {
        TestResult result = Types.InAssembly(ArchitectureAssemblies.Load(assemblyName))
            .That()
            .ResideInNamespaceMatching(HostNamespacePattern)
            .And()
            .DoNotHaveNameMatching(CompositionRootPattern)
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenNamespaces())
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailingTypesReason, string.Join(TypeSeparator, result.FailingTypeNames ?? Array.Empty<string>()));
    }

    private static string[] ForbiddenNamespaces() =>
    [
        EntityFrameworkNamespace,
        .. ArchitectureAssemblies.Names
            .Where(name => ArchitectureAssemblies.LayerOf(name) == ArchitectureAssemblies.InfrastructureLayer)
            .Select(name => string.Format(PersistenceNamespaceFormat, name, PersistenceSegment))
    ];
}
