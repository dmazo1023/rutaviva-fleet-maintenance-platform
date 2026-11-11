using Microsoft.EntityFrameworkCore;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Infrastructure.Persistence;
using RutaViva.Maintenance.Infrastructure.Persistence.Queries;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Queries;

[Trait("Category", "Integration")]
public sealed class MaintenancePlanQueriesTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string OverduePlate = "TAN201";
    private const string SummaryPlate = "RVA104";
    private const string OtherPlate = "EVA301";
    private const string OilServiceDate = "2026-09-01";
    private const string BrakesServiceDate = "2026-06-10";
    private const string OverdueBrakesServiceDate = "2026-03-01";
    private const string TiresServiceDate = "2025-08-01";
    private const long TanOdometer = 120800L;
    private const long TanOilServiceKilometers = 110000L;
    private const long TanBrakesServiceKilometers = 105000L;
    private const long RvaOdometer = 98000L;
    private const long RvaOilServiceKilometers = 92000L;
    private const long RvaBrakesServiceKilometers = 77000L;
    private const long RvaTiresServiceKilometers = 60000L;
    private const long OtherOdometer = 1000L;
    private const long OtherServiceKilometers = 500L;
    private const decimal OilConsumedPercent = 108m;
    private const decimal BrakesConsumedPercent = 79m;
    private const decimal OverdueBrakesConsumedPercent = 105m;
    private const long OilRemainingKilometers = -800L;
    private const long OilNextDueKilometers = 120000L;
    private const int SingleCount = 1;
    private const int NoCount = 0;
    private const int TwoCount = 2;

    [Fact]
    public async Task GetDetailAsync_Should_ReturnOverdueDistanceTrack_When_OilExceedsInterval()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MaintenancePlan plan = await QueryPlanSeeder.SaveAsync(
            database,
            OverduePlate,
            CustomerId.New(),
            TanOdometer,
            [
                new RoutineSeed(RoutineTypeSeed.OilChangeId, TanOilServiceKilometers, OilServiceDate, true),
                new RoutineSeed(RoutineTypeSeed.BrakesId, TanBrakesServiceKilometers, BrakesServiceDate, true)
            ],
            cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        MaintenancePlanDetailDto? detail = await new MaintenancePlanQueries(context).GetDetailAsync(plan.Id, cancellationToken);

        Assert.NotNull(detail);

        detail.Plate.Should().Be(OverduePlate);
        detail.OdometerKm.Should().Be(TanOdometer);
        detail.WorstDueStatus.Should().Be(RoutineDueStatus.Overdue);
        detail.MaxConsumedPercent.Should().Be(OilConsumedPercent);
        detail.Routines.Should().HaveCount(TwoCount);
        RoutineDetailDto oil = detail.Routines.Single(routine => routine.RoutineTypeCode == RoutineTypeSeed.OilChangeCode);
        RoutineTrackDto? oilDistance = oil.DistanceTrack;
        Assert.NotNull(oilDistance);
        oilDistance.Kind.Should().Be(RoutineTrackDto.DistanceKind);
        oilDistance.ConsumedPercent.Should().Be(OilConsumedPercent);
        oilDistance.Remaining.Should().Be(OilRemainingKilometers);
        oilDistance.Status.Should().Be(RoutineDueStatus.Overdue);
        oil.NextDueKm.Should().Be(OilNextDueKilometers);
        RoutineTrackDto? oilTime = oil.TimeTrack;
        Assert.NotNull(oilTime);
        oilTime.Kind.Should().Be(RoutineTrackDto.TimeKind);
        RoutineDetailDto brakes = detail.Routines.Single(routine => routine.RoutineTypeCode == RoutineTypeSeed.BrakesCode);
        RoutineTrackDto? brakesDistance = brakes.DistanceTrack;
        Assert.NotNull(brakesDistance);
        brakesDistance.ConsumedPercent.Should().Be(BrakesConsumedPercent);
        brakesDistance.Status.Should().Be(RoutineDueStatus.UpToDate);
    }

    [Fact]
    public async Task GetDetailAsync_Should_ReturnNull_When_PlanDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        MaintenancePlanDetailDto? detail = await new MaintenancePlanQueries(context).GetDetailAsync(VehicleId.New(), cancellationToken);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task GetSummariesAsync_Should_CountOnlyActiveRoutines_When_PlanHasInactiveRoutine()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CustomerId customerId = CustomerId.New();
        await QueryPlanSeeder.SaveAsync(
            database,
            SummaryPlate,
            customerId,
            RvaOdometer,
            [
                new RoutineSeed(RoutineTypeSeed.OilChangeId, RvaOilServiceKilometers, OilServiceDate, true),
                new RoutineSeed(RoutineTypeSeed.BrakesId, RvaBrakesServiceKilometers, OverdueBrakesServiceDate, true),
                new RoutineSeed(RoutineTypeSeed.TiresId, RvaTiresServiceKilometers, TiresServiceDate, false)
            ],
            cancellationToken);
        await QueryPlanSeeder.SaveAsync(
            database,
            OtherPlate,
            CustomerId.New(),
            OtherOdometer,
            [new RoutineSeed(RoutineTypeSeed.OilChangeId, OtherServiceKilometers, OilServiceDate, true)],
            cancellationToken);
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);

        IReadOnlyList<MaintenancePlanSummaryDto> summaries = await new MaintenancePlanQueries(context).GetSummariesAsync(customerId, cancellationToken);

        MaintenancePlanSummaryDto summary = summaries.Should().ContainSingle().Subject;
        summary.Plate.Should().Be(SummaryPlate);
        summary.WorstDueStatus.Should().Be(RoutineDueStatus.Overdue);
        summary.MaxConsumedPercent.Should().Be(OverdueBrakesConsumedPercent);
        summary.UpToDateCount.Should().Be(SingleCount);
        summary.DueSoonCount.Should().Be(NoCount);
        summary.OverdueCount.Should().Be(SingleCount);
        summary.InactiveCount.Should().Be(SingleCount);
    }

    [Fact]
    public async Task GetSummariesAsync_Should_ReturnAllPlans_When_NoCustomerFilter()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using MaintenanceDbContext context = await database.CreateDbContextAsync<MaintenanceDbContext>(cancellationToken);
        int expected = await context.MaintenancePlans.CountAsync(cancellationToken);

        IReadOnlyList<MaintenancePlanSummaryDto> summaries = await new MaintenancePlanQueries(context).GetSummariesAsync(null, cancellationToken);

        summaries.Should().HaveCount(expected);
    }
}
