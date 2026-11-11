using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Api.Contracts;
using RutaViva.Maintenance.Application.Evaluations;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Api.Endpoints;

public sealed class TimeEvaluationEndpoints : IEndpointModule
{
    private const string TimeEvaluationsRoute = "/api/v1/maintenance/time-evaluations";
    private const string ReevaluationsRoute = "/api/v1/maintenance/reevaluations";

    private static readonly ReevaluationRequest EmptyReevaluation = new(null, null);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(TimeEvaluationsRoute, EvaluateAsync);
        app.MapPost(ReevaluationsRoute, ReevaluateAsync);
    }

    private static async Task<IResult> EvaluateAsync(
        TimeEvaluationRequest request,
        ICommandHandler<ReevaluateMaintenancePlansCommand, ReevaluationSummary> handler,
        CancellationToken cancellationToken)
    {
        ReevaluateMaintenancePlansCommand command = new(request.AsOfDate ?? default(DateOnly), null);
        Result<ReevaluationSummary> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReevaluateAsync(
        ReevaluationRequest? request,
        ICommandHandler<ReevaluateMaintenancePlansCommand, ReevaluationSummary> handler,
        IClock clock,
        CancellationToken cancellationToken)
    {
        ReevaluationRequest effective = request ?? EmptyReevaluation;
        Result<VehicleId?> vehicle = OptionalIdentifiers.Vehicle(effective.VehicleId);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        DateOnly asOfDate = effective.AsOfDate ?? clock.TodayIn(MaintenanceTimeZone.Bogota);
        ReevaluateMaintenancePlansCommand command = new(asOfDate, vehicle.Value);
        Result<ReevaluationSummary> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
