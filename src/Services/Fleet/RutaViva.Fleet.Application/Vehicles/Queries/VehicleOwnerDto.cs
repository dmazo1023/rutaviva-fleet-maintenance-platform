using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record VehicleOwnerDto(CustomerId Id, string Name, string TaxId);
