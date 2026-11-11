using Microsoft.Extensions.Logging;
using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.ServiceOrders;
using RutaViva.Maintenance.Application.IntegrationEvents.Inbound;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Inbound;

public sealed class ServiceOrderCompletedHandlerTests
{
    private const string UnknownRoutineLogName = "ServiceOrderCompletedForUnknownRoutine";

    private readonly ICommandHandler<RestartRoutineCommand, RoutineId> _restartRoutine =
        Substitute.For<ICommandHandler<RestartRoutineCommand, RoutineId>>();

    private readonly CapturingLogger<ServiceOrderCompletedHandler> _logger = new();
    private readonly ServiceOrderCompletedHandler _handler;

    public ServiceOrderCompletedHandlerTests() => _handler = new ServiceOrderCompletedHandler(_restartRoutine, _logger);

    [Fact]
    public async Task HandleAsync_Should_DelegateRestartCommand_When_OrderHasRoutine()
    {
        Guid vehicleId = Guid.NewGuid();
        Guid routineId = Guid.NewGuid();
        ServiceOrderCompletedV1 integrationEvent = InboundEventFactory.ServiceOrderCompleted(vehicleId, routineId);
        StubRestart(Result<RoutineId>.Success(RoutineId.New()));

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _restartRoutine.Received(1).HandleAsync(
            Arg.Is<RestartRoutineCommand>(command => IsCommandOf(command, integrationEvent, vehicleId, routineId)),
            Arg.Any<CancellationToken>());
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccessWithWarning_When_RoutineIsUnknown()
    {
        ServiceOrderCompletedV1 integrationEvent = InboundEventFactory.ServiceOrderCompleted(Guid.NewGuid(), Guid.NewGuid());
        StubRestart(Result<RoutineId>.Failure(MaintenanceErrors.RoutineNotFound));

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning && entry.Message.Contains(UnknownRoutineLogName));
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccessWithWarning_When_PlanIsUnknown()
    {
        ServiceOrderCompletedV1 integrationEvent = InboundEventFactory.ServiceOrderCompleted(Guid.NewGuid(), Guid.NewGuid());
        StubRestart(Result<RoutineId>.Failure(MaintenanceErrors.MaintenancePlanNotFound));

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning && entry.Message.Contains(UnknownRoutineLogName));
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateFailure_When_RestartFailsForAnotherReason()
    {
        ServiceOrderCompletedV1 integrationEvent = InboundEventFactory.ServiceOrderCompleted(Guid.NewGuid(), Guid.NewGuid());
        StubRestart(Result<RoutineId>.Failure(MaintenanceErrors.ServiceRecordKilometersNegative));

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.ServiceRecordKilometersNegative);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccessWithoutDelegating_When_OrderHasNoRoutine()
    {
        ServiceOrderCompletedV1 integrationEvent = InboundEventFactory.ServiceOrderCompleted(Guid.NewGuid(), null);

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _restartRoutine.DidNotReceive().HandleAsync(Arg.Any<RestartRoutineCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailureWithoutDelegating_When_VehicleIdIsEmpty()
    {
        ServiceOrderCompletedV1 integrationEvent = InboundEventFactory.ServiceOrderCompleted(Guid.Empty, Guid.NewGuid());

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.IdentifierEmpty);
        await _restartRoutine.DidNotReceive().HandleAsync(Arg.Any<RestartRoutineCommand>(), Arg.Any<CancellationToken>());
    }

    private static bool IsCommandOf(RestartRoutineCommand command, ServiceOrderCompletedV1 integrationEvent, Guid vehicleId, Guid routineId) =>
        command.VehicleId.Value == vehicleId
        && command.RoutineId.Value == routineId
        && command.ServicedKm == integrationEvent.ServicedKm
        && command.ServicedAt == integrationEvent.ServicedAt
        && command.ServiceOrderId == integrationEvent.Order.ServiceOrderId;

    private void StubRestart(Result<RoutineId> result) =>
        _restartRoutine.HandleAsync(Arg.Any<RestartRoutineCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
}
