using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Converters;

public sealed class LicensePlateConverter() : ValueConverter<LicensePlate, string>(plate => plate.Value, value => LicensePlate.Create(value).Value);
