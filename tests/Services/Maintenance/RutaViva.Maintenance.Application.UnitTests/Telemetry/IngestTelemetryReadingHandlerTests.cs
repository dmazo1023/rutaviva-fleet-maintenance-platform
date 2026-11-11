using System.Globalization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.Application.UnitTests.Telemetry;

public sealed class IngestTelemetryReadingHandlerTests
{
    private const string PaddedLowercasePlate = " rva101 ";
    private const string MalformedPlate = "not-a-plate";
    private const string UnknownVehicleLogMarker = "OdometerReadingForUnknownVehicle";
    private const long ReceivedKm = 15000;
    private const string ClockText = "2026-11-12T13:30:00+00:00";

    private readonly IMaintenancePlanRepository _plans = Substitute.For<IMaintenancePlanRepository>();
    private readonly ICommandHandler<RecordOdometerReadingCommand, TelemetryReadingOutcome> _recorder =
        Substitute.For<ICommandHandler<RecordOdometerReadingCommand, TelemetryReadingOutcome>>();
    private readonly CapturingLogger<IngestTelemetryReadingHandler> _logger = new();
    private readonly IngestTelemetryReadingHandler _handler;

    public IngestTelemetryReadingHandlerTests()
    {
        _handler = new IngestTelemetryReadingHandler(_plans, _recorder, _logger);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnUnknownVehicle_When_PlateHasNoPlan()
    {
        _plans.FindVehicleIdByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>()).Returns((VehicleId?)null);

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(PaddedLowercasePlate), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be(OdometerReadingOutcome.Ignored);
        result.Value.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.UnknownVehicle);
        await _recorder.DidNotReceive().HandleAsync(Arg.Any<RecordOdometerReadingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_LogWarning_When_PlateHasNoPlan()
    {
        _plans.FindVehicleIdByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>()).Returns((VehicleId?)null);

        await _handler.HandleAsync(CommandFor(PaddedLowercasePlate), TestContext.Current.CancellationToken);

        CapturedLog entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain(UnknownVehicleLogMarker);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnUnknownVehicleWithoutLookup_When_PlateFormatIsInvalid()
    {
        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(MalformedPlate), TestContext.Current.CancellationToken);

        result.Value.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.UnknownVehicle);
        await _plans.DidNotReceive().FindVehicleIdByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>());
        await _recorder.DidNotReceive().HandleAsync(Arg.Any<RecordOdometerReadingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_NotAppendHistory_When_PlateHasNoPlan()
    {
        IOdometerReadingLog readingLog = Substitute.For<IOdometerReadingLog>();
        RecordOdometerReadingHandler realRecorder = CreateRealRecorder(readingLog);
        IngestTelemetryReadingHandler handler = new(_plans, realRecorder, _logger);
        _plans.FindVehicleIdByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>()).Returns((VehicleId?)null);

        Result<TelemetryReadingOutcome> result = await handler.HandleAsync(CommandFor(PaddedLowercasePlate), TestContext.Current.CancellationToken);

        result.Value.IgnoredReason.Should().Be(OdometerReadingIgnoredReason.UnknownVehicle);
        readingLog.DidNotReceive().Append(Arg.Any<OdometerReadingLogEntry>());
    }

    [Fact]
    public async Task HandleAsync_Should_DelegateWithNormalizedPlate_When_PlateHasPlan()
    {
        VehicleId vehicleId = VehicleId.New();
        TelemetryReadingOutcome expected = new(OdometerReadingOutcome.Accepted, null);
        _plans.FindVehicleIdByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>()).Returns((VehicleId?)vehicleId);
        _recorder.HandleAsync(Arg.Any<RecordOdometerReadingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TelemetryReadingOutcome>.Success(expected));

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(PaddedLowercasePlate), TestContext.Current.CancellationToken);

        result.Value.Should().Be(expected);
        await _plans.Received(1).FindVehicleIdByPlateAsync(
            Arg.Is<LicensePlate>(plate => plate.Value == TelemetryTestData.PlateValue),
            Arg.Any<CancellationToken>());
        await _recorder.Received(1).HandleAsync(
            Arg.Is<RecordOdometerReadingCommand>(command =>
                command.VehicleId == vehicleId
                && command.Kilometers == ReceivedKm
                && command.ReadAt == TelemetryTestData.ReadAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnRecorderError_When_RecorderFails()
    {
        _plans.FindVehicleIdByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>()).Returns((VehicleId?)VehicleId.New());
        _recorder.HandleAsync(Arg.Any<RecordOdometerReadingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TelemetryReadingOutcome>.Failure(MaintenanceErrors.MaintenancePlanNotFound));

        Result<TelemetryReadingOutcome> result = await _handler.HandleAsync(CommandFor(PaddedLowercasePlate), TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.MaintenancePlanNotFound);
    }

    private static IngestTelemetryReadingCommand CommandFor(string plate) =>
        new(plate, ReceivedKm, TelemetryTestData.ReadAt);

    private static RecordOdometerReadingHandler CreateRealRecorder(IOdometerReadingLog readingLog) =>
        new(
            Substitute.For<IMaintenancePlanRepository>(),
            Substitute.For<IRoutineTypeRepository>(),
            readingLog,
            Substitute.For<IIntegrationEventPublisher>(),
            Substitute.For<IUnitOfWork>(),
            new FakeClock(DateTimeOffset.Parse(ClockText, CultureInfo.InvariantCulture)),
            new CapturingLogger<RecordOdometerReadingHandler>());
}
