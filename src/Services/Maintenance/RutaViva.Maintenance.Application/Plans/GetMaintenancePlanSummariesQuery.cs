using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record GetMaintenancePlanSummariesQuery(CustomerId? CustomerId)
    : IQuery<IReadOnlyList<MaintenancePlanSummaryDto>>;
