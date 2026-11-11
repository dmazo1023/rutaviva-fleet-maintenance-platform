using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class RoutineRestartedMapperTests
{
    private readonly RoutineRestartedMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleRestartedEvent_When_RoutineRestarted()
    {
        RoutineRestarted domainEvent = CreateEvent();

        MaintenanceRoutineRestartedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<MaintenanceRoutineRestartedV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyFullSnapshot_When_RoutineRestarted()
    {
        RoutineRestarted domainEvent = CreateEvent();

        MaintenanceRoutineRestartedV1 result = (MaintenanceRoutineRestartedV1)_mapper.Map(domainEvent).Single();

        result.Routine.ShouldCopy(domainEvent.Snapshot);
    }

    [Fact]
    public void Map_Should_CopyServiceData_When_RoutineRestarted()
    {
        RoutineRestarted domainEvent = CreateEvent();

        MaintenanceRoutineRestartedV1 result = (MaintenanceRoutineRestartedV1)_mapper.Map(domainEvent).Single();

        result.ServiceOrderId.Should().Be(domainEvent.ServiceOrderId);
        result.ServicedKm.Should().Be(domainEvent.Service.Kilometers);
        DateOnly.FromDateTime(result.ServicedAt.UtcDateTime).Should().Be(domainEvent.Service.Date);
    }

    [Fact]
    public void Map_Should_UseRoutineStream_When_RoutineRestarted()
    {
        RoutineRestarted domainEvent = CreateEvent();

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceRoutine);
    }

    private static RoutineRestarted CreateEvent() =>
        new(OutboundEventFactory.Snapshot(), Guid.NewGuid(), OutboundEventFactory.Service(), OutboundEventFactory.OccurredAt());
}
