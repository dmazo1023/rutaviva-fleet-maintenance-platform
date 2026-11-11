using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Application.Abstractions;

public interface ICustomerUniquenessChecker
{
    Task<bool> IsTaxIdTakenAsync(TaxId taxId, CancellationToken cancellationToken);
}
