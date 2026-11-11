using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Converters;

public sealed class RoutineIdConverter() : ValueConverter<RoutineId, Guid>(id => id.Value, value => new RoutineId(value));
