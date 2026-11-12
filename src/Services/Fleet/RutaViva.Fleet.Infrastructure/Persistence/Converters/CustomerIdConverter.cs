using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Infrastructure.Persistence.Converters;

public sealed class CustomerIdConverter() : ValueConverter<CustomerId, Guid>(id => id.Value, value => new CustomerId(value));
