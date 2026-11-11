/*
    RutaViva - Esquema de referencia (SQL Server 2019+)
    Fuente de verdad de implementación: migraciones EF Core de cada servicio.
    Uso: sqlcmd -S localhost -E -i rutaviva-reference-schema.sql
         -v CustomersAppPassword="..." WorkshopsAppPassword="..." FleetAppPassword="..."
            MaintenanceAppPassword="..." ServiceOrdersAppPassword="..."
            ImportsAppPassword="..." ReportingAppPassword="..." EventBusAppPassword="..."
    ADR-0028: en local los servicios se conectan con autenticación de Windows (Trusted_Connection);
    los usuarios rutaviva_{schema}_app se crean igual como control verificable de aislamiento.
    RowVersion es una propiedad sombra en EF Core: el dominio no la conoce; la columna rowversion existe en la tabla.
    Los kilómetros son bigint.
*/

IF DB_ID(N'RutaViva') IS NULL
    CREATE DATABASE RutaViva;
GO

USE RutaViva;
GO

CREATE SCHEMA customers;
GO
CREATE SCHEMA workshops;
GO
CREATE SCHEMA fleet;
GO
CREATE SCHEMA maintenance;
GO
CREATE SCHEMA serviceorders;
GO
CREATE SCHEMA imports;
GO
CREATE SCHEMA reporting;
GO
CREATE SCHEMA eventbus;
GO

/* ===================== customers ===================== */

CREATE TABLE customers.Customers (
    Id          uniqueidentifier   NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Name        nvarchar(150)      NOT NULL,
    TaxId       varchar(20)        NOT NULL CONSTRAINT UX_Customers_TaxId UNIQUE,
    Email       nvarchar(254)      NOT NULL,
    Phone       varchar(20)        NULL,
    Status      varchar(20)        NOT NULL CONSTRAINT CK_Customers_Status CHECK (Status IN ('Active', 'Inactive')),
    CreatedAt   datetimeoffset(0)  NOT NULL,
    RowVersion  rowversion         NOT NULL
);

CREATE TABLE customers.Approvers (
    Id          uniqueidentifier   NOT NULL CONSTRAINT PK_Approvers PRIMARY KEY,
    CustomerId  uniqueidentifier   NOT NULL CONSTRAINT FK_Approvers_Customers
                                   REFERENCES customers.Customers (Id) ON DELETE CASCADE,
    FullName    nvarchar(150)      NOT NULL,
    Email       nvarchar(254)      NOT NULL,
    Status      varchar(20)        NOT NULL CONSTRAINT CK_Approvers_Status CHECK (Status IN ('Active', 'Inactive')),
    CreatedAt   datetimeoffset(0)  NOT NULL,
    CONSTRAINT UX_Approvers_CustomerId_Email UNIQUE (CustomerId, Email)
);

/* ===================== workshops ===================== */

CREATE TABLE workshops.Workshops (
    Id          uniqueidentifier   NOT NULL CONSTRAINT PK_Workshops PRIMARY KEY,
    Name        nvarchar(150)      NOT NULL,
    TaxId       varchar(20)        NOT NULL CONSTRAINT UX_Workshops_TaxId UNIQUE,
    Email       nvarchar(254)      NOT NULL,
    Phone       varchar(20)        NULL,
    Address     nvarchar(250)      NOT NULL,
    City        nvarchar(100)      NOT NULL,
    Status      varchar(20)        NOT NULL CONSTRAINT CK_Workshops_Status CHECK (Status IN ('Active', 'Inactive')),
    CreatedAt   datetimeoffset(0)  NOT NULL,
    RowVersion  rowversion         NOT NULL
);

/* ===================== fleet ===================== */

CREATE TABLE fleet.Vehicles (
    Id                     uniqueidentifier   NOT NULL CONSTRAINT PK_Vehicles PRIMARY KEY,
    Plate                  varchar(10)        NOT NULL CONSTRAINT UX_Vehicles_Plate UNIQUE,
    Vin                    varchar(17)        NULL,
    Brand                  nvarchar(50)       NOT NULL,
    Model                  nvarchar(50)       NOT NULL,
    ModelYear              smallint           NOT NULL CONSTRAINT CK_Vehicles_ModelYear CHECK (ModelYear BETWEEN 1950 AND 2100),
    CustomerId             uniqueidentifier   NOT NULL,
    WorkshopId             uniqueidentifier   NOT NULL,
    ResponsibleApproverId  uniqueidentifier   NULL,
    Status                 varchar(20)        NOT NULL CONSTRAINT CK_Vehicles_Status CHECK (Status IN ('Active', 'Inactive')),
    StatusChangedAt        datetimeoffset(0)  NOT NULL,
    InitialOdometerKm      bigint             NOT NULL CONSTRAINT CK_Vehicles_InitialOdometer CHECK (InitialOdometerKm >= 0),
    RegisteredAt           datetimeoffset(0)  NOT NULL,
    RowVersion             rowversion         NOT NULL
);

CREATE UNIQUE INDEX UX_Vehicles_Vin ON fleet.Vehicles (Vin) WHERE Vin IS NOT NULL;
CREATE INDEX IX_Vehicles_CustomerId_Status ON fleet.Vehicles (CustomerId, Status);

CREATE TABLE fleet.CustomerReferences (
    CustomerId  uniqueidentifier   NOT NULL CONSTRAINT PK_Fleet_CustomerReferences PRIMARY KEY,
    TaxId       varchar(20)        NOT NULL,
    Name        nvarchar(150)      NOT NULL,
    Status      varchar(20)        NOT NULL,
    UpdatedAt   datetimeoffset(0)  NOT NULL
);

CREATE UNIQUE INDEX UX_CustomerReferences_TaxId ON fleet.CustomerReferences (TaxId);

CREATE TABLE fleet.WorkshopReferences (
    WorkshopId  uniqueidentifier   NOT NULL CONSTRAINT PK_Fleet_WorkshopReferences PRIMARY KEY,
    TaxId       varchar(20)        NOT NULL,
    Name        nvarchar(150)      NOT NULL,
    Status      varchar(20)        NOT NULL,
    UpdatedAt   datetimeoffset(0)  NOT NULL
);

CREATE UNIQUE INDEX UX_WorkshopReferences_TaxId ON fleet.WorkshopReferences (TaxId);

CREATE TABLE fleet.ApproverReferences (
    ApproverId  uniqueidentifier   NOT NULL CONSTRAINT PK_Fleet_ApproverReferences PRIMARY KEY,
    CustomerId  uniqueidentifier   NOT NULL,
    FullName    nvarchar(150)      NOT NULL,
    Email       nvarchar(254)      NOT NULL,
    Status      varchar(20)        NOT NULL,
    UpdatedAt   datetimeoffset(0)  NOT NULL
);

CREATE INDEX IX_ApproverReferences_Email ON fleet.ApproverReferences (Email);

/* ===================== maintenance ===================== */

CREATE TABLE maintenance.RoutineTypes (
    Id                       uniqueidentifier   NOT NULL CONSTRAINT PK_RoutineTypes PRIMARY KEY,
    Code                     varchar(30)        NOT NULL CONSTRAINT UX_RoutineTypes_Code UNIQUE,
    Name                     nvarchar(100)      NOT NULL,
    DefaultIntervalKm        int                NULL CONSTRAINT CK_RoutineTypes_DefaultIntervalKm CHECK (DefaultIntervalKm > 0),
    DefaultIntervalMonths    smallint           NULL CONSTRAINT CK_RoutineTypes_DefaultIntervalMonths CHECK (DefaultIntervalMonths > 0),
    DueSoonThresholdPercent  decimal(5, 2)      NOT NULL CONSTRAINT DF_RoutineTypes_DueSoonThresholdPercent DEFAULT (10.00)
                                                CONSTRAINT CK_RoutineTypes_DueSoonThresholdPercent CHECK (DueSoonThresholdPercent > 0 AND DueSoonThresholdPercent < 100),
    Status                   varchar(20)        NOT NULL CONSTRAINT CK_RoutineTypes_Status CHECK (Status IN ('Active', 'Inactive')),
    UpdatedBy                nvarchar(150)      NOT NULL,
    UpdatedAt                datetimeoffset(0)  NOT NULL,
    RowVersion               rowversion         NOT NULL,
    CONSTRAINT CK_RoutineTypes_HasDefaultInterval CHECK (DefaultIntervalKm IS NOT NULL OR DefaultIntervalMonths IS NOT NULL)
);

CREATE TABLE maintenance.MaintenancePlans (
    VehicleId               uniqueidentifier   NOT NULL CONSTRAINT PK_MaintenancePlans PRIMARY KEY,
    Plate                   varchar(10)        NOT NULL,
    CustomerId              uniqueidentifier   NOT NULL,
    Status                  varchar(20)        NOT NULL CONSTRAINT CK_MaintenancePlans_Status CHECK (Status IN ('Active', 'Suspended')),
    OdometerKm              bigint             NOT NULL CONSTRAINT CK_MaintenancePlans_Odometer CHECK (OdometerKm >= 0),
    OdometerReadAt          datetimeoffset(0)  NULL,
    LastTimeEvaluationDate  date               NULL,
    RowVersion              rowversion         NOT NULL
);

CREATE INDEX IX_MaintenancePlans_Status ON maintenance.MaintenancePlans (Status) INCLUDE (CustomerId);
CREATE UNIQUE INDEX UX_MaintenancePlans_Plate ON maintenance.MaintenancePlans (Plate);

CREATE TABLE maintenance.MaintenanceRoutines (
    Id                               uniqueidentifier   NOT NULL CONSTRAINT PK_MaintenanceRoutines PRIMARY KEY,
    VehicleId                        uniqueidentifier   NOT NULL CONSTRAINT FK_MaintenanceRoutines_MaintenancePlans
                                                        REFERENCES maintenance.MaintenancePlans (VehicleId) ON DELETE CASCADE,
    RoutineTypeId                    uniqueidentifier   NOT NULL CONSTRAINT FK_MaintenanceRoutines_RoutineTypes
                                                        REFERENCES maintenance.RoutineTypes (Id),
    IntervalKm                       int                NULL CONSTRAINT CK_MaintenanceRoutines_IntervalKm CHECK (IntervalKm > 0),
    IntervalMonths                   smallint           NULL CONSTRAINT CK_MaintenanceRoutines_IntervalMonths CHECK (IntervalMonths > 0),
    DueSoonThresholdPercentOverride  decimal(5, 2)      NULL CONSTRAINT CK_MaintenanceRoutines_ThresholdOverride
                                                        CHECK (DueSoonThresholdPercentOverride > 0 AND DueSoonThresholdPercentOverride < 100),
    State                            varchar(20)        NOT NULL CONSTRAINT CK_MaintenanceRoutines_State CHECK (State IN ('Active', 'Inactive')),
    DueStatus                        varchar(20)        NOT NULL CONSTRAINT CK_MaintenanceRoutines_DueStatus
                                                        CHECK (DueStatus IN ('UpToDate', 'DueSoon', 'Overdue')),
    CycleNumber                      int                NOT NULL CONSTRAINT CK_MaintenanceRoutines_CycleNumber CHECK (CycleNumber >= 1),
    CycleStartKm                     bigint             NOT NULL CONSTRAINT CK_MaintenanceRoutines_CycleStartKm CHECK (CycleStartKm >= 0),
    CycleStartDate                   date               NOT NULL,
    DueSoonNotified                  bit                NOT NULL CONSTRAINT DF_MaintenanceRoutines_DueSoonNotified DEFAULT (0),
    UpdatedBy                        nvarchar(150)      NOT NULL,
    UpdatedAt                        datetimeoffset(0)  NOT NULL,
    CONSTRAINT CK_MaintenanceRoutines_HasInterval CHECK (IntervalKm IS NOT NULL OR IntervalMonths IS NOT NULL),
    CONSTRAINT UX_MaintenanceRoutines_VehicleId_RoutineTypeId UNIQUE (VehicleId, RoutineTypeId)
);

CREATE INDEX IX_MaintenanceRoutines_RoutineTypeId ON maintenance.MaintenanceRoutines (RoutineTypeId) INCLUDE (VehicleId);

CREATE TABLE maintenance.Alerts (
    Id                uniqueidentifier   NOT NULL CONSTRAINT PK_Alerts PRIMARY KEY,
    VehicleId         uniqueidentifier   NOT NULL,
    Plate             varchar(10)        NOT NULL,
    CustomerId        uniqueidentifier   NOT NULL,
    RoutineName       nvarchar(100)      NOT NULL,
    RoutineId         uniqueidentifier   NOT NULL CONSTRAINT FK_Alerts_MaintenanceRoutines
                                         REFERENCES maintenance.MaintenanceRoutines (Id) ON DELETE CASCADE,
    CycleNumber       int                NOT NULL,
    Type              varchar(20)        NOT NULL CONSTRAINT CK_Alerts_Type CHECK (Type IN ('DueSoon', 'Overdue')),
    TriggeredBy       varchar(20)        NOT NULL CONSTRAINT CK_Alerts_TriggeredBy CHECK (TriggeredBy IN ('Distance', 'Time', 'DistanceAndTime')),
    Status            varchar(20)        NOT NULL CONSTRAINT CK_Alerts_Status CHECK (Status IN ('Active', 'Acknowledged', 'Resolved')),
    RaisedAt          datetimeoffset(0)  NOT NULL,
    AcknowledgedAt    datetimeoffset(0)  NULL,
    AcknowledgedBy    nvarchar(150)      NULL,
    ResolvedAt        datetimeoffset(0)  NULL,
    ResolutionReason  varchar(30)        NULL CONSTRAINT CK_Alerts_ResolutionReason
                                         CHECK (ResolutionReason IN ('RoutineRestarted', 'EscalatedToOverdue', 'ParametersChanged', 'RoutineDeactivated')),
    RowVersion        rowversion         NOT NULL,
    CONSTRAINT UX_Alerts_OnePerCycleAndType UNIQUE (RoutineId, CycleNumber, Type),
    CONSTRAINT CK_Alerts_AcknowledgedData CHECK (Status <> 'Acknowledged' OR AcknowledgedAt IS NOT NULL),
    CONSTRAINT CK_Alerts_ResolvedData CHECK (Status <> 'Resolved' OR (ResolvedAt IS NOT NULL AND ResolutionReason IS NOT NULL))
);

CREATE INDEX IX_Alerts_Status_RaisedAt ON maintenance.Alerts (Status, RaisedAt DESC) INCLUDE (VehicleId, RoutineId, Type);
CREATE INDEX IX_Alerts_VehicleId ON maintenance.Alerts (VehicleId);

CREATE TABLE maintenance.OdometerReadings (
    Id             bigint IDENTITY(1, 1)  NOT NULL CONSTRAINT PK_OdometerReadings PRIMARY KEY,
    VehicleId      uniqueidentifier       NOT NULL CONSTRAINT FK_OdometerReadings_MaintenancePlans
                                          REFERENCES maintenance.MaintenancePlans (VehicleId),
    Kilometers     bigint                 NOT NULL CONSTRAINT CK_OdometerReadings_Kilometers CHECK (Kilometers >= 0),
    ReadAt         datetimeoffset(0)      NOT NULL,
    ReceivedAt     datetimeoffset(0)      NOT NULL,
    Outcome        varchar(20)            NOT NULL CONSTRAINT CK_OdometerReadings_Outcome CHECK (Outcome IN ('Accepted', 'Ignored')),
    IgnoredReason  varchar(30)            NULL CONSTRAINT CK_OdometerReadings_IgnoredReason CHECK (IgnoredReason IN ('NotGreaterThanCurrent')),
    CONSTRAINT CK_OdometerReadings_IgnoredData CHECK (
        (Outcome = 'Ignored' AND IgnoredReason IS NOT NULL)
        OR (Outcome = 'Accepted' AND IgnoredReason IS NULL))
);

CREATE INDEX IX_OdometerReadings_VehicleId_ReceivedAt ON maintenance.OdometerReadings (VehicleId, ReceivedAt DESC);

/* ===================== serviceorders ===================== */

CREATE SEQUENCE serviceorders.ServiceOrderNumbers AS bigint START WITH 1 INCREMENT BY 1;

CREATE TABLE serviceorders.ApprovalPolicies (
    Id                     bigint IDENTITY(1, 1)  NOT NULL CONSTRAINT PK_ApprovalPolicies PRIMARY KEY,
    Currency               char(3)                NOT NULL,
    AutoApprovalMaxAmount  decimal(18, 2)         NOT NULL CONSTRAINT CK_ApprovalPolicies_Amount CHECK (AutoApprovalMaxAmount >= 0),
    EffectiveFrom          datetimeoffset(0)      NOT NULL,
    CreatedBy              nvarchar(150)          NOT NULL,
    CreatedAt              datetimeoffset(0)      NOT NULL
);

CREATE INDEX IX_ApprovalPolicies_Currency_EffectiveFrom ON serviceorders.ApprovalPolicies (Currency, EffectiveFrom DESC) INCLUDE (AutoApprovalMaxAmount);

CREATE TABLE serviceorders.ServiceOrders (
    Id                            uniqueidentifier   NOT NULL CONSTRAINT PK_ServiceOrders PRIMARY KEY,
    Number                        bigint             NOT NULL CONSTRAINT DF_ServiceOrders_Number
                                                     DEFAULT (NEXT VALUE FOR serviceorders.ServiceOrderNumbers)
                                                     CONSTRAINT UX_ServiceOrders_Number UNIQUE,
    Origin                        varchar(20)        NOT NULL CONSTRAINT CK_ServiceOrders_Origin CHECK (Origin IN ('Automatic', 'Manual')),
    VehicleId                     uniqueidentifier   NOT NULL,
    Plate                         varchar(10)        NOT NULL,
    RoutineId                     uniqueidentifier   NOT NULL,
    RoutineName                   nvarchar(100)      NOT NULL,
    CustomerId                    uniqueidentifier   NOT NULL,
    WorkshopId                    uniqueidentifier   NOT NULL,
    ResponsibleApproverId         uniqueidentifier   NULL,
    Status                        varchar(20)        NOT NULL CONSTRAINT CK_ServiceOrders_Status
                                                     CHECK (Status IN ('Created', 'Quoted', 'PendingApproval', 'Approved', 'OnSite', 'InProgress', 'OnHold', 'Completed', 'Cancelled')),
    StatusBeforeHold              varchar(20)        NULL CONSTRAINT CK_ServiceOrders_StatusBeforeHold
                                                     CHECK (StatusBeforeHold IN ('Created', 'PendingApproval', 'Approved', 'OnSite', 'InProgress')),
    ApprovalReason                varchar(30)        NULL CONSTRAINT CK_ServiceOrders_ApprovalReason
                                                     CHECK (ApprovalReason IN ('Quote', 'AdditionalServices')),
    ApprovalPriority              varchar(10)        NULL CONSTRAINT CK_ServiceOrders_ApprovalPriority
                                                     CHECK (ApprovalPriority IN ('Normal', 'High')),
    AppointmentAt                 datetimeoffset(0)  NULL,
    AppointmentNotes              nvarchar(250)      NULL,
    ArrivedOnSiteAt               datetimeoffset(0)  NULL,
    TotalAmount                   decimal(18, 2)     NULL,
    Currency                      char(3)            NULL,
    AutoApprovalMaxAmountApplied  decimal(18, 2)     NULL,
    AutoApproved                  bit                NOT NULL CONSTRAINT DF_ServiceOrders_AutoApproved DEFAULT (0),
    ApprovedByApproverId          uniqueidentifier   NULL,
    CancellationReason            varchar(30)        NULL CONSTRAINT CK_ServiceOrders_CancellationReason
                                                     CHECK (CancellationReason IN ('QuoteRejected', 'CancelledByCustomer')),
    ServicedKm                    bigint             NULL CONSTRAINT CK_ServiceOrders_ServicedKm CHECK (ServicedKm >= 0),
    ServicedAt                    date               NULL,
    CreatedAt                     datetimeoffset(0)  NOT NULL,
    UpdatedAt                     datetimeoffset(0)  NOT NULL,
    RowVersion                    rowversion         NOT NULL,
    CONSTRAINT CK_ServiceOrders_QuoteConsistency CHECK (
        (TotalAmount IS NULL AND Currency IS NULL AND AutoApprovalMaxAmountApplied IS NULL)
        OR (TotalAmount > 0 AND Currency IS NOT NULL AND AutoApprovalMaxAmountApplied IS NOT NULL)),
    CONSTRAINT CK_ServiceOrders_HoldData CHECK (
        (Status = 'OnHold' AND StatusBeforeHold IS NOT NULL)
        OR (Status <> 'OnHold' AND StatusBeforeHold IS NULL)),
    CONSTRAINT CK_ServiceOrders_ApprovalRequestData CHECK (
        (ApprovalReason IS NULL AND ApprovalPriority IS NULL)
        OR (ApprovalReason IS NOT NULL AND ApprovalPriority IS NOT NULL)),
    CONSTRAINT CK_ServiceOrders_PendingApprovalHasRequest CHECK (
        Status <> 'PendingApproval' OR ApprovalReason IS NOT NULL),
    CONSTRAINT CK_ServiceOrders_AppointmentRequired CHECK (
        Status NOT IN ('OnSite', 'InProgress', 'Completed') OR AppointmentAt IS NOT NULL),
    CONSTRAINT CK_ServiceOrders_CompletionData CHECK (
        Status <> 'Completed' OR (ServicedKm IS NOT NULL AND ServicedAt IS NOT NULL)),
    CONSTRAINT CK_ServiceOrders_CancellationData CHECK (
        Status <> 'Cancelled' OR CancellationReason IS NOT NULL)
);

CREATE UNIQUE INDEX UX_ServiceOrders_OneOpenPerRoutine
    ON serviceorders.ServiceOrders (VehicleId, RoutineId)
    WHERE Status IN ('Created', 'Quoted', 'PendingApproval', 'Approved', 'OnSite', 'InProgress', 'OnHold');

CREATE INDEX IX_ServiceOrders_ApprovalInbox
    ON serviceorders.ServiceOrders (CustomerId, ApprovalPriority DESC, UpdatedAt)
    INCLUDE (Number, VehicleId, ApprovalReason, TotalAmount)
    WHERE Status = 'PendingApproval';

CREATE INDEX IX_ServiceOrders_WorkshopId_Status ON serviceorders.ServiceOrders (WorkshopId, Status) INCLUDE (Number, VehicleId);
CREATE INDEX IX_ServiceOrders_CustomerId_Status ON serviceorders.ServiceOrders (CustomerId, Status) INCLUDE (Number, VehicleId);
CREATE INDEX IX_ServiceOrders_VehicleId_Status ON serviceorders.ServiceOrders (VehicleId, Status);

CREATE TABLE serviceorders.ServiceOrderLines (
    Id              uniqueidentifier   NOT NULL CONSTRAINT PK_ServiceOrderLines PRIMARY KEY,
    ServiceOrderId  uniqueidentifier   NOT NULL CONSTRAINT FK_ServiceOrderLines_ServiceOrders
                                       REFERENCES serviceorders.ServiceOrders (Id) ON DELETE CASCADE,
    LineType        varchar(20)        NOT NULL CONSTRAINT CK_ServiceOrderLines_LineType CHECK (LineType IN ('Routine', 'Additional')),
    Description     nvarchar(250)      NOT NULL,
    Amount          decimal(18, 2)     NULL CONSTRAINT CK_ServiceOrderLines_Amount CHECK (Amount > 0),
    PricingStatus   varchar(20)        NOT NULL CONSTRAINT CK_ServiceOrderLines_PricingStatus CHECK (PricingStatus IN ('Priced', 'PendingPricing')),
    RequestedBy     varchar(20)        NOT NULL CONSTRAINT CK_ServiceOrderLines_RequestedBy CHECK (RequestedBy IN ('Workshop', 'Approver')),
    ApprovalStatus  varchar(20)        NOT NULL CONSTRAINT CK_ServiceOrderLines_ApprovalStatus
                                       CHECK (ApprovalStatus IN ('Approved', 'PendingApproval', 'Rejected')),
    CreatedAt       datetimeoffset(0)  NOT NULL,
    CONSTRAINT CK_ServiceOrderLines_PricingConsistency CHECK (
        (PricingStatus = 'Priced' AND Amount IS NOT NULL)
        OR (PricingStatus = 'PendingPricing' AND Amount IS NULL))
);

CREATE INDEX IX_ServiceOrderLines_ServiceOrderId ON serviceorders.ServiceOrderLines (ServiceOrderId);

CREATE TABLE serviceorders.ServiceOrderHistory (
    Id              bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ServiceOrderHistory PRIMARY KEY,
    ServiceOrderId  uniqueidentifier      NOT NULL CONSTRAINT FK_ServiceOrderHistory_ServiceOrders
                                          REFERENCES serviceorders.ServiceOrders (Id) ON DELETE CASCADE,
    ChangeType      varchar(30)           NOT NULL CONSTRAINT CK_ServiceOrderHistory_ChangeType
                                          CHECK (ChangeType IN ('StatusChanged', 'AppointmentScheduled', 'AppointmentRescheduled', 'AdditionalServiceAdded')),
    FromStatus      varchar(20)           NULL,
    ToStatus        varchar(20)           NOT NULL,
    Detail          nvarchar(500)         NULL,
    ChangedBy       nvarchar(150)         NOT NULL,
    ChangedAt       datetimeoffset(0)     NOT NULL
);

CREATE INDEX IX_ServiceOrderHistory_ServiceOrderId ON serviceorders.ServiceOrderHistory (ServiceOrderId, ChangedAt);

CREATE TABLE serviceorders.VehicleReferences (
    VehicleId              uniqueidentifier   NOT NULL CONSTRAINT PK_ServiceOrders_VehicleReferences PRIMARY KEY,
    Plate                  varchar(10)        NOT NULL,
    CustomerId             uniqueidentifier   NOT NULL,
    WorkshopId             uniqueidentifier   NOT NULL,
    ResponsibleApproverId  uniqueidentifier   NULL,
    Status                 varchar(20)        NOT NULL,
    UpdatedAt              datetimeoffset(0)  NOT NULL
);

CREATE TABLE serviceorders.ApproverReferences (
    ApproverId  uniqueidentifier   NOT NULL CONSTRAINT PK_ServiceOrders_ApproverReferences PRIMARY KEY,
    CustomerId  uniqueidentifier   NOT NULL,
    FullName    nvarchar(150)      NOT NULL,
    Status      varchar(20)        NOT NULL,
    UpdatedAt   datetimeoffset(0)  NOT NULL
);

CREATE INDEX IX_ApproverReferences_CustomerId ON serviceorders.ApproverReferences (CustomerId);

CREATE TABLE serviceorders.RoutineReferences (
    RoutineId    uniqueidentifier   NOT NULL CONSTRAINT PK_ServiceOrders_RoutineReferences PRIMARY KEY,
    VehicleId    uniqueidentifier   NOT NULL,
    RoutineName  nvarchar(100)      NOT NULL,
    DueStatus    varchar(20)        NOT NULL,
    State        varchar(20)        NOT NULL,
    CycleNumber  int                NOT NULL,
    UpdatedAt    datetimeoffset(0)  NOT NULL
);

CREATE INDEX IX_RoutineReferences_VehicleId ON serviceorders.RoutineReferences (VehicleId);
GO

/* ===================== imports (ADR-0022) ===================== */

CREATE TABLE imports.ImportJobs (
    Id           uniqueidentifier   NOT NULL CONSTRAINT PK_ImportJobs PRIMARY KEY,
    Status       varchar(30)        NOT NULL CONSTRAINT CK_ImportJobs_Status
                                    CHECK (Status IN ('Validating', 'PendingConfirmation', 'Importing', 'Completed', 'CompletedWithErrors', 'Failed')),
    CreatedBy    nvarchar(150)      NOT NULL,
    CreatedAt    datetimeoffset(0)  NOT NULL,
    ConfirmedAt  datetimeoffset(0)  NULL,
    CompletedAt  datetimeoffset(0)  NULL,
    RowVersion   rowversion         NOT NULL
);

CREATE INDEX IX_ImportJobs_CreatedAt ON imports.ImportJobs (CreatedAt DESC);

CREATE TABLE imports.ImportStages (
    Id           uniqueidentifier   NOT NULL CONSTRAINT PK_ImportStages PRIMARY KEY,
    ImportJobId  uniqueidentifier   NOT NULL CONSTRAINT FK_ImportStages_ImportJobs
                                    REFERENCES imports.ImportJobs (Id) ON DELETE CASCADE,
    EntityType   varchar(20)        NOT NULL CONSTRAINT CK_ImportStages_EntityType
                                    CHECK (EntityType IN ('Workshops', 'Customers', 'Approvers', 'Vehicles', 'Routines')),
    StageOrder   tinyint            NOT NULL CONSTRAINT CK_ImportStages_StageOrder CHECK (StageOrder BETWEEN 1 AND 5),
    FileName     nvarchar(260)      NOT NULL,
    Delimiter    char(1)            NOT NULL CONSTRAINT CK_ImportStages_Delimiter CHECK (Delimiter IN (';', ',')),
    FileEncoding varchar(20)        NOT NULL CONSTRAINT CK_ImportStages_FileEncoding CHECK (FileEncoding IN ('Utf8', 'Utf8Bom', 'Windows1252')),
    Status       varchar(20)        NOT NULL CONSTRAINT CK_ImportStages_Status
                                    CHECK (Status IN ('Pending', 'Validated', 'Imported', 'Failed')),
    TotalRows    int                NOT NULL CONSTRAINT DF_ImportStages_TotalRows DEFAULT (0),
    ValidRows    int                NOT NULL CONSTRAINT DF_ImportStages_ValidRows DEFAULT (0),
    InvalidRows  int                NOT NULL CONSTRAINT DF_ImportStages_InvalidRows DEFAULT (0),
    SkippedRows  int                NOT NULL CONSTRAINT DF_ImportStages_SkippedRows DEFAULT (0),
    ImportedRows int                NOT NULL CONSTRAINT DF_ImportStages_ImportedRows DEFAULT (0),
    FailureReason nvarchar(500)     NULL,
    CONSTRAINT UX_ImportStages_Job_Entity UNIQUE (ImportJobId, EntityType)
);

CREATE TABLE imports.ImportRows (
    Id             bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ImportRows PRIMARY KEY,
    ImportStageId  uniqueidentifier      NOT NULL CONSTRAINT FK_ImportRows_ImportStages
                                         REFERENCES imports.ImportStages (Id) ON DELETE CASCADE,
    RowNumber      int                   NOT NULL,
    RawData        nvarchar(2000)        NOT NULL,
    Outcome        varchar(20)           NOT NULL CONSTRAINT CK_ImportRows_Outcome
                                         CHECK (Outcome IN ('Valid', 'Invalid', 'Skipped', 'Imported', 'Failed')),
    Errors         nvarchar(2000)        NULL,
    CONSTRAINT UX_ImportRows_Stage_RowNumber UNIQUE (ImportStageId, RowNumber),
    CONSTRAINT CK_ImportRows_ErrorsRequired CHECK (Outcome NOT IN ('Invalid', 'Failed') OR Errors IS NOT NULL)
);

CREATE INDEX IX_ImportRows_Stage_Outcome ON imports.ImportRows (ImportStageId, Outcome);

/* ===================== reporting (ADR-0023, proyecciones de lectura) ===================== */

CREATE TABLE reporting.Customers (
    CustomerId  uniqueidentifier   NOT NULL CONSTRAINT PK_Reporting_Customers PRIMARY KEY,
    Name        nvarchar(150)      NOT NULL,
    TaxId       varchar(20)        NOT NULL,
    Status      varchar(20)        NOT NULL,
    UpdatedAt   datetimeoffset(3)  NOT NULL,
    LastEventOffset  bigint        NOT NULL
);

CREATE TABLE reporting.Workshops (
    WorkshopId  uniqueidentifier   NOT NULL CONSTRAINT PK_Reporting_Workshops PRIMARY KEY,
    Name        nvarchar(150)      NOT NULL,
    City        nvarchar(100)      NOT NULL,
    Status      varchar(20)        NOT NULL,
    UpdatedAt   datetimeoffset(3)  NOT NULL,
    LastEventOffset  bigint        NOT NULL
);

CREATE TABLE reporting.Vehicles (
    VehicleId   uniqueidentifier   NOT NULL CONSTRAINT PK_Reporting_Vehicles PRIMARY KEY,
    Plate       varchar(10)        NOT NULL,
    CustomerId  uniqueidentifier   NOT NULL,
    WorkshopId  uniqueidentifier   NOT NULL,
    Status      varchar(20)        NOT NULL,
    OdometerKm  bigint             NOT NULL,
    UpdatedAt   datetimeoffset(3)  NOT NULL,
    LastEventOffset  bigint        NOT NULL
);

CREATE INDEX IX_Reporting_Vehicles_CustomerId ON reporting.Vehicles (CustomerId);

CREATE TABLE reporting.VehicleRoutineStatus (
    RoutineId        uniqueidentifier   NOT NULL CONSTRAINT PK_Reporting_VehicleRoutineStatus PRIMARY KEY,
    VehicleId        uniqueidentifier   NOT NULL,
    Plate            varchar(10)        NOT NULL,
    CustomerId       uniqueidentifier   NOT NULL,
    RoutineName      nvarchar(100)      NOT NULL,
    State            varchar(20)        NOT NULL,
    DueStatus        varchar(20)        NOT NULL,
    CycleNumber      int                NOT NULL,
    CycleStartKm     bigint             NOT NULL,
    CycleStartDate   date               NOT NULL,
    NextDueKm        bigint             NULL,
    NextDueDate      date               NULL,
    ConsumedPercent  decimal(6, 2)      NULL,
    LastEventOffset  bigint             NOT NULL,
    LastEventAt      datetimeoffset(3)  NOT NULL
);

CREATE INDEX IX_Reporting_VehicleRoutineStatus_VehicleId ON reporting.VehicleRoutineStatus (VehicleId);
CREATE INDEX IX_Reporting_VehicleRoutineStatus_DueStatus ON reporting.VehicleRoutineStatus (DueStatus) INCLUDE (VehicleId, NextDueKm, NextDueDate);

CREATE TABLE reporting.ServiceOrderFacts (
    ServiceOrderId      uniqueidentifier   NOT NULL CONSTRAINT PK_Reporting_ServiceOrderFacts PRIMARY KEY,
    Number              bigint             NOT NULL,
    VehicleId           uniqueidentifier   NOT NULL,
    Plate               varchar(10)        NOT NULL,
    CustomerId          uniqueidentifier   NOT NULL,
    WorkshopId          uniqueidentifier   NOT NULL,
    RoutineName         nvarchar(100)      NOT NULL,
    Origin              varchar(20)        NOT NULL,
    Status              varchar(20)        NOT NULL,
    RoutineAmount       decimal(18, 2)     NULL,
    AdditionalAmount    decimal(18, 2)     NULL,
    TotalAmount         decimal(18, 2)     NULL,
    Currency            char(3)            NULL,
    AutoApproved        bit                NOT NULL,
    CreatedAt           datetimeoffset(0)  NOT NULL,
    QuotedAt            datetimeoffset(0)  NULL,
    ApprovedAt          datetimeoffset(0)  NULL,
    AppointmentAt       datetimeoffset(0)  NULL,
    OnSiteAt            datetimeoffset(0)  NULL,
    StartedAt           datetimeoffset(0)  NULL,
    CompletedAt         datetimeoffset(0)  NULL,
    CancelledAt         datetimeoffset(0)  NULL,
    CancellationReason  varchar(30)        NULL,
    LastEventOffset     bigint             NOT NULL,
    LastEventAt         datetimeoffset(3)  NOT NULL
);

CREATE INDEX IX_Reporting_ServiceOrderFacts_CreatedAt ON reporting.ServiceOrderFacts (CreatedAt) INCLUDE (Status, TotalAmount);
CREATE INDEX IX_Reporting_ServiceOrderFacts_Customer ON reporting.ServiceOrderFacts (CustomerId, CreatedAt);
CREATE INDEX IX_Reporting_ServiceOrderFacts_Workshop ON reporting.ServiceOrderFacts (WorkshopId, CreatedAt);

CREATE TABLE reporting.AlertFacts (
    AlertId           uniqueidentifier   NOT NULL CONSTRAINT PK_Reporting_AlertFacts PRIMARY KEY,
    VehicleId         uniqueidentifier   NOT NULL,
    Plate             varchar(10)        NOT NULL,
    CustomerId        uniqueidentifier   NOT NULL,
    RoutineName       nvarchar(100)      NOT NULL,
    Type              varchar(20)        NOT NULL,
    TriggeredBy       varchar(20)        NOT NULL,
    Status            varchar(20)        NOT NULL,
    RaisedAt          datetimeoffset(0)  NOT NULL,
    AcknowledgedAt    datetimeoffset(0)  NULL,
    ResolvedAt        datetimeoffset(0)  NULL,
    ResolutionReason  varchar(30)        NULL,
    LastEventOffset   bigint             NOT NULL,
    LastEventAt       datetimeoffset(3)  NOT NULL
);

CREATE INDEX IX_Reporting_AlertFacts_Customer_RaisedAt ON reporting.AlertFacts (CustomerId, RaisedAt);

CREATE TABLE reporting.EventCheckpoints (
    SubscriberName  varchar(60)        NOT NULL CONSTRAINT PK_reporting_EventCheckpoints PRIMARY KEY,
    LastOffset      bigint             NOT NULL,
    UpdatedAt       datetimeoffset(3)  NOT NULL
);
GO

/* ===================== eventbus (ADR-0024, D-05: log de eventos) ===================== */

CREATE TABLE eventbus.Events (
    [Offset]    bigint IDENTITY(1, 1)  NOT NULL CONSTRAINT PK_Events PRIMARY KEY,
    MessageId   uniqueidentifier       NOT NULL CONSTRAINT UX_Events_MessageId UNIQUE,
    Stream      varchar(60)            NOT NULL,
    [Type]      varchar(150)           NOT NULL,
    Subject     varchar(60)            NOT NULL,
    OccurredAt  datetimeoffset(3)      NOT NULL,
    StoredAt    datetimeoffset(3)      NOT NULL,
    Payload     nvarchar(max)          NOT NULL
);

CREATE INDEX IX_Events_Stream_Offset ON eventbus.Events (Stream, [Offset]);

CREATE TABLE eventbus.DeadLetters (
    Id              bigint IDENTITY(1, 1)  NOT NULL CONSTRAINT PK_DeadLetters PRIMARY KEY,
    SubscriberName  varchar(60)            NOT NULL,
    [Offset]        bigint                 NOT NULL,
    Error           nvarchar(2000)         NOT NULL,
    ReportedAt      datetimeoffset(3)      NOT NULL
);
GO

/* ===================== Outbox / EventCheckpoints (uno por schema de negocio) ===================== */

DECLARE @schemas TABLE (Name sysname);
INSERT INTO @schemas (Name) VALUES (N'customers'), (N'workshops'), (N'fleet'), (N'maintenance'), (N'serviceorders');

DECLARE @schema sysname;
DECLARE @sql nvarchar(max);

DECLARE schema_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @schemas;
OPEN schema_cursor;
FETCH NEXT FROM schema_cursor INTO @schema;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'
CREATE TABLE ' + QUOTENAME(@schema) + N'.OutboxMessages (
    Id          uniqueidentifier   NOT NULL CONSTRAINT PK_' + @schema + N'_OutboxMessages PRIMARY KEY,
    Stream      varchar(60)        NOT NULL,
    EventType   varchar(150)       NOT NULL,
    Subject     varchar(60)        NOT NULL,
    Payload     nvarchar(max)      NOT NULL,
    OccurredAt  datetimeoffset(3)  NOT NULL,
    ProcessedAt datetimeoffset(3)  NULL,
    Attempts    int                NOT NULL CONSTRAINT DF_' + @schema + N'_OutboxMessages_Attempts DEFAULT (0),
    LastError   nvarchar(1000)     NULL
);
CREATE INDEX IX_OutboxMessages_Pending ON ' + QUOTENAME(@schema) + N'.OutboxMessages (OccurredAt) WHERE ProcessedAt IS NULL;

CREATE TABLE ' + QUOTENAME(@schema) + N'.EventCheckpoints (
    SubscriberName  varchar(60)        NOT NULL CONSTRAINT PK_' + @schema + N'_EventCheckpoints PRIMARY KEY,
    LastOffset      bigint             NOT NULL,
    UpdatedAt       datetimeoffset(3)  NOT NULL
);';
    EXEC sys.sp_executesql @sql;
    FETCH NEXT FROM schema_cursor INTO @schema;
END

CLOSE schema_cursor;
DEALLOCATE schema_cursor;
GO

/* ===================== Datos de referencia (parámetros iniciales) ===================== */

INSERT INTO maintenance.RoutineTypes (Id, Code, Name, DefaultIntervalKm, DefaultIntervalMonths, DueSoonThresholdPercent, Status, UpdatedBy, UpdatedAt)
VALUES
    (NEWID(), 'OIL_CHANGE', N'Cambio de aceite', 10000, 6,  10.00, 'Active', N'seed', SYSDATETIMEOFFSET()),
    (NEWID(), 'BRAKES',     N'Frenos',           20000, 12, 10.00, 'Active', N'seed', SYSDATETIMEOFFSET()),
    (NEWID(), 'TIRES',      N'Llantas',          40000, 24, 10.00, 'Active', N'seed', SYSDATETIMEOFFSET());

INSERT INTO serviceorders.ApprovalPolicies (Currency, AutoApprovalMaxAmount, EffectiveFrom, CreatedBy, CreatedAt)
VALUES ('COP', 1500000.00, SYSDATETIMEOFFSET(), N'seed', SYSDATETIMEOFFSET());
GO

/* ===================== Usuarios de mínimo privilegio por servicio ===================== */

USE master;
GO
IF SUSER_ID(N'rutaviva_customers_app') IS NULL     CREATE LOGIN rutaviva_customers_app     WITH PASSWORD = N'$(CustomersAppPassword)';
IF SUSER_ID(N'rutaviva_workshops_app') IS NULL     CREATE LOGIN rutaviva_workshops_app     WITH PASSWORD = N'$(WorkshopsAppPassword)';
IF SUSER_ID(N'rutaviva_fleet_app') IS NULL         CREATE LOGIN rutaviva_fleet_app         WITH PASSWORD = N'$(FleetAppPassword)';
IF SUSER_ID(N'rutaviva_maintenance_app') IS NULL   CREATE LOGIN rutaviva_maintenance_app   WITH PASSWORD = N'$(MaintenanceAppPassword)';
IF SUSER_ID(N'rutaviva_serviceorders_app') IS NULL CREATE LOGIN rutaviva_serviceorders_app WITH PASSWORD = N'$(ServiceOrdersAppPassword)';
IF SUSER_ID(N'rutaviva_imports_app') IS NULL       CREATE LOGIN rutaviva_imports_app       WITH PASSWORD = N'$(ImportsAppPassword)';
IF SUSER_ID(N'rutaviva_reporting_app') IS NULL     CREATE LOGIN rutaviva_reporting_app     WITH PASSWORD = N'$(ReportingAppPassword)';
IF SUSER_ID(N'rutaviva_eventbus_app') IS NULL      CREATE LOGIN rutaviva_eventbus_app      WITH PASSWORD = N'$(EventBusAppPassword)';
GO

USE RutaViva;
GO

DECLARE @services TABLE (SchemaName sysname NOT NULL, UserName sysname NOT NULL);
INSERT INTO @services (SchemaName, UserName) VALUES
    (N'customers',     N'rutaviva_customers_app'),
    (N'workshops',     N'rutaviva_workshops_app'),
    (N'fleet',         N'rutaviva_fleet_app'),
    (N'maintenance',   N'rutaviva_maintenance_app'),
    (N'serviceorders', N'rutaviva_serviceorders_app'),
    (N'imports',       N'rutaviva_imports_app'),
    (N'reporting',     N'rutaviva_reporting_app'),
    (N'eventbus',      N'rutaviva_eventbus_app');

DECLARE @ownerSchema sysname;
DECLARE @userName sysname;
DECLARE @statement nvarchar(max);

DECLARE service_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT SchemaName, UserName FROM @services;
OPEN service_cursor;
FETCH NEXT FROM service_cursor INTO @ownerSchema, @userName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF USER_ID(@userName) IS NULL
    BEGIN
        SET @statement = N'CREATE USER ' + QUOTENAME(@userName) + N' FOR LOGIN ' + QUOTENAME(@userName)
                       + N' WITH DEFAULT_SCHEMA = ' + QUOTENAME(@ownerSchema) + N';';
        EXEC sys.sp_executesql @statement;
    END

    SET @statement = N'GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::' + QUOTENAME(@ownerSchema) + N' TO ' + QUOTENAME(@userName) + N';';
    EXEC sys.sp_executesql @statement;

    SELECT @statement = STRING_AGG(CONVERT(nvarchar(max),
               N'DENY SELECT, INSERT, UPDATE, DELETE ON SCHEMA::' + QUOTENAME(SchemaName) + N' TO ' + QUOTENAME(@userName) + N';'),
               NCHAR(10))
    FROM @services
    WHERE SchemaName <> @ownerSchema;
    EXEC sys.sp_executesql @statement;

    FETCH NEXT FROM service_cursor INTO @ownerSchema, @userName;
END

CLOSE service_cursor;
DEALLOCATE service_cursor;

GRANT UPDATE ON OBJECT::serviceorders.ServiceOrderNumbers TO rutaviva_serviceorders_app;
GO