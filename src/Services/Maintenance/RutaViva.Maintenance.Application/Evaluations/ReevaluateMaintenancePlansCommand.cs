using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Evaluations;

public sealed record ReevaluateMaintenancePlansCommand(DateOnly AsOfDate, VehicleId? VehicleId)
    : ICommand<ReevaluationSummary>;
