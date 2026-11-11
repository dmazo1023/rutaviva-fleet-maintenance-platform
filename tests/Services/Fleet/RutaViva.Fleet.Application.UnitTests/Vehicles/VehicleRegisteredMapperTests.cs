using RutaViva.Fleet.Application.IntegrationEvents.Outbound;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;
using RutaViva.Fleet.Domain.Vehicles.Events;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.Fleet;

namespace RutaViva.Fleet.Application.UnitTests.Vehicles;

public sealed class VehicleRegisteredMapperTests
{
    private readonly VehicleRegisteredMapper _mapper = new();

    [Fact]
    public void Map_Should_CopyAllFields_When_VehicleHasVinAndResponsible()
    {
        ApproverId responsibleId = ApproverId.New();
        VehicleRegistered domainEvent = CreateEvent(Vin.Create(VehicleTestFactory.VinText).Value, responsibleId);

        VehicleRegisteredV1 result = _mapper.Map(domainEvent)
            .Should().ContainSingle().Which
            .Should().BeOfType<VehicleRegisteredV1>().Subject;

        result.VehicleId.Should().Be(domainEvent.VehicleId.Value);
        result.Plate.Should().Be(VehicleTestFactory.PlateText);
        result.Vin.Should().Be(VehicleTestFactory.VinText);
        result.Brand.Should().Be(VehicleTestFactory.BrandText);
        result.Model.Should().Be(VehicleTestFactory.ModelText);
        result.ModelYear.Should().Be(VehicleTestFactory.ModelYear);
        result.CustomerId.Should().Be(domainEvent.CustomerId.Value);
        result.WorkshopId.Should().Be(domainEvent.WorkshopId.Value);
        result.ResponsibleApproverId.Should().Be(responsibleId.Value);
        result.Status.Should().Be(VehicleStatus.Active.ToString());
        result.InitialOdometerKm.Should().Be(VehicleTestFactory.OdometerKm);
        result.RegisteredAt.Should().Be(domainEvent.RegisteredAt);
        result.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void Map_Should_LeaveOptionalFieldsNull_When_VehicleHasNoVinNorResponsible()
    {
        VehicleRegistered domainEvent = CreateEvent(null, null);

        VehicleRegisteredV1 result = (VehicleRegisteredV1)_mapper.Map(domainEvent).Single();

        result.Vin.Should().BeNull();
        result.ResponsibleApproverId.Should().BeNull();
    }

    [Fact]
    public void Map_Should_UseFleetVehicleStream_When_VehicleRegistered()
    {
        VehicleRegistered domainEvent = CreateEvent(null, null);

        Type eventType = _mapper.Map(domainEvent).Single().GetType();

        IntegrationEventDescriptor.For(eventType).Stream.Should().Be(Streams.FleetVehicle);
    }

    private static VehicleRegistered CreateEvent(Vin? vin, ApproverId? responsibleId) =>
        new(
            VehicleId.New(),
            Plate.Create(VehicleTestFactory.PlateText).Value,
            vin,
            VehicleDescription.Create(VehicleTestFactory.BrandText, VehicleTestFactory.ModelText, VehicleTestFactory.ModelYear, VehicleTestFactory.CurrentYear).Value,
            CustomerId.New(),
            WorkshopId.New(),
            responsibleId,
            VehicleStatus.Active,
            Kilometers.Create(VehicleTestFactory.OdometerKm).Value,
            VehicleTestFactory.Now,
            VehicleTestFactory.Later);
}
