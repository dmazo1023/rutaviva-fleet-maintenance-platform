using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Infrastructure.Persistence.Converters;

public sealed class KilometersConverter() : ValueConverter<Kilometers, long>(value => value.Value, stored => Kilometers.Create(stored).Value);
