using System.Globalization;
using System.Text.Json;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;

namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

[Trait("Category", "Integration")]
public sealed class DemoLoadScenarioTests
{
    private const string Rva101 = "RVA101";
    private const string Rva102 = "RVA102";
    private const string Rva103 = "RVA103";
    private const string Rva104 = "RVA104";
    private const string Tan201 = "TAN201";
    private const string Tan202 = "TAN202";
    private const string Tan203 = "TAN203";
    private const string Eva301 = "EVA301";
    private const string Eva302 = "EVA302";
    private const string Eva30D = "EVA30D";
    private const string UnknownPlate = "XYZ999";
    private const string OilChange = "OIL_CHANGE";
    private const string Brakes = "BRAKES";
    private const string Tires = "TIRES";
    private const string UpToDate = "UpToDate";
    private const string DueSoon = "DueSoon";
    private const string Overdue = "Overdue";
    private const string ValidOutcome = "Valid";
    private const string AcceptedOutcome = "Accepted";
    private const string IgnoredOutcome = "Ignored";
    private const string NotGreaterReason = "NotGreaterThanCurrent";
    private const string UnknownVehicleReason = "UnknownVehicle";
    private const string DistanceAndTimeTrigger = "DistanceAndTime";
    private const string ResolvedStatus = "Resolved";
    private const string ActiveStatus = "Active";
    private const string EscalatedReason = "EscalatedToOverdue";
    private const string FirstEvaluationDate = "2027-01-20";
    private const string SecondEvaluationDate = "2027-02-10";
    private const int ExpectedVehicleCount = 10;
    private const int ExpectedRoutineCount = 15;
    private const int ExpectedDueSoonCount = 4;
    private const int ExpectedOverdueCount = 4;
    private const int NoCount = 0;
    private const int SingleCount = 1;
    private const int PairCount = 2;
    private const long EightyPercentOdometer = 53000;
    private const long OutOfOrderOdometer = 52950;
    private const long DueSoonOdometer = 54100;
    private const long OverdueOdometer = 55050;
    private const long Rva102OverdueOdometer = 82100;
    private const long UnknownVehicleOdometer = 15000;
    private const decimal EightyPercent = 80m;
    private const decimal NinetyOnePercent = 91m;
    private const decimal OverduePercent = 100.5m;
    private const decimal Rva102Percent = 101m;

    private static readonly string RegisteredType = IntegrationEventDescriptor.For<MaintenanceRoutineRegisteredV1>().EventType.ToString();
    private static readonly string OverdueType = IntegrationEventDescriptor.For<MaintenanceRoutineOverdueV1>().EventType.ToString();

    private static readonly IReadOnlyList<ExpectedRoutine> LoadedRoutines =
    [
        new(Rva101, OilChange, UpToDate),
        new(Rva101, Brakes, UpToDate),
        new(Rva102, OilChange, DueSoon),
        new(Rva103, OilChange, DueSoon),
        new(Rva104, OilChange, UpToDate),
        new(Rva104, Brakes, Overdue),
        new(Tan201, OilChange, Overdue),
        new(Tan201, Brakes, UpToDate),
        new(Tan202, OilChange, Overdue),
        new(Tan203, OilChange, Overdue),
        new(Eva301, OilChange, UpToDate),
        new(Eva301, Brakes, UpToDate),
        new(Eva302, Brakes, DueSoon),
        new(Eva302, Tires, UpToDate),
        new(Eva30D, OilChange, DueSoon)
    ];

    private static readonly IReadOnlyList<TelemetryCase> TelemetryCases =
    [
        new(Rva101, EightyPercentOdometer, AcceptedOutcome, null, OilChange, UpToDate, EightyPercent),
        new(Rva101, EightyPercentOdometer, IgnoredOutcome, NotGreaterReason, OilChange, UpToDate, EightyPercent),
        new(Rva101, OutOfOrderOdometer, IgnoredOutcome, NotGreaterReason, OilChange, UpToDate, EightyPercent),
        new(Rva101, DueSoonOdometer, AcceptedOutcome, null, OilChange, DueSoon, NinetyOnePercent),
        new(Rva101, OverdueOdometer, AcceptedOutcome, null, OilChange, Overdue, OverduePercent),
        new(Rva102, Rva102OverdueOdometer, AcceptedOutcome, null, OilChange, Overdue, Rva102Percent)
    ];

    private static readonly IReadOnlyList<TimeEvaluationCase> TimeEvaluationCases =
    [
        new(
            FirstEvaluationDate,
            NoCount,
            PairCount,
            PairCount,
            [new(Rva103, OilChange, Overdue), new(Eva30D, OilChange, Overdue)]),
        new(
            SecondEvaluationDate,
            SingleCount,
            SingleCount,
            SingleCount,
            [new(Rva101, Brakes, DueSoon), new(Eva302, Brakes, Overdue)])
    ];

    [Fact]
    public async Task PostBulk_Should_ReturnFifteenValidRows_When_DemoRoutinesAreLoaded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await DemoScenarioHarness.StartAsync(cancellationToken);
        IReadOnlyList<SampleVehicle> vehicles = SampleCsv.Vehicles();
        await harness.RegisterVehiclesAsync(vehicles, cancellationToken);

        IReadOnlyList<string> outcomes = await harness.LoadRoutinesAsync(SampleCsv.Routines(), cancellationToken);

        vehicles.Should().HaveCount(ExpectedVehicleCount);
        outcomes.Should().HaveCount(ExpectedRoutineCount).And.OnlyContain(outcome => outcome == ValidOutcome);
    }

    [Fact]
    public async Task GetPlan_Should_ReportExpectedDueStatusPerRoutine_When_DemoRoutinesAreLoaded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await DemoScenarioHarness.StartLoadedAsync(cancellationToken);

        foreach (IGrouping<string, ExpectedRoutine> plate in LoadedRoutines.GroupBy(routine => routine.Plate))
        {
            await AssertStatusesAsync(harness, plate.Key, plate.ToList(), cancellationToken);
        }
    }

    [Fact]
    public async Task GetPlans_Should_CountFourDueSoonAndFourOverdueRoutines_When_DemoRoutinesAreLoaded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await DemoScenarioHarness.StartLoadedAsync(cancellationToken);

        IReadOnlyList<JsonElement> summaries = await harness.Api.GetPlanSummariesAsync(cancellationToken);

        summaries.Should().HaveCount(ExpectedVehicleCount);
        summaries.Sum(summary => ScenarioJson.DueSoonCountOf(summary)).Should().Be(ExpectedDueSoonCount);
        summaries.Sum(summary => ScenarioJson.OverdueCountOf(summary)).Should().Be(ExpectedOverdueCount);
    }

    [Fact]
    public async Task GetAlerts_Should_ListFourDueSoonAndFourOverdueAlerts_When_DemoRoutinesAreLoaded()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await DemoScenarioHarness.StartLoadedAsync(cancellationToken);

        IReadOnlyList<JsonElement> alerts = await harness.Api.GetAlertsAsync(cancellationToken);

        alerts.Count(alert => ScenarioJson.AlertTypeOf(alert) == DueSoon).Should().Be(ExpectedDueSoonCount);
        alerts.Count(alert => ScenarioJson.AlertTypeOf(alert) == Overdue).Should().Be(ExpectedOverdueCount);
        JsonElement bothTriggers = alerts.Single(alert => ScenarioJson.PlateOf(alert) == Tan203);
        ScenarioJson.TriggerOf(bothTriggers).Should().Be(DistanceAndTimeTrigger);
    }

    [Fact]
    public async Task OutboxMessages_Should_StoreRegisteredBeforeOverdue_When_RoutineIsOverdueAtRegistration()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await DemoScenarioHarness.StartLoadedAsync(cancellationToken);
        string[] types = [RegisteredType, OverdueType];

        IReadOnlyList<string> singleRoutine = await harness.ReadOutboxTypesAsync(Tan202, types, cancellationToken);

        singleRoutine.Should().Equal(types);
        foreach (string plate in new[] { Rva104, Tan201, Tan203 })
        {
            await AssertRegisteredBeforeOverdueAsync(harness, plate, types, cancellationToken);
        }
    }

    [Fact]
    public async Task PostOdometerReading_Should_MatchReadmeOutcomes_When_SevenTelemetryCasesAreExecuted()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await DemoScenarioHarness.StartLoadedAsync(cancellationToken);

        foreach (TelemetryCase telemetryCase in TelemetryCases)
        {
            await AssertCaseAsync(harness, telemetryCase, cancellationToken);
        }

        await AssertUnknownVehicleAsync(harness, cancellationToken);
        await AssertEscalationAsync(harness, cancellationToken);
    }

    [Fact]
    public async Task PostTimeEvaluations_Should_MatchReadmeResults_When_BothDatesAreEvaluatedAfterTelemetry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DemoScenarioHarness harness = await DemoScenarioHarness.StartLoadedAsync(cancellationToken);
        await ApplyTelemetryAsync(harness, cancellationToken);

        foreach (TimeEvaluationCase evaluationCase in TimeEvaluationCases)
        {
            await AssertEvaluationAsync(harness, evaluationCase, cancellationToken);
        }
    }

    private static async Task AssertStatusesAsync(
        DemoScenarioHarness harness,
        string plate,
        IReadOnlyList<ExpectedRoutine> routines,
        CancellationToken cancellationToken)
    {
        JsonElement plan = await harness.Api.GetPlanAsync(harness.VehicleIdOf(plate), cancellationToken);

        foreach (ExpectedRoutine routine in routines)
        {
            ScenarioJson.DueStatusOf(plan, routine.Code).Should().Be(routine.Status);
        }
    }

    private static async Task AssertRegisteredBeforeOverdueAsync(
        DemoScenarioHarness harness,
        string plate,
        string[] types,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> stored = await harness.ReadOutboxTypesAsync(plate, types, cancellationToken);

        stored.Should().ContainInOrder(RegisteredType, OverdueType);
    }

    private static async Task AssertCaseAsync(DemoScenarioHarness harness, TelemetryCase telemetryCase, CancellationToken cancellationToken)
    {
        JsonElement response = await harness.Api.PostReadingAsync(
            telemetryCase.Plate,
            telemetryCase.Kilometers,
            harness.Clock.UtcNow,
            cancellationToken);
        JsonElement plan = await harness.Api.GetPlanAsync(harness.VehicleIdOf(telemetryCase.Plate), cancellationToken);

        ScenarioJson.OutcomeOf(response).Should().Be(telemetryCase.Outcome);
        ScenarioJson.ReasonOf(response).Should().Be(telemetryCase.Reason);
        ScenarioJson.DueStatusOf(plan, telemetryCase.Code).Should().Be(telemetryCase.Status);
        ScenarioJson.ConsumedPercentOf(plan, telemetryCase.Code).Should().Be(telemetryCase.ConsumedPercent);
    }

    private static async Task AssertUnknownVehicleAsync(DemoScenarioHarness harness, CancellationToken cancellationToken)
    {
        JsonElement response = await harness.Api.PostReadingAsync(
            UnknownPlate,
            UnknownVehicleOdometer,
            harness.Clock.UtcNow,
            cancellationToken);

        ScenarioJson.OutcomeOf(response).Should().Be(IgnoredOutcome);
        ScenarioJson.ReasonOf(response).Should().Be(UnknownVehicleReason);
    }

    private static async Task AssertEscalationAsync(DemoScenarioHarness harness, CancellationToken cancellationToken)
    {
        IReadOnlyList<JsonElement> alerts = await harness.Api.GetAlertsAsync(cancellationToken);
        List<JsonElement> ofVehicle = alerts.Where(alert => ScenarioJson.PlateOf(alert) == Rva101).ToList();

        JsonElement dueSoon = ofVehicle.Single(alert => ScenarioJson.AlertTypeOf(alert) == DueSoon);
        JsonElement overdue = ofVehicle.Single(alert => ScenarioJson.AlertTypeOf(alert) == Overdue);
        ScenarioJson.AlertStatusOf(dueSoon).Should().Be(ResolvedStatus);
        ScenarioJson.ResolutionReasonOf(dueSoon).Should().Be(EscalatedReason);
        ScenarioJson.AlertStatusOf(overdue).Should().Be(ActiveStatus);
    }

    private static async Task ApplyTelemetryAsync(DemoScenarioHarness harness, CancellationToken cancellationToken)
    {
        foreach (TelemetryCase telemetryCase in TelemetryCases)
        {
            await harness.Api.PostReadingAsync(telemetryCase.Plate, telemetryCase.Kilometers, harness.Clock.UtcNow, cancellationToken);
        }
    }

    private static async Task AssertEvaluationAsync(
        DemoScenarioHarness harness,
        TimeEvaluationCase evaluationCase,
        CancellationToken cancellationToken)
    {
        DateOnly asOfDate = DateOnly.Parse(evaluationCase.AsOfDate, CultureInfo.InvariantCulture);

        JsonElement summary = await harness.Api.EvaluateAsync(asOfDate, cancellationToken);

        ScenarioJson.EvaluatedPlansOf(summary).Should().Be(ExpectedVehicleCount);
        ScenarioJson.NewDueSoonOf(summary).Should().Be(evaluationCase.DueSoon);
        ScenarioJson.NewOverdueOf(summary).Should().Be(evaluationCase.Overdue);
        ScenarioJson.OrdersRequestedOf(summary).Should().Be(evaluationCase.Orders);
        foreach (ExpectedRoutine routine in evaluationCase.Routines)
        {
            await AssertStatusesAsync(harness, routine.Plate, [routine], cancellationToken);
        }
    }

    private sealed record ExpectedRoutine(string Plate, string Code, string Status);

    private sealed record TelemetryCase(
        string Plate,
        long Kilometers,
        string Outcome,
        string? Reason,
        string Code,
        string Status,
        decimal ConsumedPercent);

    private sealed record TimeEvaluationCase(
        string AsOfDate,
        int DueSoon,
        int Overdue,
        int Orders,
        IReadOnlyList<ExpectedRoutine> Routines);
}
