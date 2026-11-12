using Microsoft.EntityFrameworkCore;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Infrastructure.Persistence.Queries;

public sealed class CustomerQueries(CustomersDbContext context) : ICustomerQueries
{
    public async Task<IReadOnlyList<CustomerSummaryDto>> ListAsync(CustomerStatus? status, CancellationToken cancellationToken) =>
        await FilterByStatus(context.Customers.AsNoTracking(), status)
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerSummaryDto(
                customer.Id.Value,
                customer.Name.Value,
                customer.TaxId.Value,
                customer.Email.Value,
                customer.Status))
            .ToListAsync(cancellationToken);

    public async Task<CustomerDetailDto?> GetDetailAsync(CustomerId id, CancellationToken cancellationToken)
    {
        CustomerReadRow? row = await ReadCustomerAsync(id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        IReadOnlyList<ApproverDto> approvers = await ReadApproversAsync(id, cancellationToken);
        return ToDetail(row, approvers);
    }

    private static IQueryable<Customer> FilterByStatus(IQueryable<Customer> customers, CustomerStatus? status)
    {
        if (status is not { } value)
        {
            return customers;
        }

        return customers.Where(customer => customer.Status == value);
    }

    private static CustomerDetailDto ToDetail(CustomerReadRow row, IReadOnlyList<ApproverDto> approvers) =>
        new(row.Id.Value, row.Name.Value, row.TaxId.Value, row.Email.Value, row.Phone?.Value, row.Status, approvers);

    private async Task<CustomerReadRow?> ReadCustomerAsync(CustomerId id, CancellationToken cancellationToken) =>
        await context.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == id)
            .Select(customer => new CustomerReadRow(
                customer.Id,
                customer.Name,
                customer.TaxId,
                customer.Email,
                customer.Phone,
                customer.Status))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<ApproverDto>> ReadApproversAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await context.Approvers
            .AsNoTracking()
            .Where(approver => approver.CustomerId == customerId)
            .OrderBy(approver => approver.FullName)
            .Select(approver => new ApproverDto(
                approver.Id.Value,
                approver.FullName.Value,
                approver.Email.Value,
                approver.Status))
            .ToListAsync(cancellationToken);
}
