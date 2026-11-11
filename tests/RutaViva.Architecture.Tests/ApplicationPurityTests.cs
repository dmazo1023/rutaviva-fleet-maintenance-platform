using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace RutaViva.Architecture.Tests;

public sealed class ApplicationPurityTests
{
    private static readonly string[] ForbiddenNamespaces =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "CsvHelper",
        "ClosedXML"
    ];

    [Theory]
    [MemberData(nameof(ArchitectureAssemblies.Application), MemberType = typeof(ArchitectureAssemblies))]
    public void Application_Should_NotDependOnFrameworkOrFileLibraries_When_AssemblyIsApplication(string assemblyName)
    {
        TestResult result = Types.InAssembly(ArchitectureAssemblies.Load(assemblyName))
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Application must stay framework independent but these types depend on forbidden libraries: {0}", string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
