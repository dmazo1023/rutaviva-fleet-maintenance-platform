using NSubstitute;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.Fleet.Application.UnitTests.References;

internal static class ReferenceEventFactory
{
    public const string ActiveStatus = "Active";
    public const string InactiveStatus = "Inactive";
    public const string UnknownStatus = "Suspended";
    public const string TaxIdValue = "900123456-1";
    public const string NameValue = "Ruta Viva Logistics";
    public const string CityValue = "Bogota";
    public const string CustomerEmail = "owner@rutaviva.test";
    public const string FullNameValue = "Ana Approver";
    public const string IgnoredLogMarker = "ReferenceUpdateIgnored";

    private const string MetadataType = "Test";
    private const string MetadataSubject = "test.subject";
    private const long MetadataOffset = 1;
    private const int Year = 2026;
    private const int Month = 3;
    private const int Day = 10;
    private const int Hour = 10;
    private const int LaterHour = 12;

    public static DateTimeOffset Instant() => new(Year, Month, Day, Hour, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset LaterInstant() => new(Year, Month, Day, LaterHour, 0, 0, TimeSpan.Zero);

    public static EventMetadata Metadata() => new(Guid.NewGuid(), MetadataOffset, MetadataType, MetadataSubject, Instant());

    public static IUnitOfWork SucceedingUnitOfWork()
    {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        return unitOfWork;
    }
}
