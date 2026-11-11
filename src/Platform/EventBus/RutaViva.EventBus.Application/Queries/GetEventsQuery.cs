using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.EventBus.Application.Queries;

public sealed record GetEventsQuery(IReadOnlyCollection<string> Streams, long AfterOffset, int Max) : IQuery<IReadOnlyList<EventRecord>>
{
    public const int MaxPageSize = 100;
}
