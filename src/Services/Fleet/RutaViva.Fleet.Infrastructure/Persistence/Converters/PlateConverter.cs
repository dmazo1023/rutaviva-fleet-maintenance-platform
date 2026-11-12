using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Infrastructure.Persistence.Converters;

public sealed class PlateConverter() : ValueConverter<Plate, string>(value => value.Value, stored => Plate.Create(stored).Value);
