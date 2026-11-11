using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class RoutineRegisteredMapperTests
{
    private readonly RoutineRegisteredMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleRegisteredEvent_When_RoutineRegistered()
    {
        RoutineRegistered domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        MaintenanceRoutineRegisteredV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<MaintenanceRoutineRegisteredV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyFullSnapshot_When_RoutineRegistered()
    {
        RoutineRegistered domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        MaintenanceRoutineRegisteredV1 result = (MaintenanceRoutineRegisteredV1)_mapper.Map(domainEvent).Single();

        result.Routine.ShouldCopy(domainEvent.Snapshot);
    }

    [Fact]
    public void Map_Should_UseRoutineStream_When_RoutineRegistered()
    {
        RoutineRegistered domainEvent = new(OutboundEventFactory.Snapshot(), OutboundEventFactory.OccurredAt());

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceRoutine);
    }

    [Theory]
    [InlineData(20.4, 20)]
    [InlineData(20.5, 21)]
    [InlineData(20.0, 20)]
    public void Map_Should_RoundDueSoonPercent_When_PercentHasFraction(double percent, int expected)
    {
        RoutineStatusSnapshot snapshot = OutboundEventFactory.Snapshot() with { EffectiveDueSoonPercent = (decimal)percent };
        RoutineRegistered domainEvent = new(snapshot, OutboundEventFactory.OccurredAt());

        MaintenanceRoutineRegisteredV1 result = (MaintenanceRoutineRegisteredV1)_mapper.Map(domainEvent).Single();

        result.Routine.EffectiveDueSoonPercent.Should().Be(expected);
    }
}
