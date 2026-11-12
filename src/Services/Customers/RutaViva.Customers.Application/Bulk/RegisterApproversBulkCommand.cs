using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.Application.Bulk;

public sealed record RegisterApproversBulkCommand(IReadOnlyList<ApproverBulkRow> Rows, bool DryRun)
    : ICommand<IReadOnlyList<BulkRowResult>>;
