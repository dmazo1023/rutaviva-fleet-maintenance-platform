using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Alerts.Events;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class AlertRaisedMapperTests
{
    private const string RoutineNameValue = "Oil change";
    private const int CycleNumberValue = 2;

    private readonly AlertRaisedMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleAlertRaisedEvent_When_AlertRaised()
    {
        AlertRaised domainEvent = CreateEvent(AlertType.DueSoon, AlertTrigger.Distance);

        AlertRaisedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<AlertRaisedV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Theory]
    [InlineData(AlertType.DueSoon, AlertTrigger.Distance)]
    [InlineData(AlertType.Overdue, AlertTrigger.Time)]
    [InlineData(AlertType.Overdue, AlertTrigger.DistanceAndTime)]
    public void Map_Should_CopyAllFields_When_AlertRaised(AlertType type, AlertTrigger trigger)
    {
        AlertRaised domainEvent = CreateEvent(type, trigger);

        AlertRaisedV1 result = (AlertRaisedV1)_mapper.Map(domainEvent).Single();

        result.AlertId.Should().Be(domainEvent.AlertId.Value);
        result.VehicleId.Should().Be(domainEvent.VehicleId.Value);
        result.Plate.Should().Be(domainEvent.Plate.Value);
        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.RoutineId.Should().Be(domainEvent.RoutineId.Value);
        result.RoutineName.Should().Be(domainEvent.RoutineName);
        result.CycleNumber.Should().Be(domainEvent.CycleNumber);
        result.Type.Should().Be(type.ToString());
        result.TriggeredBy.Should().Be(trigger.ToString());
        result.Status.Should().Be(domainEvent.Status.ToString());
        result.RaisedAt.Should().Be(domainEvent.RaisedAt);
    }

    [Fact]
    public void Map_Should_UseAlertStream_When_AlertRaised()
    {
        AlertRaised domainEvent = CreateEvent(AlertType.DueSoon, AlertTrigger.Distance);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceAlert);
    }

    private static AlertRaised CreateEvent(AlertType type, AlertTrigger trigger) =>
        new(
            AlertId.New(),
            VehicleId.New(),
            OutboundEventFactory.Plate(),
            CustomerId.New(),
            RoutineId.New(),
            RoutineNameValue,
            CycleNumberValue,
            type,
            trigger,
            AlertStatus.Active,
            OutboundEventFactory.OccurredAt(),
            OutboundEventFactory.OccurredAt());
}
