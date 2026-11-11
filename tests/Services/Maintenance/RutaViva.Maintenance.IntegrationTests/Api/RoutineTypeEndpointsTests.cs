using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class RoutineTypeEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string RoutineTypesRoute = "/api/v1/maintenance/routine-types";
    private const string RoutineTypeRouteFormat = "/api/v1/maintenance/routine-types/{0}";
    private const string IntervalKmProperty = "intervalKm";
    private const string DueSoonThresholdProperty = "dueSoonThresholdPercent";
    private const string UpdatedByProperty = "updatedBy";
    private const string IdProperty = "id";
    private const string Operator = "operator";
    private const int IntervalKilometers = 10000;
    private const int OutOfRangePercent = 100;

    [Fact]
    public async Task GetRoutineTypes_Should_ReturnSeededCatalog_When_DatabaseIsCreated()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(RoutineTypesRoute, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PutRoutineType_Should_ReturnBadRequest_When_DueSoonPercentIsOutOfRange()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        string routineTypeId = await ReadFirstRoutineTypeIdAsync(client, cancellationToken);
        Dictionary<string, object> request = new()
        {
            [IntervalKmProperty] = IntervalKilometers,
            [DueSoonThresholdProperty] = OutOfRangePercent,
            [UpdatedByProperty] = Operator
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            string.Format(RoutineTypeRouteFormat, routineTypeId),
            request,
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<string> ReadFirstRoutineTypeIdAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(RoutineTypesRoute, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body[0].GetProperty(IdProperty).GetString()!;
    }
}
