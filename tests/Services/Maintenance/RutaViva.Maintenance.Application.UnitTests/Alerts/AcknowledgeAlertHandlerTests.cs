using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Maintenance.Application.UnitTests.Alerts;

public sealed class AcknowledgeAlertHandlerTests
{
    private const string AcknowledgedBy = "operator";

    private readonly IAlertRepository _alerts = Substitute.For<IAlertRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeClock _clock = new(AlertTestFactory.OccurredAt());
    private readonly AcknowledgeAlertHandler _handler;

    public AcknowledgeAlertHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _handler = new AcknowledgeAlertHandler(_alerts, _unitOfWork, _clock);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_AlertDoesNotExist()
    {
        AcknowledgeAlertCommand command = new(AlertId.New(), AcknowledgedBy);
        _alerts.GetAsync(command.AlertId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Alert?>(null));

        Result<AlertId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MaintenanceErrors.AlertNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_AcknowledgeAndSave_When_AlertIsActive()
    {
        Alert alert = AlertTestFactory.RaiseAlert(AlertTestFactory.Snapshot(), AlertType.Overdue);
        AcknowledgeAlertCommand command = new(alert.Id, AcknowledgedBy);
        _alerts.GetAsync(alert.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Alert?>(alert));

        Result<AlertId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(alert.Id);
        alert.Status.Should().Be(AlertStatus.Acknowledged);
        alert.AcknowledgedBy.Should().Be(AcknowledgedBy);
        alert.AcknowledgedAt.Should().Be(_clock.UtcNow);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnInvalidTransitionWithoutSaving_When_AlertIsResolved()
    {
        Alert alert = AlertTestFactory.RaiseAlert(AlertTestFactory.Snapshot(), AlertType.DueSoon);
        alert.Resolve(AlertResolutionReason.RoutineRestarted, _clock.UtcNow);
        AcknowledgeAlertCommand command = new(alert.Id, AcknowledgedBy);
        _alerts.GetAsync(alert.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Alert?>(alert));

        Result<AlertId> result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Error.Should().Be(MaintenanceErrors.AlertInvalidTransition);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
