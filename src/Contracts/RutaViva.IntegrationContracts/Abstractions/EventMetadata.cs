namespace RutaViva.IntegrationContracts.Abstractions;

public sealed record EventMetadata(Guid MessageId, long Offset, string Type, string Subject, DateTimeOffset OccurredAt);
