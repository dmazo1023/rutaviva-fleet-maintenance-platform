using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.Bulk;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.Fleet.Api.Endpoints;

public sealed class VehicleBulkEndpoints : IEndpointModule
{
    private const string BulkRoute = "/api/v1/fleet/vehicles/bulk";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(BulkRoute, RegisterBulkAsync);
    }

    private static async Task<IResult> RegisterBulkAsync(
        BulkRequest<VehicleBulkRow> request,
        ICommandHandler<RegisterVehiclesBulkCommand, IReadOnlyList<BulkRowResult>> handler,
        CancellationToken cancellationToken,
        bool dryRun = false)
    {
        RegisterVehiclesBulkCommand command = new(request.Rows ?? [], dryRun);
        Result<IReadOnlyList<BulkRowResult>> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
