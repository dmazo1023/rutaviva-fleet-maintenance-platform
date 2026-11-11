using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Converters;

public sealed class RoutineTypeCodeConverter() : ValueConverter<RoutineTypeCode, string>(code => code.Value, value => RoutineTypeCode.Create(value).Value);
