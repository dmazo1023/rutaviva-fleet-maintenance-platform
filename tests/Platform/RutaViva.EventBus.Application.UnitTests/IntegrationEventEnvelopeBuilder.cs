using System.Text.Json;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.UnitTests;

internal sealed class IntegrationEventEnvelopeBuilder
{
    private const string DefaultSource = "rutaviva/fleet";
    private const string DefaultType = "rutaviva.fleet.VehicleRegistered.v1";
    private const string DefaultSubject = "vehicle/1";
    private const string EmptyJson = "{}";

    private Guid _id = Guid.NewGuid();
    private string _type = DefaultType;
    private string _subject = DefaultSubject;
    private string _source = DefaultSource;

    public IntegrationEventEnvelopeBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public IntegrationEventEnvelopeBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public IntegrationEventEnvelopeBuilder WithSource(string source)
    {
        _source = source;
        return this;
    }

    public IntegrationEventEnvelopeBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public IntegrationEventEnvelope Build() => new()
    {
        Id = _id,
        Source = _source,
        Type = _type,
        Subject = _subject,
        Time = DateTimeOffset.UtcNow,
        Data = JsonDocument.Parse(EmptyJson).RootElement.Clone(),
    };
}
