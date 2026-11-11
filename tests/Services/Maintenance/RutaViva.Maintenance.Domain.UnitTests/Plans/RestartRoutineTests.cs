using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

public sealed class RestartRoutineTests
{
    private const string UpdatedBy = "ops.user";
    private const string RoutineNotFoundCode = "Routine.NotFound";
    private const long Rva104OdometerKm = 98000;
    private const long ServiceKm = 98400;
    private const long LowerServiceKm = 90000;
    private const long DueSoonOdometerKm = 19500;
    private const long DueSoonLastServiceKm = 10000;
    private const int ExpectedSecondCycle = 2;
    private const int ExpectedFirstCycle = 1;
    private const int BrakesIntervalKm = 20000;
    private const int BrakesIntervalMonths = 12;

    private static readonly DateOnly ServiceDate = new(2026, 11, 12);

    [Fact]
    public void RestartRoutine_Should_StartSecondCycleAndAdvanceOdometer_When_ServiceExceedsOdometer()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(plan, SampleCatalog.BrakesCode);
        SampleFleet.ClearEvents([plan]);

        Result result = plan.RestartRoutine(brakes.Id, Service(ServiceKm), Guid.NewGuid(), SampleCatalog.Catalog);

        result.IsSuccess.Should().BeTrue();
        brakes.Cycle.Number.Should().Be(ExpectedSecondCycle);
        brakes.Cycle.StartKm.Should().Be(ServiceKm);
        brakes.Cycle.StartDate.Should().Be(ServiceDate);
        brakes.DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        brakes.DueSoonNotified.Should().BeFalse();
        brakes.LastTrigger.Should().BeNull();
        plan.Odometer.Kilometers.Should().Be(ServiceKm);
    }

    [Fact]
    public void RestartRoutine_Should_EmitRestartedWithSnapshot_When_RoutineIsRestarted()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(plan, SampleCatalog.BrakesCode);
        SampleFleet.ClearEvents([plan]);
        Guid serviceOrderId = Guid.NewGuid();

        plan.RestartRoutine(brakes.Id, Service(ServiceKm), serviceOrderId, SampleCatalog.Catalog);

        RoutineRestarted restarted = plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoutineRestarted>().Subject;
        restarted.ServiceOrderId.Should().Be(serviceOrderId);
        restarted.Service.Kilometers.Should().Be(ServiceKm);
        restarted.Snapshot.CycleNumber.Should().Be(ExpectedSecondCycle);
        restarted.Snapshot.DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        restarted.Snapshot.OdometerKm.Should().Be(ServiceKm);
        restarted.Snapshot.NextDue.Kilometers.Should().Be(ServiceKm + BrakesIntervalKm);
        restarted.Snapshot.NextDue.Date.Should().Be(ServiceDate.AddMonths(BrakesIntervalMonths));
    }

    [Fact]
    public void RestartRoutine_Should_KeepOdometer_When_ServiceKilometersAreLower()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(plan, SampleCatalog.BrakesCode);

        plan.RestartRoutine(brakes.Id, Service(LowerServiceKm), Guid.NewGuid(), SampleCatalog.Catalog);

        plan.Odometer.Kilometers.Should().Be(Rva104OdometerKm);
        brakes.Cycle.StartKm.Should().Be(LowerServiceKm);
    }

    [Fact]
    public void RestartRoutine_Should_ResetDueSoonNotice_When_RoutineWasDueSoon()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(DueSoonOdometerKm, DueSoonLastServiceKm);
        MaintenanceRoutine oil = plan.Routines.Single();
        oil.DueStatus.Should().Be(RoutineDueStatus.DueSoon);
        oil.DueSoonNotified.Should().BeTrue();

        plan.RestartRoutine(oil.Id, Service(DueSoonOdometerKm), Guid.NewGuid(), SampleCatalog.Catalog);

        oil.DueSoonNotified.Should().BeFalse();
        oil.DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        oil.Cycle.Number.Should().Be(ExpectedSecondCycle);
    }

    [Fact]
    public void RestartRoutine_Should_KeepRoutineInactive_When_RoutineWasDeactivated()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(DueSoonOdometerKm, DueSoonLastServiceKm);
        MaintenanceRoutine oil = plan.Routines.Single();
        plan.ChangeRoutineState(oil.Id, RoutineState.Inactive, UpdatedBy, SampleCatalog.Catalog, ServiceDate);

        Result result = plan.RestartRoutine(oil.Id, Service(DueSoonOdometerKm), Guid.NewGuid(), SampleCatalog.Catalog);

        result.IsSuccess.Should().BeTrue();
        oil.State.Should().Be(RoutineState.Inactive);
        oil.Cycle.Number.Should().Be(ExpectedSecondCycle);
    }

    [Fact]
    public void RestartRoutine_Should_FailWithRoutineNotFound_When_RoutineDoesNotExist()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        SampleFleet.ClearEvents([plan]);

        Result result = plan.RestartRoutine(RoutineId.New(), Service(ServiceKm), Guid.NewGuid(), SampleCatalog.Catalog);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(RoutineNotFoundCode);
        plan.DomainEvents.Should().BeEmpty();
        plan.Odometer.Kilometers.Should().Be(Rva104OdometerKm);
    }

    [Fact]
    public void RestartRoutine_Should_KeepFirstCycleOfOtherRoutines_When_OneRoutineIsRestarted()
    {
        MaintenancePlan plan = SampleFleet.Load()[SampleFleet.Rva104];
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(plan, SampleCatalog.BrakesCode);

        plan.RestartRoutine(brakes.Id, Service(ServiceKm), Guid.NewGuid(), SampleCatalog.Catalog);

        SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode).Cycle.Number.Should().Be(ExpectedFirstCycle);
    }

    private static ServiceRecord Service(long kilometers) => ServiceRecord.Create(kilometers, ServiceDate).Value;
}
