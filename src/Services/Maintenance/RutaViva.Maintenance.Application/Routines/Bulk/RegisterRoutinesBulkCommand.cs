using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Maintenance.Application.Routines.Bulk;

public sealed record RegisterRoutinesBulkCommand(IReadOnlyList<RoutineBulkRow> Rows, bool DryRun)
    : ICommand<IReadOnlyList<BulkRowResult>>;
