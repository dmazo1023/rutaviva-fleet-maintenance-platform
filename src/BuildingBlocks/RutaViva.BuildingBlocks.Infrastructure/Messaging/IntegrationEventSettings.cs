namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed record IntegrationEventSettings
{
    public required string ServiceName { get; init; }
}
