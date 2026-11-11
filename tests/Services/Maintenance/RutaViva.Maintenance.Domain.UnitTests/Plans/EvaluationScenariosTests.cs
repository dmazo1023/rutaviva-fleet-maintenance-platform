using System.Globalization;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Plans;

public sealed class EvaluationScenariosTests
{
    private const string ReadAt0800 = "2026-11-12T08:00:00-05:00";
    private const string ReadAt0755 = "2026-11-12T07:55:00-05:00";
    private const string ReadAt0930 = "2026-11-12T09:30:00-05:00";
    private const string ReadAt1000 = "2026-11-12T10:00:00-05:00";
    private const string ReadAt1100 = "2026-11-12T11:00:00-05:00";
    private const string ReadAtLaterDay = "2026-12-01T10:00:00-05:00";
    private const long Reading53000 = 53000;
    private const long Reading52950 = 52950;
    private const long Reading54100 = 54100;
    private const long Reading55050 = 55050;
    private const long Reading82100 = 82100;
    private const long Reading31000 = 31000;
    private const long Rva101OdometerKm = 52300;
    private const long SuspendedOdometerKm = 12000;
    private const long SuspendedLastServiceKm = 10000;
    private const long SuspendedReadingKm = 21000;
    private const decimal Case4ConsumedPercent = 91m;
    private const decimal Case5ConsumedPercent = 100.5m;
    private const int ExpectedDueSoonOnLoad = 4;
    private const int ExpectedOverdueOnLoad = 4;
    private const int ExpectedRoutinesOnLoad = 15;
    private const int ExpectedFirstCycle = 1;
    private const int ExpectedOverdueOnJanuary = 2;
    private const int ExpectedDueSoonOnFebruary = 1;
    private const int ExpectedOverdueOnFebruary = 1;
    private const int BogotaOffsetHours = -5;

    private static readonly DateOnly January20 = new(2027, 1, 20);
    private static readonly DateOnly February10 = new(2027, 2, 10);
    private static readonly DateOnly LastServiceDate = new(2026, 10, 1);

    [Fact]
    public void RegisterRoutine_Should_LeaveFourDueSoonAndFourOverdue_When_SampleFleetIsLoaded()
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = SampleFleet.Load();

        List<MaintenanceRoutine> routines = fleet.Values.SelectMany(plan => plan.Routines).ToList();
        routines.Should().HaveCount(ExpectedRoutinesOnLoad);
        routines.Count(routine => routine.DueStatus == RoutineDueStatus.DueSoon).Should().Be(ExpectedDueSoonOnLoad);
        routines.Count(routine => routine.DueStatus == RoutineDueStatus.Overdue).Should().Be(ExpectedOverdueOnLoad);
    }

    [Fact]
    public void RegisterRoutine_Should_EmitFourDueSoonAndFourOverdueEvents_When_SampleFleetIsLoaded()
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = SampleFleet.Load();

        List<RoutineBecameDueSoon> dueSoon = fleet.Values.SelectMany(plan => plan.DomainEvents).OfType<RoutineBecameDueSoon>().ToList();
        List<RoutineBecameOverdue> overdue = fleet.Values.SelectMany(plan => plan.DomainEvents).OfType<RoutineBecameOverdue>().ToList();
        dueSoon.Should().HaveCount(ExpectedDueSoonOnLoad);
        dueSoon.Should().OnlyContain(domainEvent => domainEvent.IsFirstNoticeInCycle);
        overdue.Should().HaveCount(ExpectedOverdueOnLoad);
        fleet.Values.SelectMany(plan => plan.DomainEvents).OfType<RoutineRegistered>().Should().HaveCount(ExpectedRoutinesOnLoad);
    }

    [Theory]
    [InlineData(SampleFleet.Rva101, SampleCatalog.OilChangeCode, RoutineDueStatus.UpToDate)]
    [InlineData(SampleFleet.Rva101, SampleCatalog.BrakesCode, RoutineDueStatus.UpToDate)]
    [InlineData(SampleFleet.Rva102, SampleCatalog.OilChangeCode, RoutineDueStatus.DueSoon)]
    [InlineData(SampleFleet.Rva103, SampleCatalog.OilChangeCode, RoutineDueStatus.DueSoon)]
    [InlineData(SampleFleet.Rva104, SampleCatalog.OilChangeCode, RoutineDueStatus.UpToDate)]
    [InlineData(SampleFleet.Rva104, SampleCatalog.BrakesCode, RoutineDueStatus.Overdue)]
    [InlineData(SampleFleet.Tan201, SampleCatalog.OilChangeCode, RoutineDueStatus.Overdue)]
    [InlineData(SampleFleet.Tan201, SampleCatalog.BrakesCode, RoutineDueStatus.UpToDate)]
    [InlineData(SampleFleet.Tan202, SampleCatalog.OilChangeCode, RoutineDueStatus.Overdue)]
    [InlineData(SampleFleet.Tan203, SampleCatalog.OilChangeCode, RoutineDueStatus.Overdue)]
    [InlineData(SampleFleet.Eva301, SampleCatalog.OilChangeCode, RoutineDueStatus.UpToDate)]
    [InlineData(SampleFleet.Eva301, SampleCatalog.BrakesCode, RoutineDueStatus.UpToDate)]
    [InlineData(SampleFleet.Eva302, SampleCatalog.BrakesCode, RoutineDueStatus.DueSoon)]
    [InlineData(SampleFleet.Eva302, SampleCatalog.TiresCode, RoutineDueStatus.UpToDate)]
    [InlineData(SampleFleet.Eva30D, SampleCatalog.OilChangeCode, RoutineDueStatus.DueSoon)]
    public void RegisterRoutine_Should_ReachExpectedStatus_When_SampleRoutineIsLoaded(string plate, string code, RoutineDueStatus expected)
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = SampleFleet.Load();

        MaintenanceRoutine routine = SampleFleet.RoutineOf(fleet[plate], code);

        routine.DueStatus.Should().Be(expected);
    }

    [Theory]
    [InlineData(SampleFleet.Rva102, SampleCatalog.OilChangeCode, RoutineDueStatus.DueSoon, AlertTrigger.Distance)]
    [InlineData(SampleFleet.Rva103, SampleCatalog.OilChangeCode, RoutineDueStatus.DueSoon, AlertTrigger.Time)]
    [InlineData(SampleFleet.Eva302, SampleCatalog.BrakesCode, RoutineDueStatus.DueSoon, AlertTrigger.Distance)]
    [InlineData(SampleFleet.Eva30D, SampleCatalog.OilChangeCode, RoutineDueStatus.DueSoon, AlertTrigger.Distance)]
    [InlineData(SampleFleet.Rva104, SampleCatalog.BrakesCode, RoutineDueStatus.Overdue, AlertTrigger.Distance)]
    [InlineData(SampleFleet.Tan201, SampleCatalog.OilChangeCode, RoutineDueStatus.Overdue, AlertTrigger.Distance)]
    [InlineData(SampleFleet.Tan202, SampleCatalog.OilChangeCode, RoutineDueStatus.Overdue, AlertTrigger.Time)]
    [InlineData(SampleFleet.Tan203, SampleCatalog.OilChangeCode, RoutineDueStatus.Overdue, AlertTrigger.DistanceAndTime)]
    public void RegisterRoutine_Should_RecordTrigger_When_SampleRoutineIsAlerted(string plate, string code, RoutineDueStatus status, AlertTrigger trigger)
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = SampleFleet.Load();

        MaintenanceRoutine routine = SampleFleet.RoutineOf(fleet[plate], code);

        routine.DueStatus.Should().Be(status);
        routine.LastTrigger.Should().Be(trigger);
    }

    [Fact]
    public void RecordOdometerReading_Should_AcceptAndKeepOilUpToDate_When_Reading53000Arrives()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva101);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading53000, ReadAt0800), SampleCatalog.Catalog);

        result.Outcome.Should().Be(OdometerReadingOutcome.Accepted);
        result.IgnoredReason.Should().BeNull();
        result.Evaluation.Should().Be(ReevaluationOutcome.None);
        plan.Odometer.Kilometers.Should().Be(Reading53000);
        SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordOdometerReading_Should_IgnoreReading_When_SameKilometersArriveAgain()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva101);
        plan.RecordOdometerReading(Reading(Reading53000, ReadAt0800), SampleCatalog.Catalog);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading53000, ReadAt0800), SampleCatalog.Catalog);

        result.Outcome.Should().Be(OdometerReadingOutcome.Ignored);
        result.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.NotGreaterThanCurrent);
        plan.Odometer.Kilometers.Should().Be(Reading53000);
    }

    [Fact]
    public void RecordOdometerReading_Should_IgnoreReading_When_ReadingArrivesOutOfOrder()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva101);
        plan.RecordOdometerReading(Reading(Reading53000, ReadAt0800), SampleCatalog.Catalog);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading52950, ReadAt0755), SampleCatalog.Catalog);

        result.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.NotGreaterThanCurrent);
        plan.Odometer.Kilometers.Should().Be(Reading53000);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(Rva101OdometerKm)]
    [InlineData(Rva101OdometerKm - 1)]
    [InlineData(0L)]
    public void RecordOdometerReading_Should_IgnoreReading_When_KilometersAreNotGreaterThanCurrent(long kilometers)
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva101);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(kilometers, ReadAt0800), SampleCatalog.Catalog);

        result.Outcome.Should().Be(OdometerReadingOutcome.Ignored);
        result.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.NotGreaterThanCurrent);
        plan.Odometer.Kilometers.Should().Be(Rva101OdometerKm);
    }

    [Fact]
    public void RecordOdometerReading_Should_EmitDueSoonAsFirstNotice_When_OilReachesNinetyOnePercent()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva101);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading54100, ReadAt0930), SampleCatalog.Catalog);

        result.Evaluation.NewDueSoon.Should().Be(1);
        result.Evaluation.NewOverdue.Should().Be(0);
        RoutineBecameDueSoon dueSoon = plan.DomainEvents.OfType<RoutineBecameDueSoon>().Single();
        dueSoon.IsFirstNoticeInCycle.Should().BeTrue();
        dueSoon.Trigger.Should().Be(AlertTrigger.Distance);
        dueSoon.Snapshot.ConsumedPercent.Should().Be(Case4ConsumedPercent);
        dueSoon.Snapshot.OdometerKm.Should().Be(Reading54100);
        dueSoon.OccurredAt.Should().Be(Reading(Reading54100, ReadAt0930).ReadAt);
    }

    [Fact]
    public void RecordOdometerReading_Should_EmitOnlyOverdue_When_DueSoonOilEscalates()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva101);
        plan.RecordOdometerReading(Reading(Reading54100, ReadAt0930), SampleCatalog.Catalog);
        SampleFleet.ClearEvents([plan]);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading55050, ReadAt1100), SampleCatalog.Catalog);

        result.Evaluation.NewOverdue.Should().Be(1);
        plan.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoutineBecameOverdue>();
        RoutineBecameOverdue overdue = plan.DomainEvents.OfType<RoutineBecameOverdue>().Single();
        overdue.Trigger.Should().Be(AlertTrigger.Distance);
        overdue.Snapshot.CycleNumber.Should().Be(ExpectedFirstCycle);
        overdue.Snapshot.ConsumedPercent.Should().Be(Case5ConsumedPercent);
        SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.Overdue);
    }

    [Fact]
    public void RecordOdometerReading_Should_NotEmitAnotherOverdue_When_RoutineIsAlreadyOverdue()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva101);
        plan.RecordOdometerReading(Reading(Reading55050, ReadAt1100), SampleCatalog.Catalog);
        SampleFleet.ClearEvents([plan]);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading55050 + 1, ReadAt1100), SampleCatalog.Catalog);

        result.Evaluation.Should().Be(ReevaluationOutcome.None);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordOdometerReading_Should_EscalateToOverdue_When_DueSoonOilOfRva102ExceedsInterval()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva102);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading82100, ReadAt1000), SampleCatalog.Catalog);

        result.Evaluation.NewOverdue.Should().Be(1);
        SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.Overdue);
        plan.DomainEvents.OfType<RoutineBecameDueSoon>().Should().BeEmpty();
    }

    [Fact]
    public void RecordOdometerReading_Should_NotAdvanceTime_When_ReadingIsAccepted()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva103);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading31000, ReadAtLaterDay), SampleCatalog.Catalog);

        result.Evaluation.Should().Be(ReevaluationOutcome.None);
        plan.LastTimeEvaluationDate.Should().Be(SampleFleet.LoadDate);
        SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.DueSoon);
    }

    [Fact]
    public void RecordOdometerReading_Should_OnlyUpdateOdometer_When_PlanIsSuspended()
    {
        MaintenancePlan plan = new MaintenancePlanBuilder()
            .WithOdometer(SuspendedOdometerKm)
            .WithStatus(MaintenancePlanStatus.Suspended)
            .Build();
        RoutineRegistration registration = RoutineRegistration.Create(
            SampleCatalog.IdOf(SampleCatalog.OilChangeCode),
            SuspendedLastServiceKm,
            LastServiceDate,
            RoutineOverrides.None);
        plan.RegisterRoutine(registration, SampleCatalog.Catalog, SampleFleet.LoadDate);
        SampleFleet.ClearEvents([plan]);

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(SuspendedReadingKm, ReadAt0800), SampleCatalog.Catalog);

        result.Outcome.Should().Be(OdometerReadingOutcome.Accepted);
        result.Evaluation.Should().Be(ReevaluationOutcome.None);
        plan.Odometer.Kilometers.Should().Be(SuspendedReadingKm);
        plan.Routines.Single().DueStatus.Should().Be(RoutineDueStatus.UpToDate);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecordOdometerReading_Should_SkipRoutine_When_RoutineIsInactive()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva102);
        Deactivate(SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode));

        OdometerReadingResult result = plan.RecordOdometerReading(Reading(Reading82100, ReadAt1000), SampleCatalog.Catalog);

        result.Evaluation.Should().Be(ReevaluationOutcome.None);
        SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.DueSoon);
        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reevaluate_Should_FlagRva103AndEva30DAsOverdueByTime_When_DateIsJanuary20()
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = LoadedFleetAfterTelemetry();

        ReevaluationOutcome outcome = ReevaluateAll(fleet, January20);

        outcome.NewOverdue.Should().Be(ExpectedOverdueOnJanuary);
        outcome.NewDueSoon.Should().Be(0);
        SampleFleet.RoutineOf(fleet[SampleFleet.Rva103], SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.Overdue);
        SampleFleet.RoutineOf(fleet[SampleFleet.Rva103], SampleCatalog.OilChangeCode).LastTrigger.Should().Be(AlertTrigger.Time);
        SampleFleet.RoutineOf(fleet[SampleFleet.Eva30D], SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.Overdue);
        SampleFleet.RoutineOf(fleet[SampleFleet.Eva30D], SampleCatalog.OilChangeCode).LastTrigger.Should().Be(AlertTrigger.Time);
        fleet.Values.Should().OnlyContain(plan => plan.LastTimeEvaluationDate == January20);
    }

    [Fact]
    public void Reevaluate_Should_FlagRva101BrakesDueSoonAndEva302BrakesOverdue_When_DateIsFebruary10()
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = LoadedFleetAfterTelemetry();
        ReevaluateAll(fleet, January20);
        SampleFleet.ClearEvents(fleet.Values);

        ReevaluationOutcome outcome = ReevaluateAll(fleet, February10);

        outcome.NewDueSoon.Should().Be(ExpectedDueSoonOnFebruary);
        outcome.NewOverdue.Should().Be(ExpectedOverdueOnFebruary);
        MaintenanceRoutine brakes = SampleFleet.RoutineOf(fleet[SampleFleet.Rva101], SampleCatalog.BrakesCode);
        brakes.DueStatus.Should().Be(RoutineDueStatus.DueSoon);
        brakes.LastTrigger.Should().Be(AlertTrigger.Time);
        brakes.DueSoonNotified.Should().BeTrue();
        MaintenanceRoutine eva302Brakes = SampleFleet.RoutineOf(fleet[SampleFleet.Eva302], SampleCatalog.BrakesCode);
        eva302Brakes.DueStatus.Should().Be(RoutineDueStatus.Overdue);
        eva302Brakes.LastTrigger.Should().Be(AlertTrigger.Time);
    }

    [Fact]
    public void Reevaluate_Should_NotGoBackInTime_When_DateIsEarlierThanLastEvaluation()
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = LoadedFleetAfterTelemetry();
        ReevaluateAll(fleet, February10);
        SampleFleet.ClearEvents(fleet.Values);

        ReevaluationOutcome outcome = ReevaluateAll(fleet, January20);

        outcome.Should().Be(ReevaluationOutcome.None);
        fleet.Values.Should().OnlyContain(plan => plan.LastTimeEvaluationDate == February10);
        fleet.Values.SelectMany(plan => plan.DomainEvents).Should().BeEmpty();
    }

    [Fact]
    public void Reevaluate_Should_StampEventsAtBogotaStartOfDay_When_RoutineBecomesOverdue()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva103);

        plan.Reevaluate(January20, SampleCatalog.Catalog);

        RoutineBecameOverdue overdue = plan.DomainEvents.OfType<RoutineBecameOverdue>().Single();
        overdue.OccurredAt.Should().Be(new DateTimeOffset(January20.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(BogotaOffsetHours)));
    }

    [Fact]
    public void Reevaluate_Should_DoNothing_When_PlanIsSuspended()
    {
        MaintenancePlan plan = new MaintenancePlanBuilder().WithStatus(MaintenancePlanStatus.Suspended).Build();

        ReevaluationOutcome outcome = plan.Reevaluate(February10, SampleCatalog.Catalog);

        outcome.Should().Be(ReevaluationOutcome.None);
        plan.LastTimeEvaluationDate.Should().Be(SampleFleet.LoadDate);
    }

    [Fact]
    public void Reevaluate_Should_SkipRoutine_When_RoutineIsInactive()
    {
        MaintenancePlan plan = LoadedPlan(SampleFleet.Rva103);
        Deactivate(SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode));

        ReevaluationOutcome outcome = plan.Reevaluate(January20, SampleCatalog.Catalog);

        outcome.Should().Be(ReevaluationOutcome.None);
        SampleFleet.RoutineOf(plan, SampleCatalog.OilChangeCode).DueStatus.Should().Be(RoutineDueStatus.DueSoon);
        plan.LastTimeEvaluationDate.Should().Be(January20);
    }

    private static OdometerReading Reading(long kilometers, string readAtText) =>
        OdometerReading.Create(kilometers, DateTimeOffset.Parse(readAtText, CultureInfo.InvariantCulture)).Value;

    private static MaintenancePlan LoadedPlan(string plate)
    {
        MaintenancePlan plan = SampleFleet.Load()[plate];
        SampleFleet.ClearEvents([plan]);
        return plan;
    }

    private static IReadOnlyDictionary<string, MaintenancePlan> LoadedFleetAfterTelemetry()
    {
        IReadOnlyDictionary<string, MaintenancePlan> fleet = SampleFleet.Load();
        MaintenancePlan rva101 = fleet[SampleFleet.Rva101];
        rva101.RecordOdometerReading(Reading(Reading53000, ReadAt0800), SampleCatalog.Catalog);
        rva101.RecordOdometerReading(Reading(Reading54100, ReadAt0930), SampleCatalog.Catalog);
        rva101.RecordOdometerReading(Reading(Reading55050, ReadAt1100), SampleCatalog.Catalog);
        fleet[SampleFleet.Rva102].RecordOdometerReading(Reading(Reading82100, ReadAt1000), SampleCatalog.Catalog);
        SampleFleet.ClearEvents(fleet.Values);
        return fleet;
    }

    private static ReevaluationOutcome ReevaluateAll(IReadOnlyDictionary<string, MaintenancePlan> fleet, DateOnly asOfDate)
    {
        List<ReevaluationOutcome> outcomes = fleet.Values
            .Select(plan => plan.Reevaluate(asOfDate, SampleCatalog.Catalog))
            .ToList();

        return new ReevaluationOutcome(outcomes.Sum(outcome => outcome.NewDueSoon), outcomes.Sum(outcome => outcome.NewOverdue));
    }

    private static void Deactivate(MaintenanceRoutine routine) =>
        typeof(MaintenanceRoutine)
            .GetProperty(nameof(MaintenanceRoutine.State))!
            .SetValue(routine, RoutineState.Inactive);
}
