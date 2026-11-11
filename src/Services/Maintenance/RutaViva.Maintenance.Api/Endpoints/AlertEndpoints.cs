using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Api.Contracts;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Application.Alerts;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Api.Endpoints;

public sealed class AlertEndpoints : IEndpointModule
{
    private const string AlertsRoute = "/api/v1/maintenance/alerts";
    private const string AcknowledgeRoute = AlertsRoute + "/{id}/acknowledge";
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(AlertsRoute, SearchAsync);
        app.MapPost(AcknowledgeRoute, AcknowledgeAsync);
    }

    private static async Task<IResult> SearchAsync(
        IQueryHandler<SearchAlertsQuery, PagedResult<AlertDto>> handler,
        CancellationToken cancellationToken,
        AlertStatus? status = null,
        Guid? vehicleId = null,
        Guid? customerId = null,
        int page = DefaultPage,
        int pageSize = DefaultPageSize)
    {
        Result<VehicleId?> vehicle = OptionalIdentifiers.Vehicle(vehicleId);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        Result<CustomerId?> customer = OptionalIdentifiers.Customer(customerId);
        if (customer.IsFailure)
        {
            return customer.ToHttpResult();
        }

        SearchAlertsQuery query = new(status, vehicle.Value, customer.Value, page, pageSize);
        Result<PagedResult<AlertDto>> result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AcknowledgeAsync(
        Guid id,
        AcknowledgeAlertRequest request,
        ICommandHandler<AcknowledgeAlertCommand, AlertId> handler,
        CancellationToken cancellationToken)
    {
        Result<AlertId> alert = AlertId.Create(id);
        if (alert.IsFailure)
        {
            return alert.ToHttpResult();
        }

        AcknowledgeAlertCommand command = new(alert.Value, request.AcknowledgedBy ?? string.Empty);
        Result<AlertId> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContentResult();
    }
}
