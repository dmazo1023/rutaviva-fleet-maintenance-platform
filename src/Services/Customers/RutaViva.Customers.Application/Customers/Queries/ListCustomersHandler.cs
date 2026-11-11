using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Abstractions;

namespace RutaViva.Customers.Application.Customers.Queries;

public sealed class ListCustomersHandler(ICustomerQueries customers)
    : IQueryHandler<ListCustomersQuery, IReadOnlyList<CustomerSummaryDto>>
{
    public async Task<Result<IReadOnlyList<CustomerSummaryDto>>> HandleAsync(ListCustomersQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerSummaryDto> summaries = await customers.ListAsync(query.Status, cancellationToken);
        return Result<IReadOnlyList<CustomerSummaryDto>>.Success(summaries);
    }
}
