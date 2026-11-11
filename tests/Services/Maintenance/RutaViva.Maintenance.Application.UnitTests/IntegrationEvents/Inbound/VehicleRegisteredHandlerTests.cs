using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.Maintenance.Application.IntegrationEvents.Inbound;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Inbound;

public sealed class VehicleRegisteredHandlerTests
{
    private readonly ICommandHandler<CreateMaintenancePlanCommand, VehicleId> _createPlan =
        Substitute.For<ICommandHandler<CreateMaintenancePlanCommand, VehicleId>>();

    private readonly VehicleRegisteredHandler _handler;

    public VehicleRegisteredHandlerTests() => _handler = new VehicleRegisteredHandler(_createPlan);

    [Theory]
    [InlineData(InboundEventFactory.ActiveStatus, MaintenancePlanStatus.Active)]
    [InlineData(InboundEventFactory.InactiveStatus, MaintenancePlanStatus.Suspended)]
    public async Task HandleAsync_Should_DelegateCreatePlanCommand_When_VehicleIsRegistered(string vehicleStatus, MaintenancePlanStatus expected)
    {
        VehicleRegisteredV1 integrationEvent = InboundEventFactory.VehicleRegistered(Guid.NewGuid(), vehicleStatus);
        StubCreatePlan(Result<VehicleId>.Success(VehicleId.New()));

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _createPlan.Received(1).HandleAsync(
            Arg.Is<CreateMaintenancePlanCommand>(command => IsCommandOf(command, integrationEvent, expected)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailureWithoutDelegating_When_VehicleIdIsEmpty()
    {
        VehicleRegisteredV1 integrationEvent = InboundEventFactory.VehicleRegistered(Guid.Empty, InboundEventFactory.ActiveStatus);

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.IdentifierEmpty);
        await _createPlan.DidNotReceive().HandleAsync(Arg.Any<CreateMaintenancePlanCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateFailure_When_CommandFails()
    {
        VehicleRegisteredV1 integrationEvent = InboundEventFactory.VehicleRegistered(Guid.NewGuid(), InboundEventFactory.ActiveStatus);
        StubCreatePlan(Result<VehicleId>.Failure(MaintenanceErrors.LicensePlateInvalid));

        Result result = await _handler.HandleAsync(integrationEvent, InboundEventFactory.Metadata(), TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.LicensePlateInvalid);
    }

    private static bool IsCommandOf(CreateMaintenancePlanCommand command, VehicleRegisteredV1 integrationEvent, MaintenancePlanStatus status) =>
        command.VehicleId.Value == integrationEvent.VehicleId
        && command.Plate == integrationEvent.Plate
        && command.CustomerId.Value == integrationEvent.CustomerId
        && command.Status == status
        && command.OdometerKm == integrationEvent.InitialOdometerKm
        && command.RegisteredAt == integrationEvent.RegisteredAt;

    private void StubCreatePlan(Result<VehicleId> result) =>
        _createPlan.HandleAsync(Arg.Any<CreateMaintenancePlanCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
}
