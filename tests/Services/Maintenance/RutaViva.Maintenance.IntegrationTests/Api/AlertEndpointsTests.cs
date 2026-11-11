using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Repositories;
using RutaViva.Maintenance.IntegrationTests.Persistence.Repositories;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class AlertEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string AlertsRoute = "/api/v1/maintenance/alerts";
    private const string InvalidPageRoute = AlertsRoute + "?page=0";
    private const string AcknowledgeRouteFormat = "/api/v1/maintenance/alerts/{0}/acknowledge";
    private const string ResolvedAlertPlate = "RES123";
    private const string RaisedAtText = "2026-11-12T09:00:00-05:00";
    private const string AcknowledgedByProperty = "acknowledgedBy";
    private const string CodeProperty = "code";
    private const string ItemsProperty = "items";
    private const string Operator = "operator";
    private const string InvalidTransitionCode = "Alert.InvalidTransition";

    [Fact]
    public async Task PostAcknowledge_Should_ReturnUnprocessableEntity_When_AlertIsAlreadyResolved()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        AlertId alertId = await SeedResolvedAlertAsync(cancellationToken);
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            string.Format(AcknowledgeRouteFormat, alertId.Value),
            BuildAcknowledgeRequest(Operator),
            cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        body.GetProperty(CodeProperty).GetString().Should().Be(InvalidTransitionCode);
    }

    [Fact]
    public async Task PostAcknowledge_Should_ReturnNotFound_When_AlertDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            string.Format(AcknowledgeRouteFormat, Guid.NewGuid()),
            BuildAcknowledgeRequest(Operator),
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostAcknowledge_Should_ReturnBadRequest_When_AcknowledgedByIsEmpty()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            string.Format(AcknowledgeRouteFormat, Guid.NewGuid()),
            BuildAcknowledgeRequest(string.Empty),
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAlerts_Should_ReturnPagedResult_When_NoFiltersAreGiven()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(AlertsRoute, cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty(ItemsProperty).ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAlerts_Should_ReturnBadRequest_When_PageIsZero()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceApiFactory factory = await MaintenanceApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(InvalidPageRoute, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Dictionary<string, object> BuildAcknowledgeRequest(string acknowledgedBy) =>
        new() { [AcknowledgedByProperty] = acknowledgedBy };

    private static DateTimeOffset ParseRaisedAt() =>
        DateTimeOffset.Parse(RaisedAtText, CultureInfo.InvariantCulture);

    private async Task<AlertId> SeedResolvedAlertAsync(CancellationToken cancellationToken)
    {
        MaintenancePlan plan = await PlanSeeding.SavePlanAsync(database, ResolvedAlertPlate, cancellationToken);
        MaintenanceRoutine routine = plan.Routines.Single();
        AlertOrigin origin = new(plan.Id, plan.Plate, plan.CustomerId, routine.Id, routine.Name, routine.Cycle.Number);
        Alert alert = Alert.Raise(origin, AlertType.DueSoon, AlertTrigger.Distance, ParseRaisedAt());
        alert.Resolve(AlertResolutionReason.RoutineRestarted, ParseRaisedAt()).IsSuccess.Should().BeTrue();
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        new AlertRepository(context).Add(alert);
        await context.SaveChangesAsync(cancellationToken);
        return alert.Id;
    }
}
