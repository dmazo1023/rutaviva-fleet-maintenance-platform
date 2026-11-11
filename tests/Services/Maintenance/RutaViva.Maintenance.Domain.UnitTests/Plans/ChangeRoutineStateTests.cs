using System.Globalization;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

public sealed class ChangeRoutineStateTests
{
    private const string UpdatedBy = "ops.user";
    private const string BlankText = " ";
    private const string UpdatedByRequiredCode = "Audit.UpdatedByRequired";
    private const string RoutineNotFoundCode = "Routine.NotFound";
    private const string ReadAtText = "2026-11-13T08:00:00-05:00";
    private const long UpToDateOdometerKm = 12000;
    private const long LastServiceKm = 10000;
    private const long OverdueReadingKm = 21000;
    private const long OverdueOdometerKm = 20000;
    private const long OverdueLastServiceKm = 5000;

    private static readonly DateOnly Today = SampleFleet.LoadDate;

    [Fact]
    public void ChangeRoutineState_Should_EmitStateChangedAndKeepOverdue_When_OverdueRoutineIsDeactivated()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OverdueOdometerKm, OverdueLastServiceKm);

        Result result = plan.ChangeRoutineState(RoutineIdOf(plan), RoutineState.Inactive, UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        plan.Routines.Single().State.Should().Be(RoutineState.Inactive);
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.Overdue);
        RoutineStateChanged changed = plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoutineStateChanged>().Subject;
        changed.Snapshot.State.Should().Be(RoutineState.Inactive);
        changed.Snapshot.DueStatus.Should().Be(RoutineDueStatus.Overdue);
    }

    [Fact]
    public void ChangeRoutineState_Should_NotEmitEvents_When_StateIsRepeated()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OverdueOdometerKm, OverdueLastServiceKm);
        plan.ChangeRoutineState(RoutineIdOf(plan), RoutineState.Inactive, UpdatedBy, SampleCatalog.Catalog, Today);
        SampleFleet.ClearEvents([plan]);

        Result result = plan.ChangeRoutineState(RoutineIdOf(plan), RoutineState.Inactive, UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeRoutineState_Should_ReevaluateRoutine_When_RoutineIsReactivated()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(UpToDateOdometerKm, LastServiceKm);
        plan.ChangeRoutineState(RoutineIdOf(plan), RoutineState.Inactive, UpdatedBy, SampleCatalog.Catalog, Today);
        plan.RecordOdometerReading(Reading(OverdueReadingKm), SampleCatalog.Catalog);
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        SampleFleet.ClearEvents([plan]);

        Result result = plan.ChangeRoutineState(RoutineIdOf(plan), RoutineState.Active, UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsSuccess.Should().BeTrue();
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.Overdue);
        plan.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .Should().Equal(typeof(RoutineStateChanged), typeof(RoutineBecameOverdue));
    }

    [Fact]
    public void RecordOdometerReading_Should_NotEvaluate_When_RoutineWasDeactivated()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(UpToDateOdometerKm, LastServiceKm);
        plan.ChangeRoutineState(RoutineIdOf(plan), RoutineState.Inactive, UpdatedBy, SampleCatalog.Catalog, Today);
        SampleFleet.ClearEvents([plan]);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(OverdueReadingKm), SampleCatalog.Catalog);

        result.Evaluation.Should().Be(ReevaluationOutcome.None);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeRoutineState_Should_FailWithUpdatedByRequired_When_UpdatedByIsBlank()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(UpToDateOdometerKm, LastServiceKm);

        Result result = plan.ChangeRoutineState(RoutineIdOf(plan), RoutineState.Inactive, BlankText, SampleCatalog.Catalog, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UpdatedByRequiredCode);
        plan.Routines.Single().State.Should().Be(RoutineState.Active);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeRoutineState_Should_FailWithRoutineNotFound_When_RoutineDoesNotExist()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(UpToDateOdometerKm, LastServiceKm);

        Result result = plan.ChangeRoutineState(RoutineId.New(), RoutineState.Inactive, UpdatedBy, SampleCatalog.Catalog, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(RoutineNotFoundCode);
    }

    private static RoutineId RoutineIdOf(MaintenancePlan plan) => plan.Routines.Single().Id;

    private static OdometerReading Reading(long kilometers) =>
        OdometerReading.Create(kilometers, DateTimeOffset.Parse(ReadAtText, CultureInfo.InvariantCulture)).Value;
}
