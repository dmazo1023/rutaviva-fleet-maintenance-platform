using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed class AcknowledgeAlertHandler(IAlertRepository alerts, IUnitOfWork unitOfWork, IClock clock)
    : ICommandHandler<AcknowledgeAlertCommand, AlertId>
{
    public async Task<Result<AlertId>> HandleAsync(AcknowledgeAlertCommand command, CancellationToken cancellationToken)
    {
        Alert? alert = await alerts.GetAsync(command.AlertId, cancellationToken);
        if (alert is null)
        {
            return MaintenanceErrors.AlertNotFound;
        }

        Result acknowledged = alert.Acknowledge(command.AcknowledgedBy, clock.UtcNow);
        if (acknowledged.IsFailure)
        {
            return acknowledged.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.AlertId;
    }
}
