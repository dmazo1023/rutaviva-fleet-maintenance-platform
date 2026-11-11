using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.EventBus.Application.Queries;

public sealed record GetLatestOffsetQuery : IQuery<long>;
