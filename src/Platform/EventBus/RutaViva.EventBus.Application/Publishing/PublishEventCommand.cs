using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.Publishing;

public sealed record PublishEventCommand(string Stream, IntegrationEventEnvelope Envelope) : ICommand<long>;
