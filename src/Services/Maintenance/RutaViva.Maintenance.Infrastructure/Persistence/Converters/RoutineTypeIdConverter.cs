using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Converters;

public sealed class RoutineTypeIdConverter() : ValueConverter<RoutineTypeId, Guid>(id => id.Value, value => new RoutineTypeId(value));
