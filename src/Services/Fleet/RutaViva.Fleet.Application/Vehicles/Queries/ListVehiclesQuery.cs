using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record ListVehiclesQuery(
    CustomerId? CustomerId,
    VehicleStatus? Status,
    string? Search,
    int Page = VehicleListDefaults.FirstPage,
    int PageSize = VehicleListDefaults.DefaultPageSize) : IQuery<PagedResult<VehicleSummaryDto>>;
