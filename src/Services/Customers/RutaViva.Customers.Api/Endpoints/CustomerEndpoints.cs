using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Api.Contracts;
using RutaViva.Customers.Application.Customers;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Api.Endpoints;

public sealed class CustomerEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(CustomerRoutes.Customers, RegisterAsync);
        app.MapGet(CustomerRoutes.Customers, ListAsync);
        app.MapGet(CustomerRoutes.Customer, GetDetailAsync);
    }

    private static string LocationOf(Guid id) => string.Format(CustomerRoutes.LocationFormat, id);

    private static async Task<IResult> RegisterAsync(
        RegisterCustomerRequest request,
        ICommandHandler<RegisterCustomerCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        RegisterCustomerCommand command = new(
            request.Name ?? string.Empty,
            request.TaxId ?? string.Empty,
            request.Email ?? string.Empty,
            request.Phone);
        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreatedResult(LocationOf);
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListCustomersQuery, IReadOnlyList<CustomerSummaryDto>> handler,
        CancellationToken cancellationToken,
        CustomerStatus? status = null)
    {
        Result<IReadOnlyList<CustomerSummaryDto>> result = await handler.HandleAsync(new ListCustomersQuery(status), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        IQueryHandler<GetCustomerDetailQuery, CustomerDetailDto> handler,
        CancellationToken cancellationToken)
    {
        Result<CustomerId> customer = CustomerId.Create(id);
        if (customer.IsFailure)
        {
            return customer.ToHttpResult();
        }

        Result<CustomerDetailDto> result = await handler.HandleAsync(new GetCustomerDetailQuery(customer.Value), cancellationToken);
        return result.ToHttpResult();
    }
}
