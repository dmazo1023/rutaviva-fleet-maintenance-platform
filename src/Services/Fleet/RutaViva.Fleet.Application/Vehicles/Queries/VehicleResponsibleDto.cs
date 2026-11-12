using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record VehicleResponsibleDto(ApproverId Id, string Name, string Email);
