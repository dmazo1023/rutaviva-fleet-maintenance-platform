using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.Workshops.Application.IntegrationEvents.Outbound;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.Events;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.UnitTests.IntegrationEvents;

public sealed class WorkshopRegisteredMapperTests
{
    private readonly WorkshopRegisteredMapper _mapper = new();

    [Fact]
    public void Map_Should_ReturnSingleWorkshopRegisteredEvent_When_WorkshopRegistered()
    {
        WorkshopRegistered domainEvent = CreateEvent();

        WorkshopRegisteredV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<WorkshopRegisteredV1>().Subject;

        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_CopyAllFields_When_WorkshopRegistered()
    {
        WorkshopRegistered domainEvent = CreateEvent();

        WorkshopRegisteredV1 result = (WorkshopRegisteredV1)_mapper.Map(domainEvent).Single();

        result.WorkshopId.Should().Be(domainEvent.WorkshopId.Value);
        result.TaxId.Should().Be(WorkshopTestFactory.TaxIdValue);
        result.Name.Should().Be(WorkshopTestFactory.WorkshopName);
        result.City.Should().Be(WorkshopTestFactory.CityValue);
        result.Status.Should().Be(WorkshopStatus.Active.ToString());
    }

    [Fact]
    public void Map_Should_UseWorkshopStream_When_WorkshopRegistered()
    {
        Type eventType = _mapper.Map(CreateEvent()).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.WorkshopsWorkshop);
    }

    private static WorkshopRegistered CreateEvent() =>
        new(
            WorkshopId.New(),
            TaxId.Create(WorkshopTestFactory.TaxIdValue).Value,
            WorkshopTestFactory.WorkshopName,
            WorkshopTestFactory.CityValue,
            WorkshopStatus.Active,
            DateTimeOffset.UnixEpoch);
}
