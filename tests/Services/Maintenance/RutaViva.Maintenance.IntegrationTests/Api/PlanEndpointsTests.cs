using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class PlanEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string PlansRoute = "/api/v1/maintenance/plans";
    private const string PlanRouteFormat = "/api/v1/maintenance/plans/{0}";
    private const string OdometerReadingsRouteFormat = "/api/v1/maintenance/plans/{0}/odometer-readings?limit={1}";
    private const int ExcessiveLimit = 500;
    private const int AcceptedLimit = 10;

    [Fact]
    public async Task GetPlan_Should_ReturnNotFound_When_VehicleHasNoPlan()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            string.Format(PlanRouteFormat, Guid.NewGuid()),
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOdometerReadings_Should_ReturnBadRequest_When_LimitExceedsMaximum()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            string.Format(OdometerReadingsRouteFormat, Guid.NewGuid(), ExcessiveLimit),
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOdometerReadings_Should_ReturnEmptyList_When_VehicleHasNoReadings()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            string.Format(OdometerReadingsRouteFormat, Guid.NewGuid(), AcceptedLimit),
            cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetPlans_Should_ReturnOk_When_NoCustomerFilterIsGiven()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(PlansRoute, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
