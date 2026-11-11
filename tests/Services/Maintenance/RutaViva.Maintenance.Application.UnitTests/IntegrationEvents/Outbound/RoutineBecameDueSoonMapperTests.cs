using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class RoutineBecameDueSoonMapperTests
{
    private readonly RoutineBecameDueSoonMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleDueSoonEvent_When_RoutineBecameDueSoon()
    {
        RoutineBecameDueSoon domainEvent = CreateEvent(AlertTrigger.Distance);

        MaintenanceRoutineDueSoonV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<MaintenanceRoutineDueSoonV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyFullSnapshot_When_RoutineBecameDueSoon()
    {
        RoutineBecameDueSoon domainEvent = CreateEvent(AlertTrigger.Distance);

        MaintenanceRoutineDueSoonV1 result = (MaintenanceRoutineDueSoonV1)_mapper.Map(domainEvent).Single();

        result.Routine.ShouldCopy(domainEvent.Snapshot);
    }

    [Theory]
    [InlineData(AlertTrigger.Distance)]
    [InlineData(AlertTrigger.Time)]
    [InlineData(AlertTrigger.DistanceAndTime)]
    public void Map_Should_CopyTrigger_When_TriggerVaries(AlertTrigger trigger)
    {
        RoutineBecameDueSoon domainEvent = CreateEvent(trigger);

        MaintenanceRoutineDueSoonV1 result = (MaintenanceRoutineDueSoonV1)_mapper.Map(domainEvent).Single();

        result.TriggeredBy.Should().Be(trigger.ToString());
    }

    [Fact]
    public void Map_Should_UseRoutineStream_When_RoutineBecameDueSoon()
    {
        RoutineBecameDueSoon domainEvent = CreateEvent(AlertTrigger.Distance);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceRoutine);
    }

    private static RoutineBecameDueSoon CreateEvent(AlertTrigger trigger) =>
        new(OutboundEventFactory.Snapshot(), trigger, true, OutboundEventFactory.OccurredAt());
}
