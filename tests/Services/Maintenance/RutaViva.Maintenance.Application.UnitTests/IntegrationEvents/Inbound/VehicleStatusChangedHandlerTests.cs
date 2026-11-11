using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.Maintenance.Application.IntegrationEvents.Inbound;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Inbound;

public sealed class VehicleStatusChangedHandlerTests
{
    private readonly ICommandHandler<ChangePlanStatusCommand, VehicleId> _changePlanStatus =
        Substitute.For<ICommandHandler<ChangePlanStatusCommand, VehicleId>>();

    private readonly VehicleStatusChangedHandler _handler;

    public VehicleStatusChangedHandlerTests()
    {
        _changePlanStatus
            .HandleAsync(Arg.Any<ChangePlanStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<VehicleId>.Success(VehicleId.New())));
        _handler = new VehicleStatusChangedHandler(_changePlanStatus);
    }

    [Theory]
    [InlineData(InboundEventFactory.InactiveStatus, MaintenancePlanStatus.Suspended)]
    [InlineData(InboundEventFactory.ActiveStatus, MaintenancePlanStatus.Active)]
    public async Task HandleAsync_Should_DelegateTargetStatus_When_VehicleStatusChanges(string vehicleStatus, MaintenancePlanStatus expected)
    {
        Guid vehicleId = Guid.NewGuid();
        VehicleStatusChangedV1 integrationEvent = InboundEventFactory.VehicleStatusChanged(vehicleId, vehicleStatus);

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _changePlanStatus.Received(1).HandleAsync(
            Arg.Is<ChangePlanStatusCommand>(command => command.VehicleId.Value == vehicleId && command.TargetStatus == expected),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailureWithoutDelegating_When_VehicleIdIsEmpty()
    {
        VehicleStatusChangedV1 integrationEvent = InboundEventFactory.VehicleStatusChanged(Guid.Empty, InboundEventFactory.InactiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.IdentifierEmpty);
        await _changePlanStatus.DidNotReceive().HandleAsync(Arg.Any<ChangePlanStatusCommand>(), Arg.Any<CancellationToken>());
    }
}
