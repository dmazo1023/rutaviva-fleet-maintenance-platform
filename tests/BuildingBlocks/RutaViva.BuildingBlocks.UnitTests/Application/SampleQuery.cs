using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed record SampleQuery(string Name) : IQuery<string>;
