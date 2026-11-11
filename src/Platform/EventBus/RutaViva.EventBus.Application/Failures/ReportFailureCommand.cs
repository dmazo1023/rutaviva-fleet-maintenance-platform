using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.EventBus.Application.Failures;

public sealed record ReportFailureCommand(string Subscriber, long Offset, string Error) : ICommand<bool>;
