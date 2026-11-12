using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Application.Bulk;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Customers.Api.Endpoints;

public sealed class BulkEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(CustomerRoutes.CustomersBulk, RegisterCustomersAsync);
        app.MapPost(CustomerRoutes.ApproversBulk, RegisterApproversAsync);
    }

    private static async Task<IResult> RegisterCustomersAsync(
        BulkRequest<CustomerBulkRow> request,
        ICommandHandler<RegisterCustomersBulkCommand, IReadOnlyList<BulkRowResult>> handler,
        CancellationToken cancellationToken,
        bool dryRun = false)
    {
        RegisterCustomersBulkCommand command = new(request.Rows ?? [], dryRun);
        Result<IReadOnlyList<BulkRowResult>> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RegisterApproversAsync(
        BulkRequest<ApproverBulkRow> request,
        ICommandHandler<RegisterApproversBulkCommand, IReadOnlyList<BulkRowResult>> handler,
        CancellationToken cancellationToken,
        bool dryRun = false)
    {
        RegisterApproversBulkCommand command = new(request.Rows ?? [], dryRun);
        Result<IReadOnlyList<BulkRowResult>> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
