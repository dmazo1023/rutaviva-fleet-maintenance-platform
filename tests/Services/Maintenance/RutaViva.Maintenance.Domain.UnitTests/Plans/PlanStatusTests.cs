using System.Globalization;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

public sealed class PlanStatusTests
{
    private const string ReadAtText = "2026-11-13T08:00:00-05:00";
    private const long OdometerKm = 12000;
    private const long LastServiceKm = 10000;
    private const long OverdueReadingKm = 21000;
    private const int ExpectedOverdueCount = 1;

    private static readonly DateOnly Today = SampleFleet.LoadDate;

    [Fact]
    public void Suspend_Should_EmitStatusChanged_When_PlanIsActive()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OdometerKm, LastServiceKm);

        plan.Suspend();

        plan.Status.Should().Be(MaintenancePlanStatus.Suspended);
        MaintenancePlanStatusChanged changed = plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MaintenancePlanStatusChanged>().Subject;
        changed.Status.Should().Be(MaintenancePlanStatus.Suspended);
        changed.VehicleId.Should().Be(plan.Id);
        changed.Plate.Should().Be(plan.Plate);
        changed.CustomerId.Should().Be(plan.CustomerId);
    }

    [Fact]
    public void Suspend_Should_NotEmitTwice_When_PlanIsAlreadySuspended()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OdometerKm, LastServiceKm);
        plan.Suspend();

        plan.Suspend();

        plan.DomainEvents.OfType<MaintenancePlanStatusChanged>().Should().ContainSingle();
    }

    [Fact]
    public void Resume_Should_EmitStatusChanged_When_PlanIsSuspended()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OdometerKm, LastServiceKm, MaintenancePlanStatus.Suspended);

        ReevaluationOutcome outcome = plan.Resume(Today, SampleCatalog.Catalog);

        outcome.Should().Be(ReevaluationOutcome.None);
        plan.Status.Should().Be(MaintenancePlanStatus.Active);
        MaintenancePlanStatusChanged changed = plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MaintenancePlanStatusChanged>().Subject;
        changed.Status.Should().Be(MaintenancePlanStatus.Active);
    }

    [Fact]
    public void Resume_Should_NotEmitEvents_When_PlanIsAlreadyActive()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OdometerKm, LastServiceKm);

        ReevaluationOutcome outcome = plan.Resume(Today, SampleCatalog.Catalog);

        outcome.Should().Be(ReevaluationOutcome.None);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Resume_Should_NotEmitTwice_When_CalledRepeatedly()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OdometerKm, LastServiceKm, MaintenancePlanStatus.Suspended);
        plan.Resume(Today, SampleCatalog.Catalog);

        plan.Resume(Today, SampleCatalog.Catalog);

        plan.DomainEvents.OfType<MaintenancePlanStatusChanged>().Should().ContainSingle();
    }

    [Fact]
    public void Resume_Should_ReevaluateWithRecordedOdometer_When_ReadingArrivedWhileSuspended()
    {
        MaintenancePlan plan = AdministrationPlanFactory.OilPlan(OdometerKm, LastServiceKm);
        plan.Suspend();
        SampleFleet.ClearEvents([plan]);

        OdometerReadingResult reading = plan.RecordOdometerReading(Reading(OverdueReadingKm), SampleCatalog.Catalog);

        reading.Outcome.Should().Be(OdometerReadingOutcome.Accepted);
        reading.Evaluation.Should().Be(ReevaluationOutcome.None);
        plan.Odometer.Kilometers.Should().Be(OverdueReadingKm);
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        plan.DomainEvents.Should().BeEmpty();

        ReevaluationOutcome outcome = plan.Resume(Today, SampleCatalog.Catalog);

        outcome.NewOverdue.Should().Be(ExpectedOverdueCount);
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.Overdue);
        plan.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .Should().Equal(typeof(MaintenancePlanStatusChanged), typeof(RoutineBecameOverdue));
    }

    private static OdometerReading Reading(long kilometers) =>
        OdometerReading.Create(kilometers, DateTimeOffset.Parse(ReadAtText, CultureInfo.InvariantCulture)).Value;
}
