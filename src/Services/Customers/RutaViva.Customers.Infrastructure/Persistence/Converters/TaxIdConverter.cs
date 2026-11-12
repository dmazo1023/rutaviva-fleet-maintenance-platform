using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Converters;

public sealed class TaxIdConverter() : ValueConverter<TaxId, string>(taxId => taxId.Value, value => TaxId.Create(value).Value);
