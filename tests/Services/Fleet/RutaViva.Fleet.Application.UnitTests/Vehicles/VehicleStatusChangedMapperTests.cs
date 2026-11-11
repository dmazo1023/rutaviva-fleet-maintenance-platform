using RutaViva.Fleet.Application.IntegrationEvents.Outbound;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Fleet;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class VehicleStatusChangedMapperTests
{
    private readonly VehicleStatusChangedMapper _mapper = new();

    [Theory]
    [InlineData(VehicleStatus.Active)]
    [InlineData(VehicleStatus.Inactive)]
    public void Map_Should_CopyAllFields_When_StatusChanged(VehicleStatus status)
    {
        VehicleStatusChanged domainEvent = CreateEvent(status);

        VehicleStatusChangedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<VehicleStatusChangedV1>().Subject;

        result.VehicleId.Should().Be(domainEvent.VehicleId.Value);
        result.Plate.Should().Be(VehicleTestFactory.PlateText);
        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.Status.Should().Be(status.ToString());
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_UseFleetVehicleStream_When_StatusChanged()
    {
        VehicleStatusChanged domainEvent = CreateEvent(VehicleStatus.Inactive);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.FleetVehicle);
    }

    private static VehicleStatusChanged CreateEvent(VehicleStatus status) =>
        new(VehicleId.New(), Plate.Create(VehicleTestFactory.PlateText).Value, CustomerId.New(), status, VehicleTestFactory.Later);
}
