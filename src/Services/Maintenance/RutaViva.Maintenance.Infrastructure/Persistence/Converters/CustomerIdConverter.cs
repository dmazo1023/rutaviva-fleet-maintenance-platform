using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Converters;

public sealed class CustomerIdConverter() : ValueConverter<CustomerId, Guid>(id => id.Value, value => new CustomerId(value));
