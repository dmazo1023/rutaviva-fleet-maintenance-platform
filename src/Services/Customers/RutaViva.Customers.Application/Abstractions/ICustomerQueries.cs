using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Abstractions;

public interface ICustomerQueries
{
    Task<IReadOnlyList<CustomerSummaryDto>> ListAsync(CustomerStatus? status, CancellationToken cancellationToken);

    Task<CustomerDetailDto?> GetDetailAsync(CustomerId id, CancellationToken cancellationToken);
}
