using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Infrastructure.Persistence.Converters;

public sealed class TaxIdConverter() : ValueConverter<TaxId, string>(taxId => taxId.Value, value => TaxId.Create(value).Value);
