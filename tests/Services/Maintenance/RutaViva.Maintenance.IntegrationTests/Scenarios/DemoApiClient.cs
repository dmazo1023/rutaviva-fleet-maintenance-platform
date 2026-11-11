using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Maintenance.Api.Contracts;

namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

internal sealed class DemoApiClient(HttpClient client)
{
    private const string BulkRoute = "/api/v1/maintenance/routines/bulk";
    private const string OdometerReadingsRoute = "/api/v1/maintenance/odometer-readings";
    private const string TimeEvaluationsRoute = "/api/v1/maintenance/time-evaluations";
    private const string PlansRoute = "/api/v1/maintenance/plans";
    private const string PlanRouteFormat = PlansRoute + "/{0}";
    private const string AlertsRoute = "/api/v1/maintenance/alerts?pageSize=100";
    private const string ItemsProperty = "items";

    public async Task<JsonElement> LoadRoutinesAsync(IReadOnlyList<RoutineBulkRow> rows, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(BulkRoute, new BulkRequest<RoutineBulkRow>(rows), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    public async Task<JsonElement> PostReadingAsync(string plate, long kilometers, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        TelemetryOdometerReadingRequest request = new(plate, kilometers, readAt);
        using HttpResponseMessage response = await client.PostAsJsonAsync(OdometerReadingsRoute, request, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    public async Task<JsonElement> EvaluateAsync(DateOnly asOfDate, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(TimeEvaluationsRoute, new TimeEvaluationRequest(asOfDate), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    public async Task<JsonElement> GetPlanAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(string.Format(PlanRouteFormat, vehicleId), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    public async Task<IReadOnlyList<JsonElement>> GetPlanSummariesAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(PlansRoute, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body.EnumerateArray().ToList();
    }

    public async Task<IReadOnlyList<JsonElement>> GetAlertsAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(AlertsRoute, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body.GetProperty(ItemsProperty).EnumerateArray().ToList();
    }
}
