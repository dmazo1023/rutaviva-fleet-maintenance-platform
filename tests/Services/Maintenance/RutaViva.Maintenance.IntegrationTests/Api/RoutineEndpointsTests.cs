using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class RoutineEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string BulkRoute = "/api/v1/maintenance/routines/bulk";
    private const string DryRunBulkRoute = BulkRoute + "?dryRun=true";
    private const string ParametersRouteFormat = "/api/v1/maintenance/plans/{0}/routines/{1}";
    private const string StateRouteFormat = "/api/v1/maintenance/plans/{0}/routines/{1}/state";
    private const string UnknownPlate = "ZZZ999";
    private const string OilChangeCode = "OIL_CHANGE";
    private const int FirstRowNumber = 1;
    private const int OverLimitRowCount = 501;
    private const string OutcomeProperty = "outcome";
    private const string InvalidOutcome = "Invalid";
    private const string UpdatedByProperty = "updatedBy";
    private const string StateProperty = "state";
    private const string DueSoonThresholdProperty = "dueSoonThresholdPercent";
    private const string Operator = "operator";
    private const string InactiveState = "Inactive";
    private const int ThresholdPercent = 20;

    [Fact]
    public async Task PostBulk_Should_ReturnBadRequest_When_BatchHasMoreThanFiveHundredRows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        BulkRequest<RoutineBulkRow> request = BuildBulk(OverLimitRowCount);

        using HttpResponseMessage response = await client.PostAsJsonAsync(BulkRoute, request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostBulk_Should_ReturnInvalidRow_When_PlateHasNoPlanInDryRun()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        BulkRequest<RoutineBulkRow> request = BuildBulk(FirstRowNumber);

        using HttpResponseMessage response = await client.PostAsJsonAsync(DryRunBulkRoute, request, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body[0].GetProperty(OutcomeProperty).GetString().Should().Be(InvalidOutcome);
    }

    [Fact]
    public async Task PutRoutineParameters_Should_ReturnNotFound_When_PlanDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object> request = new()
        {
            [DueSoonThresholdProperty] = ThresholdPercent,
            [UpdatedByProperty] = Operator
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            string.Format(ParametersRouteFormat, Guid.NewGuid(), Guid.NewGuid()),
            request,
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchRoutineState_Should_ReturnBadRequest_When_UpdatedByIsMissing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object> request = new() { [StateProperty] = InactiveState };
        using HttpRequestMessage message = new(HttpMethod.Patch, string.Format(StateRouteFormat, Guid.NewGuid(), Guid.NewGuid()))
        {
            Content = JsonContent.Create(request)
        };

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static BulkRequest<RoutineBulkRow> BuildBulk(int rowCount)
    {
        List<RoutineBulkRow> rows = Enumerable.Range(FirstRowNumber, rowCount)
            .Select(number => new RoutineBulkRow(number, UnknownPlate, OilChangeCode, null, null, null, null, null))
            .ToList();
        return new BulkRequest<RoutineBulkRow>(rows);
    }
}
