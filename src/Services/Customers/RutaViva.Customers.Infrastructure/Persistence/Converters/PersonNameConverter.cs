using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Converters;

public sealed class PersonNameConverter() : ValueConverter<PersonName, string>(name => name.Value, value => PersonName.Create(value).Value);
