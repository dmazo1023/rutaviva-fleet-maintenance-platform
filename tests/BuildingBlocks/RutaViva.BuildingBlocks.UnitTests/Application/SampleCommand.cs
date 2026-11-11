using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed record SampleCommand(string Name) : ICommand<Guid>;
