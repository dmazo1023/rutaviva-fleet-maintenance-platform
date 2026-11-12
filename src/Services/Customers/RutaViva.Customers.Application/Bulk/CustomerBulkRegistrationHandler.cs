using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain.ValueObjects;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.Application.Bulk;

public sealed class CustomerBulkRegistrationHandler(
    ICommandHandler<RegisterCustomerCommand, Guid> registerHandler,
    ICustomerUniquenessChecker uniqueness)
    : BulkRegistrationHandler<CustomerBulkRow, RegisterCustomerCommand>(registerHandler)
{
    protected override int RowNumberOf(CustomerBulkRow row) => row.RowNumber;

    protected override string NaturalKey(CustomerBulkRow row) => BulkNaturalKeys.ForTaxId(row.TaxId, row.RowNumber);

    protected override Task<Result<RegisterCustomerCommand>> ResolveCommandAsync(CustomerBulkRow row, CancellationToken cancellationToken)
    {
        RegisterCustomerCommand command = new(row.Name, row.TaxId, row.Email, row.Phone);
        return Task.FromResult(Result<RegisterCustomerCommand>.Success(command));
    }

    protected override async Task<bool> ExistsAsync(string naturalKey, CancellationToken cancellationToken)
    {
        Result<TaxId> taxId = TaxId.Create(naturalKey);
        if (taxId.IsFailure)
        {
            return false;
        }

        return await uniqueness.IsTaxIdTakenAsync(taxId.Value, cancellationToken);
    }
}
