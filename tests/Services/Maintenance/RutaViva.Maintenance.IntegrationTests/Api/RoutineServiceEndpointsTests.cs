using System.Net;
using System.Net.Http.Json;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class RoutineServiceEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string ServicesRouteFormat = "/api/v1/maintenance/plans/{0}/routines/{1}/services";
    private const string ServicedKmProperty = "servicedKm";
    private const string ServicedAtProperty = "servicedAt";
    private const long ServicedKm = 15000;

    private static readonly DateTimeOffset ServicedAt = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PostService_Should_ReturnNotFound_When_PlanDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object> request = new()
        {
            [ServicedKmProperty] = ServicedKm,
            [ServicedAtProperty] = ServicedAt
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(ServicesRoute(), request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostService_Should_ReturnBadRequest_When_ServiceDataIsMissing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object> request = new()
        {
            [ServicedKmProperty] = ServicedKm
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(ServicesRoute(), request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string ServicesRoute() => string.Format(ServicesRouteFormat, Guid.NewGuid(), Guid.NewGuid());
}
