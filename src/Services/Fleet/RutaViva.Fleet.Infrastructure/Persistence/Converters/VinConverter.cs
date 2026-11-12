using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Infrastructure.Persistence.Converters;

public sealed class VinConverter() : ValueConverter<Vin, string>(value => value.Value, stored => Vin.Create(stored).Value);
