using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed record AcknowledgeAlertCommand(AlertId AlertId, string AcknowledgedBy) : ICommand<AlertId>;
