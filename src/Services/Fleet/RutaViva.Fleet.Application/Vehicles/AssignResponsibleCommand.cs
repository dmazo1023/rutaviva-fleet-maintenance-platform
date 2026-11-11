using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed record AssignResponsibleCommand(VehicleId VehicleId, ApproverId? ResponsibleApproverId) : ICommand<VehicleId>;
