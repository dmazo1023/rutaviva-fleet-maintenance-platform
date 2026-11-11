using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Api.Contracts;

public sealed record ChangeRoutineStateRequest(RoutineState? State, string? UpdatedBy);
