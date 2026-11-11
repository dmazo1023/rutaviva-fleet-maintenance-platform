using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Application.IntegrationEvents.Outbound;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Alerts.Events;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.UnitTests.IntegrationEvents.Outbound;

public sealed class AlertStatusChangedMapperTests
{
    private const string AcknowledgedByValue = "operator";

    private readonly AlertStatusChangedMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleStatusChangedEvent_When_AlertStatusChanged()
    {
        AlertStatusChanged domainEvent = CreateResolvedEvent(AlertResolutionReason.RoutineRestarted);

        AlertStatusChangedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<AlertStatusChangedV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Theory]
    [InlineData(AlertResolutionReason.RoutineRestarted)]
    [InlineData(AlertResolutionReason.EscalatedToOverdue)]
    [InlineData(AlertResolutionReason.ParametersChanged)]
    [InlineData(AlertResolutionReason.RoutineDeactivated)]
    public void Map_Should_CopyAllFields_When_AlertResolved(AlertResolutionReason reason)
    {
        AlertStatusChanged domainEvent = CreateResolvedEvent(reason);

        AlertStatusChangedV1 result = (AlertStatusChangedV1)_mapper.Map(domainEvent).Single();

        result.AlertId.Should().Be(domainEvent.AlertId.Value);
        result.VehicleId.Should().Be(domainEvent.VehicleId.Value);
        result.Plate.Should().Be(domainEvent.Plate.Value);
        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.RoutineId.Should().Be(domainEvent.RoutineId.Value);
        result.Type.Should().Be(domainEvent.Type.ToString());
        result.TriggeredBy.Should().Be(domainEvent.Trigger.ToString());
        result.Status.Should().Be(domainEvent.Status.ToString());
        result.AcknowledgedAt.Should().Be(domainEvent.AcknowledgedAt);
        result.AcknowledgedBy.Should().Be(domainEvent.AcknowledgedBy);
        result.ResolvedAt.Should().Be(domainEvent.ResolvedAt);
        result.ResolutionReason.Should().Be(reason.ToString());
    }

    [Fact]
    public void Map_Should_LeaveResolutionEmpty_When_AlertOnlyAcknowledged()
    {
        AlertStatusChanged domainEvent = CreateAcknowledgedEvent();

        AlertStatusChangedV1 result = (AlertStatusChangedV1)_mapper.Map(domainEvent).Single();

        result.ResolutionReason.Should().BeNull();
        result.ResolvedAt.Should().BeNull();
        result.AcknowledgedBy.Should().Be(AcknowledgedByValue);
    }

    [Fact]
    public void Map_Should_UseAlertStream_When_AlertStatusChanged()
    {
        AlertStatusChanged domainEvent = CreateAcknowledgedEvent();

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.MaintenanceAlert);
    }

    private static AlertStatusChanged CreateResolvedEvent(AlertResolutionReason reason) =>
        new(
            AlertId.New(),
            VehicleId.New(),
            OutboundEventFactory.Plate(),
            CustomerId.New(),
            RoutineId.New(),
            AlertType.Overdue,
            AlertTrigger.Distance,
            AlertStatus.Resolved,
            OutboundEventFactory.OccurredAt(),
            AcknowledgedByValue,
            OutboundEventFactory.OccurredAt(),
            reason,
            OutboundEventFactory.OccurredAt());

    private static AlertStatusChanged CreateAcknowledgedEvent() =>
        new(
            AlertId.New(),
            VehicleId.New(),
            OutboundEventFactory.Plate(),
            CustomerId.New(),
            RoutineId.New(),
            AlertType.DueSoon,
            AlertTrigger.Time,
            AlertStatus.Acknowledged,
            OutboundEventFactory.OccurredAt(),
            AcknowledgedByValue,
            null,
            null,
            OutboundEventFactory.OccurredAt());
}
