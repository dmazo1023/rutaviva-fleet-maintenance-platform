using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

public sealed class RegisterRoutineTests
{
    private const long OverdueOdometerKm = 20000;
    private const long OverdueLastServiceKm = 5000;
    private const long UpToDateOdometerKm = 12000;
    private const long UpToDateLastServiceKm = 10000;
    private const long SmallOdometerKm = 1000;
    private const long LastServiceAboveOdometerKm = 1500;
    private const long NewVehicleOdometerKm = 12345;
    private const int MotorcycleIntervalKm = 3000;
    private const int MotorcycleIntervalMonths = 3;
    private const long MotorcycleOdometerKm = 12500;
    private const long MotorcycleLastServiceKm = 10000;
    private const decimal ExpectedConsumedPercent = 150m;
    private const int ExpectedCycleNumber = 1;
    private const int ExpectedRoutineCount = 1;
    private const int DaysBeforeToday = 1;
    private const decimal MotorcycleDueSoonPercent = 20m;

    private static readonly DateOnly Today = SampleFleet.LoadDate;
    private static readonly DateOnly LastServiceDate = new(2026, 10, 1);
    private static readonly DateOnly OldServiceDate = new(2026, 6, 1);

    [Fact]
    public void RegisterRoutine_Should_EmitRegisteredThenOverdue_When_RoutineIsAlreadyOverdue()
    {
        MaintenancePlan plan = PlanWithOdometer(OverdueOdometerKm);
        RoutineRegistration registration = Registration(SampleCatalog.OilChangeCode, OverdueLastServiceKm, OldServiceDate);

        Result<RoutineId> result = plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        plan.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .Should().Equal(typeof(RoutineRegistered), typeof(RoutineBecameOverdue));
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.Overdue);
        plan.Routines.Single().LastTrigger.Should().Be(AlertTrigger.Distance);
    }

    [Fact]
    public void RegisterRoutine_Should_RegisterWithUpToDateSnapshot_When_RoutineIsAlreadyOverdue()
    {
        MaintenancePlan plan = PlanWithOdometer(OverdueOdometerKm);

        plan.RegisterRoutine(Registration(SampleCatalog.OilChangeCode, OverdueLastServiceKm, OldServiceDate), SampleCatalog.Catalog, Today);

        RoutineRegistered registered = plan.DomainEvents.OfType<RoutineRegistered>().Single();
        registered.Snapshot.DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        RoutineBecameOverdue overdue = plan.DomainEvents.OfType<RoutineBecameOverdue>().Single();
        overdue.Snapshot.DueStatus.Should().Be(RoutineDueStatus.Overdue);
        overdue.Snapshot.ConsumedPercent.Should().Be(ExpectedConsumedPercent);
    }

    [Fact]
    public void RegisterRoutine_Should_FillSnapshot_When_RoutineIsRegistered()
    {
        MaintenancePlan plan = PlanWithOdometer(UpToDateOdometerKm);

        Result<RoutineId> result = plan.RegisterRoutine(Registration(SampleCatalog.OilChangeCode, UpToDateLastServiceKm, LastServiceDate), SampleCatalog.Catalog, Today);

        RoutineStatusSnapshot snapshot = plan.DomainEvents.OfType<RoutineRegistered>().Single().Snapshot;
        snapshot.RoutineId.Should().Be(result.Value);
        snapshot.VehicleId.Should().Be(plan.Id);
        snapshot.Plate.Should().Be(plan.Plate);
        snapshot.CustomerId.Should().Be(plan.CustomerId);
        snapshot.RoutineTypeCode.Value.Should().Be(SampleCatalog.OilChangeCode);
        snapshot.State.Should().Be(RoutineState.Active);
        snapshot.CycleNumber.Should().Be(ExpectedCycleNumber);
        snapshot.CycleStartKm.Should().Be(UpToDateLastServiceKm);
        snapshot.CycleStartDate.Should().Be(LastServiceDate);
        snapshot.OdometerKm.Should().Be(UpToDateOdometerKm);
        snapshot.NextDue.Kilometers.Should().Be(UpToDateLastServiceKm + SampleCatalog.OilChangeKm);
        snapshot.NextDue.Date.Should().Be(LastServiceDate.AddMonths(SampleCatalog.OilChangeMonths));
    }

    [Fact]
    public void RegisterRoutine_Should_EmitOnlyRegistered_When_RoutineIsUpToDate()
    {
        MaintenancePlan plan = PlanWithOdometer(UpToDateOdometerKm);

        plan.RegisterRoutine(Registration(SampleCatalog.OilChangeCode, UpToDateLastServiceKm, LastServiceDate), SampleCatalog.Catalog, Today);

        plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoutineRegistered>();
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
    }

    [Theory]
    [InlineData(20, RoutineDueStatus.DueSoon)]
    [InlineData(10, RoutineDueStatus.UpToDate)]
    public void RegisterRoutine_Should_ApplyOverridePercent_When_MotorcycleRoutineIsRegistered(int percent, RoutineDueStatus expected)
    {
        MaintenancePlan plan = PlanWithOdometer(MotorcycleOdometerKm);
        RoutineOverrides overrides = RoutineOverrides.Create(MotorcycleIntervalKm, MotorcycleIntervalMonths, percent).Value;
        RoutineRegistration registration = RoutineRegistration.Create(
            SampleCatalog.IdOf(SampleCatalog.OilChangeCode),
            MotorcycleLastServiceKm,
            LastServiceDate,
            overrides);

        plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        plan.Routines.Single().DueStatus.Should().Be(expected);
    }

    [Fact]
    public void RegisterRoutine_Should_EmitDueSoonAsFirstNotice_When_RoutineStartsDueSoon()
    {
        MaintenancePlan plan = PlanWithOdometer(MotorcycleOdometerKm);
        RoutineOverrides overrides = RoutineOverrides.Create(MotorcycleIntervalKm, MotorcycleIntervalMonths, MotorcycleDueSoonPercent).Value;
        RoutineRegistration registration = RoutineRegistration.Create(
            SampleCatalog.IdOf(SampleCatalog.OilChangeCode),
            MotorcycleLastServiceKm,
            LastServiceDate,
            overrides);

        plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        plan.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .Should().Equal(typeof(RoutineRegistered), typeof(RoutineBecameDueSoon));
        RoutineBecameDueSoon dueSoon = plan.DomainEvents.OfType<RoutineBecameDueSoon>().Single();
        dueSoon.IsFirstNoticeInCycle.Should().BeTrue();
        dueSoon.Trigger.Should().Be(AlertTrigger.Distance);
        plan.Routines.Single().DueSoonNotified.Should().BeTrue();
    }

    [Fact]
    public void RegisterRoutine_Should_FailWithAlreadyExists_When_TypeIsRepeated()
    {
        MaintenancePlan plan = PlanWithOdometer(UpToDateOdometerKm);
        RoutineRegistration registration = Registration(SampleCatalog.OilChangeCode, UpToDateLastServiceKm, LastServiceDate);
        plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        Result<RoutineId> result = plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.RoutineAlreadyExists);
        plan.Routines.Should().HaveCount(ExpectedRoutineCount);
    }

    [Fact]
    public void RegisterRoutine_Should_FailWithLastServiceKmAboveOdometer_When_ServiceExceedsOdometer()
    {
        MaintenancePlan plan = PlanWithOdometer(SmallOdometerKm);

        Result<RoutineId> result = plan.RegisterRoutine(Registration(SampleCatalog.OilChangeCode, LastServiceAboveOdometerKm, LastServiceDate), SampleCatalog.Catalog, Today);

        result.Error.Should().Be(MaintenanceErrors.RoutineLastServiceKmAboveOdometer);
        plan.Routines.Should().BeEmpty();
        plan.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void RegisterRoutine_Should_FailWithIncompleteLastService_When_OnlyKilometersOrDateIsGiven(bool withKilometers, bool withDate)
    {
        MaintenancePlan plan = PlanWithOdometer(UpToDateOdometerKm);
        long? kilometers = withKilometers ? UpToDateLastServiceKm : null;
        DateOnly? date = withDate ? LastServiceDate : null;
        RoutineRegistration registration = RoutineRegistration.Create(SampleCatalog.IdOf(SampleCatalog.OilChangeCode), kilometers, date, RoutineOverrides.None);

        Result<RoutineId> result = plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        result.Error.Should().Be(MaintenanceErrors.RoutineIncompleteLastService);
        plan.Routines.Should().BeEmpty();
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RegisterRoutine_Should_StartCycleAtOdometerAndLoadDate_When_NoLastServiceIsGiven()
    {
        MaintenancePlan plan = PlanWithOdometer(NewVehicleOdometerKm);
        RoutineRegistration registration = RoutineRegistration.Create(SampleCatalog.IdOf(SampleCatalog.BrakesCode), null, null, RoutineOverrides.None);

        Result<RoutineId> result = plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        MaintenanceCycle cycle = plan.Routines.Single().Cycle;
        cycle.Number.Should().Be(ExpectedCycleNumber);
        cycle.StartKm.Should().Be(NewVehicleOdometerKm);
        cycle.StartDate.Should().Be(Today);
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
    }

    [Fact]
    public void RegisterRoutine_Should_AcceptServiceRecord_When_LastServiceIsGivenAsRecord()
    {
        MaintenancePlan plan = PlanWithOdometer(UpToDateOdometerKm);
        ServiceRecord service = ServiceRecord.Create(UpToDateLastServiceKm, LastServiceDate).Value;
        RoutineRegistration registration = RoutineRegistration.Create(SampleCatalog.IdOf(SampleCatalog.OilChangeCode), service, RoutineOverrides.None);

        plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        plan.Routines.Single().Cycle.StartKm.Should().Be(UpToDateLastServiceKm);
        plan.Routines.Single().Cycle.StartDate.Should().Be(LastServiceDate);
    }

    [Fact]
    public void RegisterRoutine_Should_FailWithRoutineTypeNotFound_When_TypeIsNotInCatalog()
    {
        MaintenancePlan plan = PlanWithOdometer(UpToDateOdometerKm);
        RoutineRegistration registration = RoutineRegistration.Create(RoutineTypeId.New(), null, null, RoutineOverrides.None);

        Result<RoutineId> result = plan.RegisterRoutine(registration, SampleCatalog.Catalog, Today);

        result.Error.Should().Be(MaintenanceErrors.RoutineTypeNotFound);
    }

    [Fact]
    public void RegisterRoutine_Should_NotEvaluate_When_PlanIsSuspended()
    {
        MaintenancePlan plan = new MaintenancePlanBuilder()
            .WithOdometer(OverdueOdometerKm)
            .WithStatus(MaintenancePlanStatus.Suspended)
            .Build();

        plan.RegisterRoutine(Registration(SampleCatalog.OilChangeCode, OverdueLastServiceKm, OldServiceDate), SampleCatalog.Catalog, Today);

        plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoutineRegistered>();
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
    }

    [Fact]
    public void RegisterRoutine_Should_AdvanceLastEvaluationDate_When_PlanIsActive()
    {
        DateOnly registeredOn = Today.AddDays(-DaysBeforeToday);
        MaintenancePlan plan = new MaintenancePlanBuilder().WithOdometer(UpToDateOdometerKm).WithRegisteredOn(registeredOn).Build();

        plan.RegisterRoutine(Registration(SampleCatalog.OilChangeCode, UpToDateLastServiceKm, LastServiceDate), SampleCatalog.Catalog, Today);

        plan.LastTimeEvaluationDate.Should().Be(Today);
    }

    [Fact]
    public void Routines_Should_BeExposedAsReadOnlyCollection_When_PlanIsCreated()
    {
        MaintenancePlan plan = PlanWithOdometer(UpToDateOdometerKm);

        plan.Routines.Should().BeAssignableTo<IReadOnlyCollection<MaintenanceRoutine>>();
    }

    private static MaintenancePlan PlanWithOdometer(long kilometers) =>
        new MaintenancePlanBuilder().WithOdometer(kilometers).Build();

    private static RoutineRegistration Registration(string code, long lastServiceKm, DateOnly lastServiceDate) =>
        RoutineRegistration.Create(SampleCatalog.IdOf(code), lastServiceKm, lastServiceDate, RoutineOverrides.None);
}
