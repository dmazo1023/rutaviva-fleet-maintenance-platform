using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.UnitTests.Common;

public sealed class VehicleErrorsTests
{
    private const string OwnerNotFoundCode = "Vehicle.OwnerNotFound";
    private const string OwnerInactiveCode = "Vehicle.OwnerInactive";
    private const string WorkshopNotFoundCode = "Vehicle.WorkshopNotFound";
    private const string WorkshopInactiveCode = "Vehicle.WorkshopInactive";
    private const string ResponsibleNotFoundCode = "Vehicle.ResponsibleNotFound";
    private const string ResponsibleInactiveCode = "Vehicle.ResponsibleInactive";
    private const string ResponsibleAmbiguousCode = "Vehicle.ResponsibleAmbiguous";
    private const string PlateAlreadyRegisteredCode = "Vehicle.PlateAlreadyRegistered";
    private const string VinAlreadyRegisteredCode = "Vehicle.VinAlreadyRegistered";

    public static TheoryData<Error, string, ErrorType> Catalog => new()
    {
        { VehicleErrors.OwnerNotFound, OwnerNotFoundCode, ErrorType.BusinessRule },
        { VehicleErrors.OwnerInactive, OwnerInactiveCode, ErrorType.BusinessRule },
        { VehicleErrors.WorkshopNotFound, WorkshopNotFoundCode, ErrorType.BusinessRule },
        { VehicleErrors.WorkshopInactive, WorkshopInactiveCode, ErrorType.BusinessRule },
        { VehicleErrors.ResponsibleNotFound, ResponsibleNotFoundCode, ErrorType.BusinessRule },
        { VehicleErrors.ResponsibleInactive, ResponsibleInactiveCode, ErrorType.BusinessRule },
        { VehicleErrors.ResponsibleAmbiguous, ResponsibleAmbiguousCode, ErrorType.BusinessRule },
        { VehicleErrors.PlateAlreadyRegistered, PlateAlreadyRegisteredCode, ErrorType.Conflict },
        { VehicleErrors.VinAlreadyRegistered, VinAlreadyRegisteredCode, ErrorType.Conflict }
    };

    [Theory]
    [MemberData(nameof(Catalog))]
    public void Catalog_Should_ExposeStableCodeAndType_When_ErrorIsReferenced(Error error, string code, ErrorType type)
    {
        error.Code.Should().Be(code);
        error.Type.Should().Be(type);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }
}
