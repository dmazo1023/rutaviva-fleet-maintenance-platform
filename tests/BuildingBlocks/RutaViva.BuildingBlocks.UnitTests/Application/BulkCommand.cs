using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed record BulkCommand(string Code) : ICommand<Guid>;
