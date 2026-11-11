using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.IntegrationTests.Persistence.Queries;

internal sealed record RoutineSeed(RoutineTypeId TypeId, long LastServiceKilometers, string LastServiceDate, bool IsActive);
