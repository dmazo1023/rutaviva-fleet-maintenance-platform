using System.Globalization;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Alerts.Events;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.UnitTests.Alerts;

public sealed class AlertTests
{
    private const string PlateText = "RVA101";
    private const string RoutineName = "Oil change";
    private const string Operator = "operator@rutaviva.com";
    private const string RaisedAtText = "2026-11-01T08:00:00-05:00";
    private const string AcknowledgedAtText = "2026-11-02T09:30:00-05:00";
    private const string ResolvedAtText = "2026-11-03T10:45:00-05:00";
    private const string LaterResolvedAtText = "2026-11-04T11:00:00-05:00";
    private const int CycleNumber = 3;

    private static readonly DateTimeOffset RaisedAt = Parse(RaisedAtText);
    private static readonly DateTimeOffset AcknowledgedAt = Parse(AcknowledgedAtText);
    private static readonly DateTimeOffset ResolvedAt = Parse(ResolvedAtText);
    private static readonly DateTimeOffset LaterResolvedAt = Parse(LaterResolvedAtText);

    [Fact]
    public void Raise_Should_CreateActiveAlertAndEmitAlertRaised_When_OriginIsValid()
    {
        AlertOrigin origin = CreateOrigin();

        Alert alert = Alert.Raise(origin, AlertType.Overdue, AlertTrigger.DistanceAndTime, RaisedAt);

        alert.Status.Should().Be(AlertStatus.Active);
        alert.RaisedAt.Should().Be(RaisedAt);
        alert.AcknowledgedAt.Should().BeNull();
        alert.AcknowledgedBy.Should().BeNull();
        alert.ResolvedAt.Should().BeNull();
        alert.ResolutionReason.Should().BeNull();
        alert.DomainEvents.Should().ContainSingle();
        AlertRaised raised = alert.DomainEvents.OfType<AlertRaised>().Single();
        raised.AlertId.Should().Be(alert.Id);
        raised.VehicleId.Should().Be(origin.VehicleId);
        raised.Plate.Should().Be(origin.Plate);
        raised.CustomerId.Should().Be(origin.CustomerId);
        raised.RoutineId.Should().Be(origin.RoutineId);
        raised.RoutineName.Should().Be(RoutineName);
        raised.CycleNumber.Should().Be(CycleNumber);
        raised.Type.Should().Be(AlertType.Overdue);
        raised.Trigger.Should().Be(AlertTrigger.DistanceAndTime);
        raised.Status.Should().Be(AlertStatus.Active);
        raised.RaisedAt.Should().Be(RaisedAt);
        raised.OccurredAt.Should().Be(RaisedAt);
    }

    [Theory]
    [InlineData(AlertType.DueSoon, AlertTrigger.Distance)]
    [InlineData(AlertType.DueSoon, AlertTrigger.Time)]
    [InlineData(AlertType.Overdue, AlertTrigger.DistanceAndTime)]
    public void Raise_Should_KeepTypeAndTrigger_When_AlertIsCreated(AlertType type, AlertTrigger trigger)
    {
        Alert alert = Alert.Raise(CreateOrigin(), type, trigger, RaisedAt);

        alert.Type.Should().Be(type);
        alert.Trigger.Should().Be(trigger);
    }

    [Fact]
    public void Acknowledge_Should_MoveToAcknowledgedAndEmitEvent_When_AlertIsActive()
    {
        Alert alert = CreateActiveAlert();

        Result result = alert.Acknowledge(Operator, AcknowledgedAt);

        result.IsSuccess.Should().BeTrue();
        alert.Status.Should().Be(AlertStatus.Acknowledged);
        alert.AcknowledgedAt.Should().Be(AcknowledgedAt);
        alert.AcknowledgedBy.Should().Be(Operator);
        AlertStatusChanged changed = LastStatusChanged(alert);
        changed.AlertId.Should().Be(alert.Id);
        changed.VehicleId.Should().Be(alert.VehicleId);
        changed.Plate.Should().Be(alert.Plate);
        changed.CustomerId.Should().Be(alert.CustomerId);
        changed.RoutineId.Should().Be(alert.RoutineId);
        changed.Type.Should().Be(alert.Type);
        changed.Trigger.Should().Be(alert.Trigger);
        changed.Status.Should().Be(AlertStatus.Acknowledged);
        changed.AcknowledgedAt.Should().Be(AcknowledgedAt);
        changed.AcknowledgedBy.Should().Be(Operator);
        changed.ResolvedAt.Should().BeNull();
        changed.ResolutionReason.Should().BeNull();
        changed.OccurredAt.Should().Be(AcknowledgedAt);
    }

    [Theory]
    [InlineData(AlertResolutionReason.RoutineRestarted)]
    [InlineData(AlertResolutionReason.EscalatedToOverdue)]
    [InlineData(AlertResolutionReason.ParametersChanged)]
    [InlineData(AlertResolutionReason.RoutineDeactivated)]
    public void Resolve_Should_MoveToResolvedAndEmitEvent_When_AlertIsActive(AlertResolutionReason reason)
    {
        Alert alert = CreateActiveAlert();

        Result result = alert.Resolve(reason, ResolvedAt);

        result.IsSuccess.Should().BeTrue();
        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.ResolvedAt.Should().Be(ResolvedAt);
        alert.ResolutionReason.Should().Be(reason);
        AlertStatusChanged changed = LastStatusChanged(alert);
        changed.Status.Should().Be(AlertStatus.Resolved);
        changed.AcknowledgedAt.Should().BeNull();
        changed.AcknowledgedBy.Should().BeNull();
        changed.ResolvedAt.Should().Be(ResolvedAt);
        changed.ResolutionReason.Should().Be(reason);
        changed.OccurredAt.Should().Be(ResolvedAt);
    }

    [Fact]
    public void Resolve_Should_MoveToResolvedKeepingAcknowledgement_When_AlertIsAcknowledged()
    {
        Alert alert = CreateActiveAlert();
        alert.Acknowledge(Operator, AcknowledgedAt);

        Result result = alert.Resolve(AlertResolutionReason.RoutineRestarted, ResolvedAt);

        result.IsSuccess.Should().BeTrue();
        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.AcknowledgedAt.Should().Be(AcknowledgedAt);
        alert.AcknowledgedBy.Should().Be(Operator);
        AlertStatusChanged changed = LastStatusChanged(alert);
        changed.Status.Should().Be(AlertStatus.Resolved);
        changed.AcknowledgedAt.Should().Be(AcknowledgedAt);
        changed.AcknowledgedBy.Should().Be(Operator);
        changed.ResolvedAt.Should().Be(ResolvedAt);
        changed.ResolutionReason.Should().Be(AlertResolutionReason.RoutineRestarted);
    }

    [Fact]
    public void Resolve_Should_DoNothingAndEmitNothing_When_AlertIsAlreadyResolved()
    {
        Alert alert = CreateActiveAlert();
        alert.Resolve(AlertResolutionReason.EscalatedToOverdue, ResolvedAt);
        int eventsBefore = alert.DomainEvents.Count;

        Result result = alert.Resolve(AlertResolutionReason.RoutineDeactivated, LaterResolvedAt);

        result.IsSuccess.Should().BeTrue();
        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.ResolvedAt.Should().Be(ResolvedAt);
        alert.ResolutionReason.Should().Be(AlertResolutionReason.EscalatedToOverdue);
        alert.DomainEvents.Should().HaveCount(eventsBefore);
    }

    [Fact]
    public void Acknowledge_Should_ReturnInvalidTransitionAndKeepState_When_AlertIsResolved()
    {
        Alert alert = CreateActiveAlert();
        alert.Resolve(AlertResolutionReason.RoutineRestarted, ResolvedAt);
        int eventsBefore = alert.DomainEvents.Count;

        Result result = alert.Acknowledge(Operator, AcknowledgedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.AlertInvalidTransition);
        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.AcknowledgedAt.Should().BeNull();
        alert.AcknowledgedBy.Should().BeNull();
        alert.DomainEvents.Should().HaveCount(eventsBefore);
    }

    [Fact]
    public void Acknowledge_Should_ReturnInvalidTransition_When_AlertIsAlreadyAcknowledged()
    {
        Alert alert = CreateActiveAlert();
        alert.Acknowledge(Operator, AcknowledgedAt);
        int eventsBefore = alert.DomainEvents.Count;

        Result result = alert.Acknowledge(Operator, ResolvedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.AlertInvalidTransition);
        alert.AcknowledgedAt.Should().Be(AcknowledgedAt);
        alert.DomainEvents.Should().HaveCount(eventsBefore);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Acknowledge_Should_ReturnAcknowledgedByRequired_When_UserIsBlank(string acknowledgedBy)
    {
        Alert alert = CreateActiveAlert();

        Result result = alert.Acknowledge(acknowledgedBy, AcknowledgedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.AlertAcknowledgedByRequired);
        alert.Status.Should().Be(AlertStatus.Active);
        alert.DomainEvents.Should().ContainSingle();
    }

    private static DateTimeOffset Parse(string text) => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);

    private static AlertOrigin CreateOrigin() => new(
        VehicleId.New(),
        LicensePlate.Create(PlateText).Value,
        CustomerId.New(),
        RoutineId.New(),
        RoutineName,
        CycleNumber);

    private static Alert CreateActiveAlert() => Alert.Raise(CreateOrigin(), AlertType.DueSoon, AlertTrigger.Distance, RaisedAt);

    private static AlertStatusChanged LastStatusChanged(Alert alert) =>
        alert.DomainEvents.OfType<AlertStatusChanged>().Last();
}
