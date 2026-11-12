using Microsoft.EntityFrameworkCore;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomerUniquenessChecker(CustomersDbContext context) : ICustomerUniquenessChecker
{
    public async Task<bool> IsTaxIdTakenAsync(TaxId taxId, CancellationToken cancellationToken) =>
        await context.Customers
            .AsNoTracking()
            .AnyAsync(customer => customer.TaxId == taxId, cancellationToken);
}
