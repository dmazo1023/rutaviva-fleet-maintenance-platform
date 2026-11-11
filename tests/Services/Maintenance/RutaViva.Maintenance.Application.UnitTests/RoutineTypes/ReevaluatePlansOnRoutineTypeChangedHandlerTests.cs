using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Primitives;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.RoutineTypes;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.Events;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.UnitTests.RoutineTypes;

public sealed class ReevaluatePlansOnRoutineTypeChangedHandlerTests
{
    private const string FirstPlate = "RVA101";
    private const string SecondPlate = "RVA102";
    private const string ThirdPlate = "RVA103";
    private const string Code = "OIL_CHANGE";
    private const string Name = "Oil change";
    private const int IntervalKm = 10000;
    private const int IntervalMonths = 6;
    private const decimal DueSoonPercent = 10m;
    private const long OdometerKm = 1000;
    private const long LastServiceKm = 0;
    private const int Year = 2026;
    private const int RegisteredMonth = 1;
    private const int RegisteredDay = 2;
    private const int ServiceDay = 1;
    private const int TodayMonth = 11;
    private const int TodayDay = 12;
    private const int ExpectedPlanCount = 2;

    private static readonly DateTimeOffset ReadAt = new(Year, RegisteredMonth, RegisteredDay, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly RegisteredOn = new(Year, RegisteredMonth, RegisteredDay);
    private static readonly DateOnly ServiceDate = new(Year, RegisteredMonth, ServiceDay);
    private static readonly DateOnly Today = new(Year, TodayMonth, TodayDay);
    private static readonly RoutineTypeId OilChangeId = RoutineTypeId.New();

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ReevaluatePlansOnRoutineTypeChangedHandler _handler;

    public ReevaluatePlansOnRoutineTypeChangedHandlerTests()
    {
        _clock.TodayIn(MaintenanceTimeZone.Bogota).Returns(Today);
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(Catalog());
        _handler = new ReevaluatePlansOnRoutineTypeChangedHandler(_plans, _routineTypes, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_ReevaluateOnlyReturnedPlans_When_RoutineTypeChanges()
    {
        MaintenancePlan first = CreatePlanWithOilRoutine(FirstPlate);
        MaintenancePlan second = CreatePlanWithOilRoutine(SecondPlate);
        MaintenancePlan untouched = CreatePlanWithOilRoutine(ThirdPlate);
        StubPlanIds(first, second);
        StubGet(first, second, untouched);

        await _handler.HandleAsync(ParametersChanged(), TestContext.Current.CancellationToken);

        first.LastTimeEvaluationDate.Should().Be(Today);
        second.LastTimeEvaluationDate.Should().Be(Today);
        untouched.LastTimeEvaluationDate.Should().Be(RegisteredOn);
        await _plans.DidNotReceive().GetAsync(untouched.Id, Arg.Any<CancellationToken>());
        await _plans.Received(ExpectedPlanCount).GetAsync(Arg.Any<VehicleId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_RaiseOverdue_When_ReevaluationFindsExpiredRoutine()
    {
        MaintenancePlan plan = CreatePlanWithOilRoutine(FirstPlate);
        StubPlanIds(plan);
        StubGet(plan);

        await _handler.HandleAsync(ParametersChanged(), TestContext.Current.CancellationToken);

        plan.DomainEvents.OfType<RoutineBecameOverdue>().Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_NotLoadAnything_When_NoPlanUsesTheRoutineType()
    {
        _plans.GetActivePlanIdsWithRoutineTypeAsync(OilChangeId, Arg.Any<CancellationToken>()).Returns(Array.Empty<VehicleId>());

        await _handler.HandleAsync(ParametersChanged(), TestContext.Current.CancellationToken);

        await _plans.DidNotReceive().GetAsync(Arg.Any<VehicleId>(), Arg.Any<CancellationToken>());
        await _routineTypes.DidNotReceive().GetCatalogAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_SkipPlan_When_PlanIsNoLongerFound()
    {
        MaintenancePlan existing = CreatePlanWithOilRoutine(FirstPlate);
        VehicleId missing = VehicleId.New();
        _plans.GetActivePlanIdsWithRoutineTypeAsync(OilChangeId, Arg.Any<CancellationToken>()).Returns([missing, existing.Id]);
        _plans.GetAsync(missing, Arg.Any<CancellationToken>()).Returns(Task.FromResult<MaintenancePlan?>(null));
        StubGet(existing);

        await _handler.HandleAsync(ParametersChanged(), TestContext.Current.CancellationToken);

        existing.LastTimeEvaluationDate.Should().Be(Today);
    }

    private static RoutineTypeParametersChanged ParametersChanged() => new(OilChangeId, ReadAt);

    private static RoutineParameterCatalog Catalog() =>
        new(
        [
            KeyValuePair.Create(
                OilChangeId,
                new RoutineTypeParameters(
                    RoutineTypeCode.Create(Code).Value,
                    Name,
                    MaintenanceInterval.Create(IntervalKm, IntervalMonths).Value,
                    DueSoonThreshold.Create(DueSoonPercent).Value))
        ]);

    private static MaintenancePlan CreatePlanWithOilRoutine(string plate)
    {
        MaintenancePlan plan = MaintenancePlan.Create(
            VehicleId.New(),
            LicensePlate.Create(plate).Value,
            CustomerId.New(),
            MaintenancePlanStatus.Active,
            Odometer.Create(OdometerKm, ReadAt).Value,
            RegisteredOn);
        RoutineRegistration registration = RoutineRegistration.Create(OilChangeId, LastServiceKm, ServiceDate, RoutineOverrides.None);
        plan.RegisterRoutine(registration, Catalog(), RegisteredOn);
        ((IAggregateRoot)plan).ClearDomainEvents();
        return plan;
    }

    private void StubPlanIds(params MaintenancePlan[] returned) =>
        _plans.GetActivePlanIdsWithRoutineTypeAsync(OilChangeId, Arg.Any<CancellationToken>())
            .Returns(returned.Select(plan => plan.Id).ToList());

    private void StubGet(params MaintenancePlan[] plans)
    {
        foreach (MaintenancePlan plan in plans)
        {
            _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        }
    }
}
