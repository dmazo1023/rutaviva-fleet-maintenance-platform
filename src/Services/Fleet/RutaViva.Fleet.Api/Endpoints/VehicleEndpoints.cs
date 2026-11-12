using RutaViva.BuildingBlocks.Api.Endpoints;
using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Api.Contracts;
using RutaViva.Fleet.Application.Vehicles;
using RutaViva.Fleet.Application.Vehicles.Queries;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Api.Endpoints;

public sealed class VehicleEndpoints : IEndpointModule
{
    private const string VehiclesRoute = "/api/v1/fleet/vehicles";
    private const string VehicleRoute = VehiclesRoute + "/{id}";
    private const string ByPlateRoute = VehiclesRoute + "/by-plate/{plate}";
    private const string StatusRoute = VehicleRoute + "/status";
    private const string ResponsibleRoute = VehicleRoute + "/responsible";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(VehiclesRoute, RegisterAsync);
        app.MapGet(VehiclesRoute, ListAsync);
        app.MapGet(VehicleRoute, GetByIdAsync);
        app.MapGet(ByPlateRoute, GetByPlateAsync);
        app.MapPatch(StatusRoute, ChangeStatusAsync);
        app.MapPut(ResponsibleRoute, AssignResponsibleAsync);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterVehicleRequest request,
        ICommandHandler<RegisterVehicleCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        Result<RegisterVehicleCommand> command = RegisterVehicleRequestMapper.ToCommand(request);
        if (command.IsFailure)
        {
            return command.ToHttpResult();
        }

        Result<Guid> result = await handler.HandleAsync(command.Value, cancellationToken);
        return result.ToCreatedResult(VehiclesRoute);
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListVehiclesQuery, PagedResult<VehicleSummaryDto>> handler,
        CancellationToken cancellationToken,
        Guid? customerId = null,
        VehicleStatus? status = null,
        string? search = null,
        int page = VehicleListDefaults.FirstPage,
        int pageSize = VehicleListDefaults.DefaultPageSize)
    {
        Result<CustomerId?> customer = OptionalIdentifiers.Customer(customerId);
        if (customer.IsFailure)
        {
            return customer.ToHttpResult();
        }

        ListVehiclesQuery query = new(customer.Value, status, search, page, pageSize);
        Result<PagedResult<VehicleSummaryDto>> result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IQueryHandler<GetVehicleDetailQuery, VehicleDetailDto> handler,
        CancellationToken cancellationToken)
    {
        Result<VehicleId> vehicle = VehicleId.Create(id);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        Result<VehicleDetailDto> result = await handler.HandleAsync(new GetVehicleDetailQuery(vehicle.Value), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetByPlateAsync(
        string plate,
        IQueryHandler<GetVehicleByPlateQuery, VehicleDetailDto> handler,
        CancellationToken cancellationToken)
    {
        Result<VehicleDetailDto> result = await handler.HandleAsync(new GetVehicleByPlateQuery(plate), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeVehicleStatusRequest request,
        ICommandHandler<ChangeVehicleStatusCommand, VehicleId> handler,
        CancellationToken cancellationToken)
    {
        if (request.Status is not { } target)
        {
            return Result.Failure(VehicleApplicationErrors.InvalidStatus).ToHttpResult();
        }

        Result<VehicleId> vehicle = VehicleId.Create(id);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        Result<VehicleId> result = await handler.HandleAsync(new ChangeVehicleStatusCommand(vehicle.Value, target), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> AssignResponsibleAsync(
        Guid id,
        AssignResponsibleRequest request,
        ICommandHandler<AssignResponsibleCommand, VehicleId> handler,
        CancellationToken cancellationToken)
    {
        Result<VehicleId> vehicle = VehicleId.Create(id);
        if (vehicle.IsFailure)
        {
            return vehicle.ToHttpResult();
        }

        Result<ApproverId?> responsible = OptionalIdentifiers.Approver(request.ResponsibleApproverId);
        if (responsible.IsFailure)
        {
            return responsible.ToHttpResult();
        }

        AssignResponsibleCommand command = new(vehicle.Value, responsible.Value);
        Result<VehicleId> result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContentResult();
    }
}
