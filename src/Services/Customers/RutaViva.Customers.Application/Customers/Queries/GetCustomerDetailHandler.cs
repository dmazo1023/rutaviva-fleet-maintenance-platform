using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Customers.Queries;

public sealed class GetCustomerDetailHandler(ICustomerQueries customers)
    : IQueryHandler<GetCustomerDetailQuery, CustomerDetailDto>
{
    public async Task<Result<CustomerDetailDto>> HandleAsync(GetCustomerDetailQuery query, CancellationToken cancellationToken)
    {
        CustomerDetailDto? detail = await customers.GetDetailAsync(query.CustomerId, cancellationToken);
        if (detail is null)
        {
            return CustomerErrors.NotFound;
        }

        return detail;
    }
}
