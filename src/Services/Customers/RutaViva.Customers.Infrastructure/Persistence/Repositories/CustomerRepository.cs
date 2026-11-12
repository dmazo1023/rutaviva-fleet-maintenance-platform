using Microsoft.EntityFrameworkCore;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(CustomersDbContext context) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        await context.Customers
            .Include(customer => customer.Approvers)
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public async Task<Customer?> GetByTaxIdAsync(TaxId taxId, CancellationToken cancellationToken) =>
        await context.Customers
            .Include(customer => customer.Approvers)
            .FirstOrDefaultAsync(customer => customer.TaxId == taxId, cancellationToken);

    public void Add(Customer customer) => context.Customers.Add(customer);
}
