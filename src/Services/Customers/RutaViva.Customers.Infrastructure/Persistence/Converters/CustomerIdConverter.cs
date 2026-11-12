using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Infrastructure.Persistence.Converters;

public sealed class CustomerIdConverter() : ValueConverter<CustomerId, Guid>(id => id.Value, value => new CustomerId(value));
