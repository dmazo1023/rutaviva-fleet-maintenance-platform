using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Infrastructure.Persistence.Converters;

public sealed class VehicleIdConverter() : ValueConverter<VehicleId, Guid>(id => id.Value, value => new VehicleId(value));
