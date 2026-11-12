using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Converters;

public sealed class EmailAddressConverter() : ValueConverter<EmailAddress, string>(email => email.Value, value => EmailAddress.Create(value).Value);
