using System.Globalization;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Queries;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Queries;

[Trait("Category", "Integration")]
public sealed class AlertQueriesTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string PagingPlate = "TAN203";
    private const string FilterPlate = "TAN204";
    private const string ServiceDate = "2026-05-01";
    private const string FirstRaisedAtText = "2026-11-12T09:00:00-05:00";
    private const string SecondRaisedAtText = "2026-11-12T10:00:00-05:00";
    private const string ThirdRaisedAtText = "2026-11-12T11:00:00-05:00";
    private const string FourthRaisedAtText = "2026-11-12T12:00:00-05:00";
    private const string AcknowledgedBy = "operator";
    private const long Odometer = 70100L;
    private const long OilServiceKilometers = 60000L;
    private const long BrakesServiceKilometers = 50000L;
    private const long TiresServiceKilometers = 40000L;
    private const int FirstPage = 1;
    private const int SecondPage = 2;
    private const int PageSize = 3;
    private const int LargePageSize = 50;
    private const int TotalAlerts = 4;
    private const int NoAlerts = 0;
    private const int OneAlert = 1;
    private const int ThreeAlerts = 3;

    [Fact]
    public async Task SearchAsync_Should_ReturnPagesOrderedByRaisedAtDescendingWithTotal_When_PageSizeIsSmallerThanTotal()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CustomerId customerId = CustomerId.New();
        await SeedAlertsAsync(customerId, PagingPlate, cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        AlertQueries queries = new(context);

        PagedResult<AlertDto> first = await queries.SearchAsync(new AlertSearchCriteria(null, null, customerId, FirstPage, PageSize), cancellationToken);
        PagedResult<AlertDto> second = await queries.SearchAsync(new AlertSearchCriteria(null, null, customerId, SecondPage, PageSize), cancellationToken);

        first.TotalCount.Should().Be(TotalAlerts);
        first.Page.Should().Be(FirstPage);
        first.PageSize.Should().Be(PageSize);
        first.Items.Should().HaveCount(ThreeAlerts);
        first.Items.Select(alert => alert.RaisedAt).Should().BeInDescendingOrder();
        second.TotalCount.Should().Be(TotalAlerts);
        second.Items.Should().HaveCount(OneAlert);
        second.Items.Single().RaisedAt.Should().Be(ParseInstant(FirstRaisedAtText));
    }

    [Fact]
    public async Task SearchAsync_Should_FilterByStatusAndVehicle_When_CriteriaAreProvided()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CustomerId customerId = CustomerId.New();
        VehicleId vehicleId = await SeedAlertsAsync(customerId, FilterPlate, cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        AlertQueries queries = new(context);

        PagedResult<AlertDto> acknowledged = await queries.SearchAsync(
            new AlertSearchCriteria(AlertStatus.Acknowledged, vehicleId, customerId, FirstPage, LargePageSize),
            cancellationToken);
        PagedResult<AlertDto> active = await queries.SearchAsync(
            new AlertSearchCriteria(AlertStatus.Active, null, customerId, FirstPage, LargePageSize),
            cancellationToken);
        PagedResult<AlertDto> otherCustomer = await queries.SearchAsync(
            new AlertSearchCriteria(null, null, CustomerId.New(), FirstPage, LargePageSize),
            cancellationToken);

        acknowledged.TotalCount.Should().Be(OneAlert);
        acknowledged.Items.Single().AcknowledgedBy.Should().Be(AcknowledgedBy);
        active.TotalCount.Should().Be(ThreeAlerts);
        otherCustomer.Items.Should().BeEmpty();
        otherCustomer.TotalCount.Should().Be(NoAlerts);
    }

    private static DateTimeOffset ParseInstant(string text) => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);

    private static Alert Raise(MaintenancePlan plan, MaintenanceRoutine routine, AlertType type, string raisedAtText)
    {
        AlertOrigin origin = new(plan.Id, plan.Plate, plan.CustomerId, routine.Id, routine.Name, routine.Cycle.Number);
        return Alert.Raise(origin, type, AlertTrigger.Distance, ParseInstant(raisedAtText));
    }

    private async Task<VehicleId> SeedAlertsAsync(CustomerId customerId, string plate, CancellationToken cancellationToken)
    {
        MaintenancePlan plan = await QueryPlanSeeder.SaveAsync(
            database,
            plate,
            customerId,
            Odometer,
            [
                new RoutineSeed(RoutineTypeSeed.OilChangeId, OilServiceKilometers, ServiceDate, true),
                new RoutineSeed(RoutineTypeSeed.BrakesId, BrakesServiceKilometers, ServiceDate, true),
                new RoutineSeed(RoutineTypeSeed.TiresId, TiresServiceKilometers, ServiceDate, true)
            ],
            cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        List<MaintenanceRoutine> routines = plan.Routines.ToList();
        Alert acknowledged = Raise(plan, routines[0], AlertType.DueSoon, FirstRaisedAtText);
        context.Alerts.AddRange(
            acknowledged,
            Raise(plan, routines[1], AlertType.DueSoon, SecondRaisedAtText),
            Raise(plan, routines[2], AlertType.DueSoon, ThirdRaisedAtText),
            Raise(plan, routines[0], AlertType.Overdue, FourthRaisedAtText));
        acknowledged.Acknowledge(AcknowledgedBy, ParseInstant(FourthRaisedAtText)).IsSuccess.Should().BeTrue();
        await context.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }
}
