using Microsoft.AspNetCore.SignalR;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application;
using RutaViva.EventBus.Application.Failures;
using RutaViva.EventBus.Application.Publishing;
using RutaViva.EventBus.Application.Queries;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;
using SignalRHub = Microsoft.AspNetCore.SignalR.Hub;

namespace RutaViva.EventBus.Hub.Hubs;

public sealed class EventsHub(
    ICommandHandler<PublishEventCommand, long> publishHandler,
    IQueryHandler<GetEventsQuery, IReadOnlyList<EventRecord>> getEventsHandler,
    IQueryHandler<GetLatestOffsetQuery, long> latestOffsetHandler,
    ICommandHandler<ReportFailureCommand, bool> reportFailureHandler) : SignalRHub
{
    public const string GroupPrefix = "stream:";

    private const string FailureFormat = "{0}: {1}";

    public async Task<long> Publish(string stream, IntegrationEventEnvelope envelope)
    {
        Result<long> result = await publishHandler.HandleAsync(new PublishEventCommand(stream, envelope), Context.ConnectionAborted);
        long offset = Unwrap(result);
        await NotifyAvailableAsync(stream, offset);
        return offset;
    }

    public async Task Subscribe(string[] streams)
    {
        EnsureStreams(streams);
        foreach (string stream in streams)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(stream), Context.ConnectionAborted);
        }
    }

    public async Task<IReadOnlyList<EventRecord>> GetEvents(string[] streams, long afterOffset, int max)
    {
        Result<IReadOnlyList<EventRecord>> result = await getEventsHandler.HandleAsync(new GetEventsQuery(streams, afterOffset, max), Context.ConnectionAborted);
        return Unwrap(result);
    }

    public async Task<long> GetLatestOffset()
    {
        Result<long> result = await latestOffsetHandler.HandleAsync(new GetLatestOffsetQuery(), Context.ConnectionAborted);
        return Unwrap(result);
    }

    public async Task ReportFailure(string subscriber, long offset, string error)
    {
        Result<bool> result = await reportFailureHandler.HandleAsync(new ReportFailureCommand(subscriber, offset, error), Context.ConnectionAborted);
        EnsureSuccess(result);
    }

    private static string GroupName(string stream) => GroupPrefix + stream;

    private static HubException ToHubException(Error error) =>
        new(string.Format(FailureFormat, error.Code, error.Message));

    private static void EnsureSuccess(Result result)
    {
        if (result.IsFailure)
        {
            throw ToHubException(result.Error);
        }
    }

    private static T Unwrap<T>(Result<T> result)
    {
        EnsureSuccess(result);
        return result.Value;
    }

    private static void EnsureStreams(string[]? streams)
    {
        if (streams is null || streams.Length == 0)
        {
            throw ToHubException(EventBusErrors.StreamsMissing);
        }

        if (streams.Any(string.IsNullOrWhiteSpace))
        {
            throw ToHubException(EventBusErrors.StreamMissing);
        }
    }

    private Task NotifyAvailableAsync(string stream, long offset) =>
        Clients.Group(GroupName(stream)).SendAsync(EventBusHubMethods.EventsAvailable, stream, offset, CancellationToken.None);
}
