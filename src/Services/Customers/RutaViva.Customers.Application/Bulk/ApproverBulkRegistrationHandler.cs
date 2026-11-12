using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Approvers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.Application.Bulk;

public sealed class ApproverBulkRegistrationHandler(
    ICommandHandler<AddApproverCommand, Guid> addHandler,
    ICustomerRepository customers)
    : BulkRegistrationHandler<ApproverBulkRow, AddApproverCommand>(addHandler)
{
    protected override int RowNumberOf(ApproverBulkRow row) => row.RowNumber;

    protected override string NaturalKey(ApproverBulkRow row) =>
        BulkNaturalKeys.ForApprover(row.CustomerTaxId, row.Email, row.RowNumber);

    protected override async Task<Result<AddApproverCommand>> ResolveCommandAsync(ApproverBulkRow row, CancellationToken cancellationToken)
    {
        Result<TaxId> taxId = TaxId.Create(row.CustomerTaxId);
        if (taxId.IsFailure)
        {
            return taxId.Error;
        }

        Customer? customer = await customers.GetByTaxIdAsync(taxId.Value, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        return new AddApproverCommand(customer.Id, row.FullName, row.Email);
    }

    protected override async Task<bool> ExistsAsync(string naturalKey, CancellationToken cancellationToken)
    {
        if (!BulkNaturalKeys.TryParseApprover(naturalKey, out string taxIdText, out string email))
        {
            return false;
        }

        Result<TaxId> taxId = TaxId.Create(taxIdText);
        if (taxId.IsFailure)
        {
            return false;
        }

        Customer? customer = await customers.GetByTaxIdAsync(taxId.Value, cancellationToken);
        return customer is not null && HasApprover(customer, email);
    }

    private static bool HasApprover(Customer customer, string email) =>
        customer.Approvers.Any(approver => approver.Email.Value == email);
}
