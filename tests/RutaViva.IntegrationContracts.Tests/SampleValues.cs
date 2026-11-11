using System.Globalization;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.IntegrationContracts.ServiceOrders;

namespace RutaViva.IntegrationContracts.Tests;

internal static class SampleValues
{
    public const string Plate = "ABC123";
    public const string RoutineName = "Oil change";
    public const string ActiveStatus = "Active";
    public const string InactiveStatus = "Inactive";
    public const string Vin = "1HGCM82633A004352";
    public const string Brand = "Toyota";
    public const string Model = "Hilux";
    public const string Currency = "COP";
    public const string OrderNumber = "SO-0001";
    public const string OrderOrigin = "Routine";
    public const string OrderStatus = "Created";
    public const string ApproverName = "Ana Torres";
    public const string ApproverEmail = "ana.torres@example.com";
    public const string TriggeredBy = "Odometer";
    public const string CustomerTaxId = "900123456";
    public const string WorkshopTaxId = "800765432";
    public const int ModelYear = 2024;
    public const int CycleNumber = 3;
    public const long InitialOdometerKm = 15000;
    public const long CycleStartKm = 10000;
    public const long NextDueKm = 15000;
    public const long CurrentOdometerKm = 14200;
    public const long ServicedKm = 14800;
    public const long ReadingKm = 15200;
    public const decimal RoutineAmount = 1200.5m;
    public const decimal AdditionalAmount = 300.25m;
    public const decimal TotalAmount = 1500.75m;

    private const string RoutineTypeCode = "OIL_CHANGE";
    private const string RoutineState = "Active";
    private const string DueSoonStatus = "DueSoon";
    private const int IntervalKm = 5000;
    private const int IntervalMonths = 6;
    private const int DueSoonPercent = 80;
    private const decimal ConsumedPercent = 84.5m;
    private const string CycleStartDateText = "2026-01-10";
    private const string NextDueDateText = "2026-07-10";
    private const string OccurredAtText = "2026-03-15T10:00:00+00:00";
    private const string RaisedAtText = "2026-03-15T09:55:00+00:00";
    private const string AcknowledgedAtText = "2026-03-15T09:58:00+00:00";
    private const string ResolvedAtText = "2026-03-15T09:59:00+00:00";
    private const string ServicedAtText = "2026-03-14T16:00:00+00:00";
    private const string RegisteredAtText = "2026-01-05T08:00:00+00:00";
    private const string ScheduledAtText = "2026-03-20T09:00:00+00:00";
    private const string ReadAtText = "2026-03-15T09:50:00+00:00";
    private const string CreatedAtText = "2026-03-14T08:30:00+00:00";

    public static readonly Guid VehicleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid WorkshopId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ApproverId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid RoutineId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid AlertId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid ServiceOrderId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid FirstLineId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid SecondLineId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid EnvelopeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static readonly DateTimeOffset OccurredAt = Instant(OccurredAtText);
    public static readonly DateTimeOffset RaisedAt = Instant(RaisedAtText);
    public static readonly DateTimeOffset AcknowledgedAt = Instant(AcknowledgedAtText);
    public static readonly DateTimeOffset ResolvedAt = Instant(ResolvedAtText);
    public static readonly DateTimeOffset ServicedAt = Instant(ServicedAtText);
    public static readonly DateTimeOffset RegisteredAt = Instant(RegisteredAtText);
    public static readonly DateTimeOffset ScheduledAt = Instant(ScheduledAtText);
    public static readonly DateTimeOffset ReadAt = Instant(ReadAtText);
    public static readonly DateTimeOffset CreatedAt = Instant(CreatedAtText);

    public static RoutineSnapshot Routine() => new(
        RoutineId,
        VehicleId,
        Plate,
        CustomerId,
        RoutineTypeCode,
        RoutineName,
        RoutineState,
        DueSoonStatus,
        CycleNumber,
        CycleStartKm,
        DateOnly.Parse(CycleStartDateText, CultureInfo.InvariantCulture),
        IntervalKm,
        IntervalMonths,
        DueSoonPercent,
        NextDueKm,
        DateOnly.Parse(NextDueDateText, CultureInfo.InvariantCulture),
        CurrentOdometerKm,
        ConsumedPercent);

    public static ServiceOrderSnapshot Order() => new(
        ServiceOrderId,
        OrderNumber,
        OrderOrigin,
        VehicleId,
        Plate,
        RoutineId,
        RoutineName,
        CustomerId,
        WorkshopId,
        ApproverId,
        OrderStatus,
        null,
        null,
        null,
        RoutineAmount,
        AdditionalAmount,
        TotalAmount,
        Currency,
        false,
        ScheduledAt,
        null,
        CreatedAt);

    private static DateTimeOffset Instant(string text) => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
}
