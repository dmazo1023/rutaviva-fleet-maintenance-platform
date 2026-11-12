import {
  AlertResolutionReason,
  AlertStatus,
  AlertTrigger,
  AlertType,
  ApprovalPriority,
  ApprovalReason,
  ApproverStatus,
  CancellationReason,
  CustomerStatus,
  FileEncoding,
  ImportEntityType,
  ImportJobStatus,
  ImportRowOutcome,
  ImportStageStatus,
  MaintenancePlanStatus,
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
  PricingStatus,
  Profile,
  ReportCode,
  ReportFormat,
  RoutineDueStatus,
  RoutineState,
  RoutineTypeStatus,
  ServiceLineApprovalStatus,
  ServiceLineRequester,
  ServiceLineType,
  ServiceOrderAction,
  ServiceOrderBlockedReason,
  ServiceOrderChangeType,
  ServiceOrderOrigin,
  ServiceOrderStatus,
  VehicleStatus,
  WorkshopStatus,
} from './enums';

export interface EnumLabelsCatalog {
  readonly customerStatus: Readonly<Record<CustomerStatus, string>>;
  readonly approverStatus: Readonly<Record<ApproverStatus, string>>;
  readonly workshopStatus: Readonly<Record<WorkshopStatus, string>>;
  readonly vehicleStatus: Readonly<Record<VehicleStatus, string>>;
  readonly routineTypeStatus: Readonly<Record<RoutineTypeStatus, string>>;
  readonly maintenancePlanStatus: Readonly<Record<MaintenancePlanStatus, string>>;
  readonly routineState: Readonly<Record<RoutineState, string>>;
  readonly routineDueStatus: Readonly<Record<RoutineDueStatus, string>>;
  readonly alertType: Readonly<Record<AlertType, string>>;
  readonly alertTrigger: Readonly<Record<AlertTrigger, string>>;
  readonly alertStatus: Readonly<Record<AlertStatus, string>>;
  readonly odometerReadingOutcome: Readonly<Record<OdometerReadingOutcome, string>>;
  readonly odometerReadingIgnoredReason: Readonly<Record<OdometerReadingIgnoredReason, string>>;
  readonly alertResolutionReason: Readonly<Record<AlertResolutionReason, string>>;
  readonly serviceOrderStatus: Readonly<Record<ServiceOrderStatus, string>>;
  readonly serviceOrderOrigin: Readonly<Record<ServiceOrderOrigin, string>>;
  readonly cancellationReason: Readonly<Record<CancellationReason, string>>;
  readonly approvalReason: Readonly<Record<ApprovalReason, string>>;
  readonly approvalPriority: Readonly<Record<ApprovalPriority, string>>;
  readonly serviceLineType: Readonly<Record<ServiceLineType, string>>;
  readonly pricingStatus: Readonly<Record<PricingStatus, string>>;
  readonly serviceLineApprovalStatus: Readonly<Record<ServiceLineApprovalStatus, string>>;
  readonly serviceLineRequester: Readonly<Record<ServiceLineRequester, string>>;
  readonly serviceOrderChangeType: Readonly<Record<ServiceOrderChangeType, string>>;
  readonly importJobStatus: Readonly<Record<ImportJobStatus, string>>;
  readonly importEntityType: Readonly<Record<ImportEntityType, string>>;
  readonly importStageStatus: Readonly<Record<ImportStageStatus, string>>;
  readonly fileEncoding: Readonly<Record<FileEncoding, string>>;
  readonly importRowOutcome: Readonly<Record<ImportRowOutcome, string>>;
  readonly reportCode: Readonly<Record<ReportCode, string>>;
  readonly reportFormat: Readonly<Record<ReportFormat, string>>;
  readonly serviceOrderAction: Readonly<Record<ServiceOrderAction, string>>;
  readonly serviceOrderBlockedReason: Readonly<Record<ServiceOrderBlockedReason, string>>;
  readonly profile: Readonly<Record<Profile, string>>;
}

export const ENUM_LABELS: EnumLabelsCatalog = {
  customerStatus: {
    Active: 'Activo',
    Inactive: 'Inactivo',
  },
  approverStatus: {
    Active: 'Activo',
    Inactive: 'Inactivo',
  },
  workshopStatus: {
    Active: 'Activo',
    Inactive: 'Inactivo',
  },
  vehicleStatus: {
    Active: 'Activo',
    Inactive: 'Inactivo',
  },
  routineTypeStatus: {
    Active: 'Activa',
    Inactive: 'Inactiva',
  },
  maintenancePlanStatus: {
    Active: 'Activo',
    Suspended: 'En pausa',
  },
  routineState: {
    Active: 'Activa',
    Inactive: 'Inactiva',
  },
  routineDueStatus: {
    UpToDate: 'Al día',
    DueSoon: 'Por vencer',
    Overdue: 'Vencida',
  },
  alertType: {
    DueSoon: 'Por vencer',
    Overdue: 'Vencida',
  },
  alertTrigger: {
    Distance: 'Por km',
    Time: 'Por tiempo',
    DistanceAndTime: 'Por km y tiempo',
  },
  alertStatus: {
    Active: 'Activa',
    Acknowledged: 'Atendida',
    Resolved: 'Resuelta',
  },
  odometerReadingOutcome: {
    Accepted: 'Aceptada',
    Ignored: 'Ignorada',
  },
  odometerReadingIgnoredReason: {
    NotGreaterThanCurrent: 'No es mayor que el odómetro actual',
    UnknownVehicle: 'Vehículo desconocido',
  },
  alertResolutionReason: {
    RoutineRestarted: 'Resuelta: rutina reiniciada',
    EscalatedToOverdue: 'Resuelta: escalada a vencida',
    ParametersChanged: 'Resuelta: parámetros cambiados',
    RoutineDeactivated: 'Resuelta: rutina desactivada',
  },
  serviceOrderStatus: {
    Created: 'Creada',
    Quoted: 'Cotizada',
    PendingApproval: 'Por aprobar',
    Approved: 'Aprobada',
    OnSite: 'En sitio',
    InProgress: 'En proceso',
    OnHold: 'En pausa',
    Completed: 'Finalizada',
    Cancelled: 'Cancelada',
  },
  serviceOrderOrigin: {
    Automatic: 'Automática',
    Manual: 'Manual',
  },
  cancellationReason: {
    QuoteRejected: 'Cotización rechazada',
    CancelledByCustomer: 'Cancelada por el cliente',
  },
  approvalReason: {
    Quote: 'Cotización',
    AdditionalServices: 'Servicios adicionales',
  },
  approvalPriority: {
    Normal: 'Normal',
    High: 'Prioritaria',
  },
  serviceLineType: {
    Routine: 'Rutina',
    Additional: 'Adicional',
  },
  pricingStatus: {
    Priced: 'Con precio',
    PendingPricing: 'Sin precio',
  },
  serviceLineApprovalStatus: {
    Approved: 'Aprobado',
    PendingApproval: 'Por aprobar',
    Rejected: 'Rechazado',
  },
  serviceLineRequester: {
    Workshop: 'Taller',
    Approver: 'Aprobador',
  },
  serviceOrderChangeType: {
    StatusChanged: 'Cambio de estado',
    AppointmentScheduled: 'Cita agendada',
    AppointmentRescheduled: 'Cita reprogramada',
    AdditionalServiceAdded: 'Servicio adicional agregado',
  },
  importJobStatus: {
    Validating: 'Validando',
    PendingConfirmation: 'Por confirmar',
    Importing: 'Importando',
    Completed: 'Completada',
    CompletedWithErrors: 'Completada con errores',
    Failed: 'Fallida',
  },
  importEntityType: {
    Workshops: 'Talleres',
    Customers: 'Clientes',
    Approvers: 'Aprobadores',
    Vehicles: 'Vehículos',
    Routines: 'Rutinas',
  },
  importStageStatus: {
    Pending: 'Pendiente',
    Validated: 'Validada',
    Imported: 'Importada',
    Failed: 'Fallida',
  },
  fileEncoding: {
    Utf8: 'UTF-8',
    Utf8Bom: 'UTF-8 con BOM',
    Windows1252: 'Windows-1252',
  },
  importRowOutcome: {
    Valid: 'Válida',
    Invalid: 'Inválida',
    Skipped: 'Omitida',
    Imported: 'Importada',
    Failed: 'Fallida',
  },
  reportCode: {
    FleetMaintenanceStatus: 'Estado de mantenimiento de la flota',
    ServiceOrders: 'Órdenes de servicio',
    MaintenanceCosts: 'Costos de mantenimiento',
    AlertsAttention: 'Alertas y atención',
  },
  reportFormat: {
    Xlsx: 'Excel',
    Csv: 'CSV',
  },
  serviceOrderAction: {
    Approve: 'Aprobar',
    Reject: 'Rechazar',
    RequestAdditionalService: 'Pedir servicio',
    Cancel: 'Cancelar orden',
    Quote: 'Cotizar',
    PriceAdditionalService: 'Guardar precio',
    ScheduleAppointment: 'Agendar cita',
    MarkOnSite: 'En sitio',
    AddAdditionalService: 'Agregar servicio',
    Start: 'Iniciar',
    Complete: 'Finalizar',
  },
  serviceOrderBlockedReason: {
    WaitingForPrice: 'Esperando precio del taller',
    AppointmentRequired: 'Sin cita',
  },
  profile: {
    Operator: 'Operador',
    Approver: 'Aprobador',
    Workshop: 'Taller',
  },
};

