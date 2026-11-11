namespace RutaViva.IntegrationContracts.EventBus;

public sealed record EventRecord(long Offset, string Stream, IntegrationEventEnvelope Envelope);
