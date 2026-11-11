using RutaViva.Fleet.Application.IntegrationEvents.Outbound;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Fleet;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class VehicleResponsibleAssignedMapperTests
{
    private readonly VehicleResponsibleAssignedMapper _mapper = new();

    [Fact]
    public void Map_Should_CopyAllFields_When_ResponsibleIsAssigned()
    {
        ApproverId responsibleId = ApproverId.New();
        VehicleResponsibleAssigned domainEvent = new(VehicleId.New(), responsibleId, VehicleTestFactory.Later);

        VehicleResponsibleAssignedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<VehicleResponsibleAssignedV1>().Subject;

        result.VehicleId.Should().Be(domainEvent.VehicleId.Value);
        result.ResponsibleApproverId.Should().Be(responsibleId.Value);
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_LeaveResponsibleNull_When_ResponsibleIsRemoved()
    {
        VehicleResponsibleAssigned domainEvent = new(VehicleId.New(), null, VehicleTestFactory.Later);

        VehicleResponsibleAssignedV1 result = (VehicleResponsibleAssignedV1)_mapper.Map(domainEvent).Single();

        result.ResponsibleApproverId.Should().BeNull();
    }

    [Fact]
    public void Map_Should_UseFleetVehicleStream_When_ResponsibleIsAssigned()
    {
        VehicleResponsibleAssigned domainEvent = new(VehicleId.New(), null, VehicleTestFactory.Later);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.FleetVehicle);
    }
}
