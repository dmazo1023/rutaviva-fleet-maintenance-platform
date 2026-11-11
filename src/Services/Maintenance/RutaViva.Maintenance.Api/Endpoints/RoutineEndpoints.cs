using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Maintenance.Api.Contracts;
using RutaViva.Maintenance.Application.Routines;
using RutaViva.Maintenance.Application.Routines.Bulk;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Api.Endpoints;

public sealed class RoutineEndpoints : IEndpointModule
{
    private const string RoutineRoute = "/api/v1/maintenance/plans/{vehicleId}/routines/{routineId}";
    private const string RoutineStateRoute = RoutineRoute + "/state";
    private const string BulkRoute = "/api/v1/maintenance/routines/bulk";
    private const string StateRequiredCode = "Routine.StateRequired";
    private const string StateRequiredMessage = "The routine state is required.";

    private static readonly Error StateRequired = new(StateRequiredCode, StateRequiredMessage, ErrorType.Validation);

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(RoutineRoute, UpdateParametersAsync);
        app.MapPatch(RoutineStateRoute, ChangeStateAsync);
        app.MapPost(BulkRoute, RegisterBulkAsync);
    }

    private static async Task<IResult> UpdateParametersAsync(
        Guid vehicleId,
        Guid routineId,
        UpdateRoutineParametersRequest request,
        ICommandHandler<UpdateRoutineParametersCommand, RoutineId> handler,
        CancellationToken cancellationToken)
    {
        Result<VehicleId> vehicle = VehicleId.Create(vehicleId);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        Result<RoutineId> routine = RoutineId.Create(routineId);
        if (routine.IsFailure)
        {
            return routine.ToHttpResult();
        }

        UpdateRoutineParametersCommand command = new(
            vehicle.Value,
            routine.Value,
            request.IntervalKm,
            request.IntervalMonths,
            request.DueSoonThresholdPercent,
            request.UpdatedBy ?? string.Empty);
        Result<RoutineId> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ChangeStateAsync(
        Guid vehicleId,
        Guid routineId,
        ChangeRoutineStateRequest request,
        ICommandHandler<ChangeRoutineStateCommand, RoutineId> handler,
        CancellationToken cancellationToken)
    {
        if (request.State is not { } state)
        {
            return Result.Failure(StateRequired).ToHttpResult();
        }

        Result<VehicleId> vehicle = VehicleId.Create(vehicleId);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        Result<RoutineId> routine = RoutineId.Create(routineId);
        if (routine.IsFailure)
        {
            return routine.ToHttpResult();
        }

        ChangeRoutineStateCommand command = new(vehicle.Value, routine.Value, state, request.UpdatedBy ?? string.Empty);
        Result<RoutineId> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> RegisterBulkAsync(
        BulkRequest<RoutineBulkRow> request,
        ICommandHandler<RegisterRoutinesBulkCommand, IReadOnlyList<BulkRowResult>> handler,
        CancellationToken cancellationToken,
        bool dryRun = false)
    {
        RegisterRoutinesBulkCommand command = new(request.Rows ?? [], dryRun);
        Result<IReadOnlyList<BulkRowResult>> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
