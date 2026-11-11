using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Converters;

public sealed class VehicleIdConverter() : ValueConverter<VehicleId, Guid>(id => id.Value, value => new VehicleId(value));
