using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.Application.Bulk;

public sealed record RegisterCustomersBulkCommand(IReadOnlyList<CustomerBulkRow> Rows, bool DryRun)
    : ICommand<IReadOnlyList<BulkRowResult>>;
