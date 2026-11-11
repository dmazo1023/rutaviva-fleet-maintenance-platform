using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

public sealed class UpdateRoutineOverridesTests
{
    private const string UpdatedBy = "ops.user";
    private const string EmptyText = "";
    private const string BlankText = "   ";
    private const string UpdatedByRequiredCode = "Audit.UpdatedByRequired";
    private const string RoutineNotFoundCode = "Routine.NotFound";
    private const long MotorcycleOdometerKm = 12500;
    private const long MotorcycleLastServiceKm = 10000;
    private const long OverdueOdometerKm = 20000;
    private const long OverdueLastServiceKm = 5000;
    private const int MotorcycleIntervalKm = 3000;
    private const int WideIntervalKm = 20000;
    private const decimal MotorcycleDueSoonPercent = 20m;
    private const decimal LowDueSoonPercent = 5m;
    private const int ExpectedEventCountAfterRevert = 2;

    private static readonly DateOnly Today = SampleFleet.LoadDate;

    [Fact]
    public void UpdateRoutineOverrides_Should_BecomeDueSoon_When_MotorcycleOverridesApply()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm);

        Result result = plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.DueSoon);
        plan.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .Should().Equal(typeof(RoutineParametersChanged), typeof(RoutineBecameDueSoon));
        plan.DomainEvents.OfType<RoutineBecameDueSoon>().Single().IsFirstNoticeInCycle.Should().BeTrue();
    }

    [Fact]
    public void UpdateRoutineOverrides_Should_FillSnapshotWithEffectiveInterval_When_OverridesApply()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm);

        plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);

        RoutineStatusSnapshot snapshot = plan.DomainEvents.OfType<RoutineParametersChanged>().Single().Snapshot;
        snapshot.IntervalKm.Should().Be(MotorcycleIntervalKm);
        snapshot.EffectiveDueSoonPercent.Should().Be(MotorcycleDueSoonPercent);
        snapshot.NextDue.Kilometers.Should().Be(MotorcycleLastServiceKm + MotorcycleIntervalKm);
    }

    [Fact]
    public void UpdateRoutineOverrides_Should_RevertDueSoon_When_OverridesAreRemoved()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm);
        plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);
        SampleFleet.ClearEvents([plan]);

        Result result = plan.UpdateRoutineOverrides(RoutineIdOf(plan), RoutineOverrides.None, UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        plan.DomainEvents.Should().HaveCount(ExpectedEventCountAfterRevert);
        plan.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .Should().Equal(typeof(RoutineParametersChanged), typeof(RoutineDueStatusReverted));
    }

    [Fact]
    public void UpdateRoutineOverrides_Should_NotRepeatFirstNotice_When_RoutineReturnsToDueSoonInTheSameCycle()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm);
        plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);
        plan.UpdateRoutineOverrides(RoutineIdOf(plan), RoutineOverrides.None, UpdatedBy, SampleCatalog.Catalog, Today);
        SampleFleet.ClearEvents([plan]);

        plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);

        plan.DomainEvents.OfType<RoutineBecameDueSoon>().Single().IsFirstNoticeInCycle.Should().BeFalse();
        plan.Routines.Single().DueSoonNotified.Should().BeTrue();
    }

    [Fact]
    public void UpdateRoutineOverrides_Should_RevertDueSoon_When_PercentIsLowered()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm);
        plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);
        SampleFleet.ClearEvents([plan]);
        RoutineOverrides lowered = RoutineOverrides.Create(MotorcycleIntervalKm, null, LowDueSoonPercent).Value;

        plan.UpdateRoutineOverrides(RoutineIdOf(plan), lowered, UpdatedBy, SampleCatalog.Catalog, Today);

        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        plan.DomainEvents.OfType<RoutineDueStatusReverted>().Should().ContainSingle();
    }

    [Fact]
    public void UpdateRoutineOverrides_Should_KeepOverdue_When_IntervalIsExtended()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OverdueOdometerKm, OverdueLastServiceKm);
        RoutineOverrides extended = RoutineOverrides.Create(WideIntervalKm, null, null).Value;

        Result result = plan.UpdateRoutineOverrides(RoutineIdOf(plan), extended, UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.Overdue);
        plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoutineParametersChanged>();
        plan.DomainEvents.OfType<RoutineParametersChanged>().Single().Snapshot.NextDue.Kilometers.Should().Be(OverdueLastServiceKm + WideIntervalKm);
    }

    [Theory]
    [InlineData(EmptyText)]
    [InlineData(BlankText)]
    public void UpdateRoutineOverrides_Should_FailWithUpdatedByRequired_When_UpdatedByIsBlank(string updatedBy)
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm);

        Result result = plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), updatedBy, SampleCatalog.Catalog, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UpdatedByRequiredCode);
        plan.DomainEvents.Should().BeEmpty();
        plan.Routines.Single().Overrides.Should().Be(RoutineOverrides.None);
    }

    [Fact]
    public void UpdateRoutineOverrides_Should_FailWithRoutineNotFound_When_RoutineDoesNotExist()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm);

        Result result = plan.UpdateRoutineOverrides(RoutineId.New(), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(RoutineNotFoundCode);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateRoutineOverrides_Should_OnlyEmitParametersChanged_When_PlanIsSuspended()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(MotorcycleOdometerKm, MotorcycleLastServiceKm, MaintenancePlanStatus.Suspended);

        plan.UpdateRoutineOverrides(RoutineIdOf(plan), MotorcycleOverrides(), UpdatedBy, SampleCatalog.Catalog, Today);

        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoutineParametersChanged>();
    }

    private static RoutineId RoutineIdOf(MaintenancePlan plan) => plan.Routines.Single().Id;

    private static RoutineOverrides MotorcycleOverrides() =>
        RoutineOverrides.Create(MotorcycleIntervalKm, null, MotorcycleDueSoonPercent).Value;
}
