using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Api.Contracts;
using RutaViva.Maintenance.Application.RoutineTypes;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Api.Endpoints;

public sealed class RoutineTypeEndpoints : IEndpointModule
{
    private const string RoutineTypesRoute = "/api/v1/maintenance/routine-types";
    private const string RoutineTypeRoute = RoutineTypesRoute + "/{id}";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(RoutineTypesRoute, GetAllAsync);
        app.MapPut(RoutineTypeRoute, UpdateParametersAsync);
    }

    private static async Task<IResult> GetAllAsync(
        IQueryHandler<GetRoutineTypesQuery, IReadOnlyList<RoutineTypeDto>> handler,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RoutineTypeDto>> result = await handler.HandleAsync(new GetRoutineTypesQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateParametersAsync(
        Guid id,
        UpdateRoutineTypeParametersRequest request,
        ICommandHandler<UpdateRoutineTypeParametersCommand, RoutineTypeId> handler,
        CancellationToken cancellationToken)
    {
        Result<RoutineTypeId> routineType = RoutineTypeId.Create(id);
        if (routineType.IsFailure)
        {
            return routineType.ToHttpResult();
        }

        UpdateRoutineTypeParametersCommand command = new(
            routineType.Value,
            request.IntervalKm,
            request.IntervalMonths,
            request.DueSoonThresholdPercent,
            request.UpdatedBy ?? string.Empty);
        Result<RoutineTypeId> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContentResult();
    }
}
