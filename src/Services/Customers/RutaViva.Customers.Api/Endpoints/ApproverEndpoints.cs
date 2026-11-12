using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Api.Contracts;
using RutaViva.Customers.Application.Approvers;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Api.Endpoints;

public sealed class ApproverEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(CustomerRoutes.Approvers, AddAsync);
    }

    private static async Task<IResult> AddAsync(
        Guid id,
        AddApproverRequest request,
        ICommandHandler<AddApproverCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        Result<CustomerId> customer = CustomerId.Create(id);
        if (customer.IsFailure)
        {
            return customer.ToHttpResult();
        }

        AddApproverCommand command = new(customer.Value, request.FullName ?? string.Empty, request.Email ?? string.Empty);
        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreatedResult(_ => string.Format(CustomerRoutes.LocationFormat, id));
    }
}
