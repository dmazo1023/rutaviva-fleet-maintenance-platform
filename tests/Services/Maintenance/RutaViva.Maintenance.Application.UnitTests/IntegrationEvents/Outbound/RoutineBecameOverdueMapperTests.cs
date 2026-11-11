using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class RoutineBecameOverdueMapperTests
{
    private readonly RoutineBecameOverdueMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleOverdueEvent_When_RoutineBecameOverdue()
    {
        RoutineBecameOverdue domainEvent = CreateEvent(AlertTrigger.Distance);

        MaintenanceRoutineOverdueV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<MaintenanceRoutineOverdueV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyFullSnapshot_When_RoutineBecameOverdue()
    {
        RoutineBecameOverdue domainEvent = CreateEvent(AlertTrigger.Distance);

        MaintenanceRoutineOverdueV1 result = (MaintenanceRoutineOverdueV1)_mapper.Map(domainEvent).Single();

        result.Routine.ShouldCopy(domainEvent.Snapshot);
    }

    [Theory]
    [InlineData(AlertTrigger.Distance)]
    [InlineData(AlertTrigger.Time)]
    [InlineData(AlertTrigger.DistanceAndTime)]
    public void Map_Should_CopyTrigger_When_TriggerVaries(AlertTrigger trigger)
    {
        RoutineBecameOverdue domainEvent = CreateEvent(trigger);

        MaintenanceRoutineOverdueV1 result = (MaintenanceRoutineOverdueV1)_mapper.Map(domainEvent).Single();

        result.TriggeredBy.Should().Be(trigger.ToString());
    }

    [Fact]
    public void Map_Should_UseRoutineStream_When_RoutineBecameOverdue()
    {
        RoutineBecameOverdue domainEvent = CreateEvent(AlertTrigger.Distance);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceRoutine);
    }

    private static RoutineBecameOverdue CreateEvent(AlertTrigger trigger) =>
        new(OutboundEventFactory.Snapshot(), trigger, OutboundEventFactory.OccurredAt());
}
