using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class RoutineDueStatusRevertedMapperTests
{
    private const string ExpectedReason = "ParametersChanged";

    private readonly RoutineDueStatusRevertedMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleRevertedEvent_When_RoutineDueStatusReverted()
    {
        RoutineDueStatusReverted domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        MaintenanceRoutineDueStatusRevertedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<MaintenanceRoutineDueStatusRevertedV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyFullSnapshot_When_RoutineDueStatusReverted()
    {
        RoutineDueStatusReverted domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        MaintenanceRoutineDueStatusRevertedV1 result = (MaintenanceRoutineDueStatusRevertedV1)_mapper.Map(domainEvent).Single();

        result.Routine.ShouldCopy(domainEvent.Snapshot);
    }

    [Fact]
    public void Map_Should_UseParametersChangedReason_When_RoutineDueStatusReverted()
    {
        RoutineDueStatusReverted domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        MaintenanceRoutineDueStatusRevertedV1 result = (MaintenanceRoutineDueStatusRevertedV1)_mapper.Map(domainEvent).Single();

        result.Reason.Should().Be(ExpectedReason);
    }

    [Fact]
    public void Map_Should_UseRoutineStream_When_RoutineDueStatusReverted()
    {
        RoutineDueStatusReverted domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceRoutine);
    }
}
