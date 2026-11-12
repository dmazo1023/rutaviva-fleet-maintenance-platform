using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Fleet.Application.Bulk;

public sealed record RegisterVehiclesBulkCommand(IReadOnlyList<VehicleBulkRow> Rows, bool DryRun)
    : ICommand<IReadOnlyList<BulkRowResult>>;
