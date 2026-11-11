using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken);

    Task<Customer?> GetByTaxIdAsync(TaxId taxId, CancellationToken cancellationToken);

    void Add(Customer customer);
}
