using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class MaintenancePlanStatusChangedMapperTests
{
    private readonly MaintenancePlanStatusChangedMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSinglePlanStatusEvent_When_PlanStatusChanged()
    {
        MaintenancePlanStatusChanged domainEvent = CreateEvent(MaintenancePlanStatus.Suspended);

        MaintenancePlanStatusChangedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<MaintenancePlanStatusChangedV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Theory]
    [InlineData(MaintenancePlanStatus.Active)]
    [InlineData(MaintenancePlanStatus.Suspended)]
    public void Map_Should_CopyAllFields_When_PlanStatusChanged(MaintenancePlanStatus status)
    {
        MaintenancePlanStatusChanged domainEvent = CreateEvent(status);

        MaintenancePlanStatusChangedV1 result = (MaintenancePlanStatusChangedV1)_mapper.Map(domainEvent).Single();

        result.VehicleId.Should().Be(domainEvent.VehicleId.Value);
        result.Plate.Should().Be(domainEvent.Plate.Value);
        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.Status.Should().Be(status.ToString());
    }

    [Fact]
    public void Map_Should_UseRoutineStream_When_PlanStatusChanged()
    {
        MaintenancePlanStatusChanged domainEvent = CreateEvent(MaintenancePlanStatus.Active);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceRoutine);
    }

    private static MaintenancePlanStatusChanged CreateEvent(MaintenancePlanStatus status) =>
        new(VehicleId.New(), OutboundEventFactory.Plate(), CustomerId.New(), status, OutboundEventFactory.OccurredAt());
}
