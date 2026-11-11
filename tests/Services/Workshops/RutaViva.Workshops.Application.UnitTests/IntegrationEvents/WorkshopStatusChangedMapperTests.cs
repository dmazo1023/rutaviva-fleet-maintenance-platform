using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.Workshops.Application.IntegrationEvents.Outbound;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.Events;

namespace RutaViva.Workshops.Application.UnitTests.IntegrationEvents;

public sealed class WorkshopStatusChangedMapperTests
{
    private readonly WorkshopStatusChangedMapper _mapper = new();

    [Theory]
    [InlineData(WorkshopStatus.Active)]
    [InlineData(WorkshopStatus.Inactive)]
    public void Map_Should_CopyAllFields_When_WorkshopStatusChanged(WorkshopStatus status)
    {
        WorkshopStatusChanged domainEvent = new(WorkshopId.New(), status, DateTimeOffset.UnixEpoch);

        WorkshopStatusChangedV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<WorkshopStatusChangedV1>().Subject;

        result.WorkshopId.Should().Be(domainEvent.WorkshopId.Value);
        result.Status.Should().Be(status.ToString());
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_UseWorkshopStream_When_WorkshopStatusChanged()
    {
        WorkshopStatusChanged domainEvent = new(WorkshopId.New(), WorkshopStatus.Inactive, DateTimeOffset.UnixEpoch);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.WorkshopsWorkshop);
    }
}
