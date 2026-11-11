using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Plans;
using RutaViva.Maintenance.Application.Telemetry;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Api.Endpoints;

public sealed class PlanEndpoints : IEndpointModule
{
    private const string PlansRoute = "/api/v1/maintenance/plans";
    private const string PlanRoute = PlansRoute + "/{vehicleId}";
    private const string OdometerReadingsRoute = PlanRoute + "/odometer-readings";
    private const int DefaultReadingsLimit = 50;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(PlansRoute, GetSummariesAsync);
        app.MapGet(PlanRoute, GetDetailAsync);
        app.MapGet(OdometerReadingsRoute, GetOdometerReadingsAsync);
    }

    private static async Task<IResult> GetSummariesAsync(
        IQueryHandler<GetMaintenancePlanSummariesQuery, IReadOnlyList<MaintenancePlanSummaryDto>> handler,
        CancellationToken cancellationToken,
        Guid? customerId = null)
    {
        Result<CustomerId?> customer = OptionalIdentifiers.Customer(customerId);
        if (customer.IsFailure)
        {
            return customer.ToHttpResult();
        }

        GetMaintenancePlanSummariesQuery query = new(customer.Value);
        Result<IReadOnlyList<MaintenancePlanSummaryDto>> result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDetailAsync(
        Guid vehicleId,
        IQueryHandler<GetMaintenancePlanQuery, MaintenancePlanDetailDto> handler,
        CancellationToken cancellationToken)
    {
        Result<VehicleId> vehicle = VehicleId.Create(vehicleId);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        Result<MaintenancePlanDetailDto> result = await handler.HandleAsync(new GetMaintenancePlanQuery(vehicle.Value), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetOdometerReadingsAsync(
        Guid vehicleId,
        IQueryHandler<GetOdometerReadingsQuery, IReadOnlyList<OdometerReadingDto>> handler,
        CancellationToken cancellationToken,
        int limit = DefaultReadingsLimit)
    {
        Result<VehicleId> vehicle = VehicleId.Create(vehicleId);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        GetOdometerReadingsQuery query = new(vehicle.Value, limit);
        Result<IReadOnlyList<OdometerReadingDto>> result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }
}
