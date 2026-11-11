using System.Text.Json;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.IntegrationContracts.ServiceOrders;
using RutaViva.IntegrationContracts.Workshops;

namespace RutaViva.IntegrationContracts.Tests;

internal static class IntegrationEventSamples
{
    private const string EnvelopeSource = "rutaviva.tests";
    private const string EnvelopeSubject = "sample-subject";
    private const string EnvelopeTraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";
    private const string EnvelopeTime = "2026-03-15T10:00:00+00:00";
    private const string AlertType = "DueSoon";
    private const string OpenStatus = "Open";
    private const string ResolvedStatus = "Resolved";
    private const string AcknowledgedBy = "ana.torres";
    private const string ResolutionReason = "ServiceCompleted";
    private const string ReadingOutcome = "Accepted";
    private const string SuspendedStatus = "Suspended";
    private const string AppointmentNotes = "Bring spare key";
    private const string ApprovalReason = "AmountAboveThreshold";
    private const string ApprovalPriority = "High";
    private const string PendingApprovalStatus = "PendingApproval";
    private const string RevertReason = "IntervalChanged";
    private const string DateTrigger = "Date";
    private const string CustomerName = "Transportes Andina";
    private const string CustomerEmail = "contact@andina.example.com";
    private const string WorkshopName = "Taller Central";
    private const string WorkshopCity = "Bogota";
    private const string RoutineLineType = "Routine";
    private const string AdditionalLineType = "Additional";
    private const string RoutineLineDescription = "Oil change service";
    private const string AdditionalLineDescription = "Brake pads";
    private const string PricedStatus = "Priced";
    private const string SystemRequester = "System";
    private const string WorkshopRequester = "Workshop";
    private const string ApprovedStatus = "Approved";
    private const string PendingStatus = "Pending";

    private static readonly IReadOnlyDictionary<string, Func<IIntegrationEvent>> Factories = new Dictionary<string, Func<IIntegrationEvent>>
    {
        [ApproverRegisteredV1.EventName] = () => new ApproverRegisteredV1(SampleValues.ApproverId, SampleValues.CustomerId, SampleValues.ApproverName, SampleValues.ApproverEmail, SampleValues.ActiveStatus, SampleValues.OccurredAt),
        [ApproverStatusChangedV1.EventName] = () => new ApproverStatusChangedV1(SampleValues.ApproverId, SampleValues.CustomerId, SampleValues.InactiveStatus, SampleValues.OccurredAt),
        [CustomerRegisteredV1.EventName] = () => new CustomerRegisteredV1(SampleValues.CustomerId, SampleValues.CustomerTaxId, CustomerName, CustomerEmail, SampleValues.ActiveStatus, SampleValues.OccurredAt),
        [CustomerStatusChangedV1.EventName] = () => new CustomerStatusChangedV1(SampleValues.CustomerId, SampleValues.InactiveStatus, SampleValues.OccurredAt),
        [VehicleRegisteredV1.EventName] = () => new VehicleRegisteredV1(SampleValues.VehicleId, SampleValues.Plate, SampleValues.Vin, SampleValues.Brand, SampleValues.Model, SampleValues.ModelYear, SampleValues.CustomerId, SampleValues.WorkshopId, SampleValues.ApproverId, SampleValues.ActiveStatus, SampleValues.InitialOdometerKm, SampleValues.RegisteredAt, SampleValues.OccurredAt),
        [VehicleResponsibleAssignedV1.EventName] = () => new VehicleResponsibleAssignedV1(SampleValues.VehicleId, SampleValues.ApproverId, SampleValues.OccurredAt),
        [VehicleStatusChangedV1.EventName] = () => new VehicleStatusChangedV1(SampleValues.VehicleId, SampleValues.Plate, SampleValues.CustomerId, SampleValues.InactiveStatus, SampleValues.OccurredAt),
        [AlertRaisedV1.EventName] = () => new AlertRaisedV1(SampleValues.AlertId, SampleValues.VehicleId, SampleValues.Plate, SampleValues.CustomerId, SampleValues.RoutineId, SampleValues.RoutineName, SampleValues.CycleNumber, AlertType, SampleValues.TriggeredBy, OpenStatus, SampleValues.RaisedAt, SampleValues.OccurredAt),
        [AlertStatusChangedV1.EventName] = () => new AlertStatusChangedV1(SampleValues.AlertId, SampleValues.VehicleId, SampleValues.Plate, SampleValues.CustomerId, SampleValues.RoutineId, AlertType, SampleValues.TriggeredBy, ResolvedStatus, SampleValues.AcknowledgedAt, AcknowledgedBy, SampleValues.ResolvedAt, ResolutionReason, SampleValues.OccurredAt),
        [MaintenancePlanStatusChangedV1.EventName] = () => new MaintenancePlanStatusChangedV1(SampleValues.VehicleId, SampleValues.Plate, SampleValues.CustomerId, SuspendedStatus, SampleValues.OccurredAt),
        [MaintenanceRoutineDueSoonV1.EventName] = () => new MaintenanceRoutineDueSoonV1(SampleValues.Routine(), SampleValues.TriggeredBy, SampleValues.OccurredAt),
        [MaintenanceRoutineDueStatusRevertedV1.EventName] = () => new MaintenanceRoutineDueStatusRevertedV1(SampleValues.Routine(), RevertReason, SampleValues.OccurredAt),
        [MaintenanceRoutineOverdueV1.EventName] = () => new MaintenanceRoutineOverdueV1(SampleValues.Routine(), DateTrigger, SampleValues.OccurredAt),
        [MaintenanceRoutineParametersChangedV1.EventName] = () => new MaintenanceRoutineParametersChangedV1(SampleValues.Routine(), SampleValues.OccurredAt),
        [MaintenanceRoutineRegisteredV1.EventName] = () => new MaintenanceRoutineRegisteredV1(SampleValues.Routine(), SampleValues.OccurredAt),
        [MaintenanceRoutineRestartedV1.EventName] = () => new MaintenanceRoutineRestartedV1(SampleValues.Routine(), SampleValues.ServiceOrderId, SampleValues.ServicedKm, SampleValues.ServicedAt, SampleValues.OccurredAt),
        [MaintenanceRoutineStateChangedV1.EventName] = () => new MaintenanceRoutineStateChangedV1(SampleValues.Routine(), SampleValues.OccurredAt),
        [OdometerReadingRecordedV1.EventName] = () => new OdometerReadingRecordedV1(SampleValues.VehicleId, SampleValues.Plate, SampleValues.CustomerId, SampleValues.ReadingKm, SampleValues.ReadAt, ReadingOutcome, null, SampleValues.ReadingKm, SampleValues.OccurredAt),
        [ServiceOrderAppointmentScheduledV1.EventName] = () => new ServiceOrderAppointmentScheduledV1(SampleValues.Order(), SampleValues.ScheduledAt, AppointmentNotes, false, SampleValues.OccurredAt),
        [ServiceOrderApprovalRequestedV1.EventName] = () => new ServiceOrderApprovalRequestedV1(SampleValues.Order(), ApprovalReason, ApprovalPriority, SampleValues.OccurredAt),
        [ServiceOrderCompletedV1.EventName] = () => new ServiceOrderCompletedV1(SampleValues.Order(), SampleValues.ServicedKm, SampleValues.ServicedAt, SampleValues.OccurredAt),
        [ServiceOrderCreatedV1.EventName] = () => new ServiceOrderCreatedV1(SampleValues.Order(), SampleValues.OccurredAt),
        [ServiceOrderLinesChangedV1.EventName] = () => new ServiceOrderLinesChangedV1(SampleValues.Order(), Lines(), SampleValues.OccurredAt),
        [ServiceOrderStatusChangedV1.EventName] = () => new ServiceOrderStatusChangedV1(SampleValues.Order(), SampleValues.OrderStatus, PendingApprovalStatus, SampleValues.OccurredAt),
        [WorkshopRegisteredV1.EventName] = () => new WorkshopRegisteredV1(SampleValues.WorkshopId, SampleValues.WorkshopTaxId, WorkshopName, WorkshopCity, SampleValues.ActiveStatus, SampleValues.OccurredAt),
        [WorkshopStatusChangedV1.EventName] = () => new WorkshopStatusChangedV1(SampleValues.WorkshopId, SampleValues.InactiveStatus, SampleValues.OccurredAt)
    };

    public static IReadOnlyCollection<string> EventNames => Factories.Keys.ToArray();

    public static IIntegrationEvent Create(string eventName) => Factories[eventName]();

    public static IntegrationEventEnvelope CreateEnvelope(string eventName) => new()
    {
        Id = SampleValues.EnvelopeId,
        Source = EnvelopeSource,
        Type = eventName,
        Subject = EnvelopeSubject,
        Time = DateTimeOffset.Parse(EnvelopeTime, System.Globalization.CultureInfo.InvariantCulture),
        TraceParent = EnvelopeTraceParent,
        Data = JsonSerializer.SerializeToElement(Create(eventName), IntegrationEventCatalog.TypeOf(eventName), ContractSnapshot.Options)
    };

    private static ServiceOrderLineSnapshot[] Lines() =>
    [
        new(SampleValues.FirstLineId, RoutineLineType, RoutineLineDescription, SampleValues.RoutineAmount, PricedStatus, SystemRequester, ApprovedStatus),
        new(SampleValues.SecondLineId, AdditionalLineType, AdditionalLineDescription, SampleValues.AdditionalAmount, PricedStatus, WorkshopRequester, PendingStatus)
    ];
}
