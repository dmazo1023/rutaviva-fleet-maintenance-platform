using System.Globalization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.Application.UnitTests.Telemetry;

public sealed class RecordOdometerReadingHandlerTests
{
    private const string ConflictCode = "Concurrency.Conflict";
    private const string OtherFailureCode = "Other.Failure";
    private const string ConflictMessage = "The record was modified by another process.";
    private const string AcceptedName = "Accepted";
    private const string NotGreaterName = "NotGreaterThanCurrent";
    private const string IgnoredLogMarker = "OdometerReadingIgnored";
    private const string ReceivedAtText = "2026-11-12T13:30:00+00:00";
    private const long CurrentKm = 10000;
    private const long AdvancedKm = 12000;
    private const long LowerKm = 9000;
    private const long NegativeKm = -1;
    private const int TwoAttempts = 2;

    private static readonly Error Conflict = new(ConflictCode, ConflictMessage, ErrorType.Conflict);
    private static readonly Error OtherFailure = new(OtherFailureCode, ConflictMessage, ErrorType.BusinessRule);

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly IRoutineTypeRepository _routineTypes = Substitute.For<IRoutineTypeRepository>();
    private readonly IOdometerReadingLog _readingLog = Substitute.For<IOdometerReadingLog>();
    private readonly IIntegrationEventPublisher _publisher = Substitute.For<IIntegrationEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CapturingLogger<RecordOdometerReadingHandler> _logger = new();
    private readonly List<OdometerReadingLogEntry> _entries = [];
    private readonly List<IIntegrationEvent> _events = [];
    private readonly DateTimeOffset _receivedAt = DateTimeOffset.Parse(ReceivedAtText, CultureInfo.InvariantCulture);
    private readonly RecordOdometerReadingHandler _handler;

    public RecordOdometerReadingHandlerTests()
    {
        _routineTypes.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(TelemetryTestData.EmptyCatalog());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _readingLog.When(log => log.Append(Arg.Any<OdometerReadingLogEntry>()))
            .Do(call => _entries.Add(call.Arg<OdometerReadingLogEntry>()));
        _publisher.When(publisher => publisher.Enqueue(Arg.Any<IIntegrationEvent>(), Arg.Any<string>()))
            .Do(call => _events.Add(call.Arg<IIntegrationEvent>()));
        _handler = new RecordOdometerReadingHandler(
            _plans,
            _routineTypes,
            _readingLog,
            _publisher,
            _unitOfWork,
            new FakeClock(_receivedAt),
            _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnAccepted_When_ReadingAdvancesOdometer()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(plan, AdvancedKm), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be(OdometerReadingOutcome.Accepted);
        result.Value.IgnoredReason.Should().BeNull();
        plan.Odometer.Kilometers.Should().Be(AdvancedKm);
    }

    [Fact]
    public async Task HandleAsync_Should_AppendAcceptedEntry_When_ReadingAdvancesOdometer()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        await _handler.HandleAsync(CommandFor(plan, AdvancedKm), TestContext.Current.CancellationToken);

        OdometerReadingLogEntry entry = _entries.Should().ContainSingle().Which;
        entry.VehicleId.Should().Be(plan.Id);
        entry.Kilometers.Should().Be(AdvancedKm);
        entry.ReadAt.Should().Be(TelemetryTestData.ReadAt);
        entry.ReceivedAt.Should().Be(_receivedAt);
        entry.Outcome.Should().Be(OdometerReadingOutcome.Accepted);
        entry.IgnoredReason.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_EnqueueEventWithNewOdometer_When_ReadingIsAccepted()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        await _handler.HandleAsync(CommandFor(plan, AdvancedKm), TestContext.Current.CancellationToken);

        OdometerReadingRecordedV1 recorded = _events.Should().ContainSingle().Which
            .Should().BeOfType<OdometerReadingRecordedV1>().Subject;
        recorded.Kilometers.Should().Be(AdvancedKm);
        recorded.CurrentOdometerKm.Should().Be(AdvancedKm);
        recorded.Outcome.Should().Be(AcceptedName);
        recorded.IgnoredReason.Should().BeNull();
        recorded.VehicleId.Should().Be(plan.Id.Value);
        recorded.Plate.Should().Be(plan.Plate.Value);
        recorded.CustomerId.Should().Be(plan.CustomerId.Value);
        recorded.OccurredAt.Should().Be(_receivedAt);
        _publisher.Received(1).Enqueue(Arg.Any<IIntegrationEvent>(), plan.Id.ToString());
    }

    [Theory]
    [InlineData(LowerKm)]
    [InlineData(CurrentKm)]
    public async Task HandleAsync_Should_ReturnIgnored_When_ReadingDoesNotAdvanceOdometer(long kilometers)
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(plan, kilometers), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be(OdometerReadingOutcome.Ignored);
        result.Value.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.NotGreaterThanCurrent);
        plan.Odometer.Kilometers.Should().Be(CurrentKm);
    }

    [Fact]
    public async Task HandleAsync_Should_AppendIgnoredEntry_When_ReadingDoesNotAdvanceOdometer()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        await _handler.HandleAsync(CommandFor(plan, LowerKm), TestContext.Current.CancellationToken);

        OdometerReadingLogEntry entry = _entries.Should().ContainSingle().Which;
        entry.Kilometers.Should().Be(LowerKm);
        entry.Outcome.Should().Be(OdometerReadingOutcome.Ignored);
        entry.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.NotGreaterThanCurrent);
    }

    [Fact]
    public async Task HandleAsync_Should_EnqueueEventWithPreviousOdometer_When_ReadingIsIgnored()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        await _handler.HandleAsync(CommandFor(plan, LowerKm), TestContext.Current.CancellationToken);

        OdometerReadingRecordedV1 recorded = _events.Should().ContainSingle().Which
            .Should().BeOfType<OdometerReadingRecordedV1>().Subject;
        recorded.Kilometers.Should().Be(LowerKm);
        recorded.CurrentOdometerKm.Should().Be(CurrentKm);
        recorded.IgnoredReason.Should().Be(NotGreaterName);
    }

    [Fact]
    public async Task HandleAsync_Should_LogIgnoredReading_When_ReadingDoesNotAdvanceOdometer()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        await _handler.HandleAsync(CommandFor(plan, LowerKm), TestContext.Current.CancellationToken);

        CapturedLog entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain(IgnoredLogMarker).And.Contain(plan.Id.Value.ToString());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_PlanDoesNotExist()
    {
        RecordOdometerReadingCommand command = new(VehicleId.New(), AdvancedKm, TelemetryTestData.ReadAt);

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.MaintenancePlanNotFound);
        _readingLog.DidNotReceive().Append(Arg.Any<OdometerReadingLogEntry>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnValidationError_When_KilometersAreNegative()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(plan, NegativeKm), TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.OdometerReadingKilometersNegative);
        _readingLog.DidNotReceive().Append(Arg.Any<OdometerReadingLogEntry>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSaveError_When_SaveChangesFails()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(OtherFailure));

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(plan, AdvancedKm), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(OtherFailureCode);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_When_OneConflictIsFollowedBySuccess()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(Conflict), Result.Success());

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(plan, AdvancedKm), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _plans.Received(TwoAttempts).GetAsync(plan.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(TwoAttempts).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnConflict_When_TwoConflictsOccur()
    {
        MaintenancePlan plan = ArrangePlan(CurrentKm);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(Conflict));

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(plan, AdvancedKm), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ConflictCode);
        await _unitOfWork.Received(TwoAttempts).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static RecordOdometerReadingCommand CommandFor(MaintenancePlan plan, long kilometers) =>
        new(plan.Id, kilometers, TelemetryTestData.ReadAt);

    private MaintenancePlan ArrangePlan(long odometerKm)
    {
        MaintenancePlan plan = TelemetryTestData.CreatePlan(odometerKm);
        _plans.GetAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        return plan;
    }
}
