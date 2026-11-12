using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Infrastructure.Persistence.Converters;

public sealed class EmailAddressConverter() : ValueConverter<EmailAddress, string>(email => email.Value, value => EmailAddress.Create(value).Value);
