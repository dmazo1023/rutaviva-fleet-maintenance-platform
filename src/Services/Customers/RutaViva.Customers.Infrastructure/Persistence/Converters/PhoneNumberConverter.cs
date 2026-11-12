using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Converters;

public sealed class PhoneNumberConverter() : ValueConverter<PhoneNumber, string>(phone => phone.Value, value => PhoneNumber.Create(value).Value);
