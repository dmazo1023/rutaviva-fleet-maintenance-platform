namespace RutaViva.Maintenance.Api.Contracts;

public sealed record RegisterRoutineServiceRequest(long? ServicedKm, DateTimeOffset? ServicedAt);
