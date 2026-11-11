namespace RutaViva.BuildingBlocks.Domain.Results;

public sealed record Error(string Code, string Message, ErrorType Type);
