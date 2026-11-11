using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class TimeEvaluationEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string TimeEvaluationsRoute = "/api/v1/maintenance/time-evaluations";
    private const string ReevaluationsRoute = "/api/v1/maintenance/reevaluations";
    private const string AsOfDateProperty = "asOfDate";
    private const string VehicleIdProperty = "vehicleId";
    private const string AsOfDateText = "2027-01-20";

    [Fact]
    public async Task PostTimeEvaluations_Should_ReturnSameSummaryAsReevaluations_When_BothAreInvokedForTheSameDate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object> request = new() { [AsOfDateProperty] = AsOfDateText };

        using HttpResponseMessage evaluation = await client.PostAsJsonAsync(TimeEvaluationsRoute, request, cancellationToken);
        using HttpResponseMessage reevaluation = await client.PostAsJsonAsync(ReevaluationsRoute, request, cancellationToken);
        JsonElement evaluationBody = await evaluation.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        JsonElement reevaluationBody = await reevaluation.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        evaluation.StatusCode.Should().Be(HttpStatusCode.OK);
        reevaluation.StatusCode.Should().Be(HttpStatusCode.OK);
        reevaluationBody.GetRawText().Should().Be(evaluationBody.GetRawText());
    }

    [Fact]
    public async Task PostTimeEvaluations_Should_ReturnBadRequest_When_AsOfDateIsMissing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            TimeEvaluationsRoute,
            new Dictionary<string, object>(),
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostReevaluations_Should_ReturnNotFound_When_VehicleHasNoPlan()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object> request = new() { [VehicleIdProperty] = Guid.NewGuid() };

        using HttpResponseMessage response = await client.PostAsJsonAsync(ReevaluationsRoute, request, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
