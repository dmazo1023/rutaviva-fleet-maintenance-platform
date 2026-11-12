using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record VehicleListCriteria(
    CustomerId? CustomerId,
    VehicleStatus? Status,
    string? Search,
    int Page,
    int PageSize);
