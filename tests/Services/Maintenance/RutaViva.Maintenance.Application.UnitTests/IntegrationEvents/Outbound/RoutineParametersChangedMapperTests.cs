using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class RoutineParametersChangedMapperTests
{
    private readonly RoutineParametersChangedMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleParametersChangedEvent_When_RoutineParametersChanged()
    {
        RoutineParametersChanged domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        MaintenanceRoutineParametersChangedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<MaintenanceRoutineParametersChangedV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyFullSnapshot_When_RoutineParametersChanged()
    {
        RoutineParametersChanged domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        MaintenanceRoutineParametersChangedV1 result = (MaintenanceRoutineParametersChangedV1)_mapper.Map(domainEvent).Single();

        result.Routine.ShouldCopy(domainEvent.Snapshot);
    }

    [Fact]
    public void Map_Should_UseRoutineStream_When_RoutineParametersChanged()
    {
        RoutineParametersChanged domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceRoutine);
    }
}
