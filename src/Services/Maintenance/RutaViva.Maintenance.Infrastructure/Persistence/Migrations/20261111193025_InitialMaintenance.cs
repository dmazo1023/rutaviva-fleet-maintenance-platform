using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RutaViva.Maintenance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "maintenance");

            migrationBuilder.CreateTable(
                name: "EventCheckpoints",
                schema: "maintenance",
                columns: table => new
                {
                    SubscriberName = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    LastOffset = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCheckpoints", x => x.SubscriberName);
                });

            migrationBuilder.CreateTable(
                name: "MaintenancePlans",
                schema: "maintenance",
                columns: table => new
                {
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Plate = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LastTimeEvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    OdometerKm = table.Column<long>(type: "bigint", nullable: false),
                    OdometerReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePlans", x => x.VehicleId);
                    table.CheckConstraint("CK_MaintenancePlans_Status", "[Status] IN ('Active', 'Suspended')");
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Stream = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    EventType = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    Subject = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutineTypes",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultIntervalKm = table.Column<int>(type: "int", nullable: true),
                    DefaultIntervalMonths = table.Column<short>(type: "smallint", nullable: true),
                    DueSoonThresholdPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineTypes", x => x.Id);
                    table.CheckConstraint("CK_RoutineTypes_Status", "[Status] IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "OdometerReadings",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kilometers = table.Column<long>(type: "bigint", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    Outcome = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IgnoredReason = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdometerReadings", x => x.Id);
                    table.CheckConstraint("CK_OdometerReadings_IgnoredReason", "[IgnoredReason] IN ('NotGreaterThanCurrent', 'UnknownVehicle')");
                    table.CheckConstraint("CK_OdometerReadings_Kilometers", "[Kilometers] >= 0");
                    table.CheckConstraint("CK_OdometerReadings_Outcome", "[Outcome] IN ('Accepted', 'Ignored')");
                    table.ForeignKey(
                        name: "FK_OdometerReadings_MaintenancePlans_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "maintenance",
                        principalTable: "MaintenancePlans",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRoutines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutineTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutineTypeCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DueStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DueSoonNotified = table.Column<bool>(type: "bit", nullable: false),
                    LastTrigger = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    CycleStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CycleStartKm = table.Column<long>(type: "bigint", nullable: false),
                    DueSoonThresholdPercentOverride = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IntervalKm = table.Column<int>(type: "int", nullable: true),
                    IntervalMonths = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRoutines", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceRoutines_DueStatus", "[DueStatus] IN ('UpToDate', 'DueSoon', 'Overdue')");
                    table.CheckConstraint("CK_MaintenanceRoutines_LastTrigger", "[LastTrigger] IN ('Distance', 'Time', 'DistanceAndTime')");
                    table.CheckConstraint("CK_MaintenanceRoutines_State", "[State] IN ('Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_MaintenanceRoutines_MaintenancePlans_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "maintenance",
                        principalTable: "MaintenancePlans",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceRoutines_RoutineTypes_RoutineTypeId",
                        column: x => x.RoutineTypeId,
                        principalSchema: "maintenance",
                        principalTable: "RoutineTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Plate = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TriggeredBy = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ResolutionReason = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.CheckConstraint("CK_Alerts_ResolutionReason", "[ResolutionReason] IN ('RoutineRestarted', 'EscalatedToOverdue', 'ParametersChanged', 'RoutineDeactivated')");
                    table.CheckConstraint("CK_Alerts_Status", "[Status] IN ('Active', 'Acknowledged', 'Resolved')");
                    table.CheckConstraint("CK_Alerts_TriggeredBy", "[TriggeredBy] IN ('Distance', 'Time', 'DistanceAndTime')");
                    table.CheckConstraint("CK_Alerts_Type", "[Type] IN ('DueSoon', 'Overdue')");
                    table.ForeignKey(
                        name: "FK_Alerts_MaintenanceRoutines_RoutineId",
                        column: x => x.RoutineId,
                        principalSchema: "maintenance",
                        principalTable: "MaintenanceRoutines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "maintenance",
                table: "RoutineTypes",
                columns: new[] { "Id", "DefaultIntervalKm", "DefaultIntervalMonths", "Code", "DueSoonThresholdPercent", "Name", "Status", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("7f3c1a10-0001-4000-8000-000000000001"), 10000, (short)6, "OIL_CHANGE", 10m, "Cambio de aceite", "Active", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" },
                    { new Guid("7f3c1a10-0001-4000-8000-000000000002"), 20000, (short)12, "BRAKES", 10m, "Frenos", "Active", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" },
                    { new Guid("7f3c1a10-0001-4000-8000-000000000003"), 40000, (short)24, "TIRES", 10m, "Llantas", "Active", new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CustomerId_Status",
                schema: "maintenance",
                table: "Alerts",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Status_RaisedAt",
                schema: "maintenance",
                table: "Alerts",
                columns: new[] { "Status", "RaisedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_VehicleId",
                schema: "maintenance",
                table: "Alerts",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "UX_Alerts_OnePerCycleAndType",
                schema: "maintenance",
                table: "Alerts",
                columns: new[] { "RoutineId", "CycleNumber", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MaintenancePlans_Plate",
                schema: "maintenance",
                table: "MaintenancePlans",
                column: "Plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRoutines_RoutineTypeId",
                schema: "maintenance",
                table: "MaintenanceRoutines",
                column: "RoutineTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_MaintenanceRoutines_VehicleId_RoutineTypeId",
                schema: "maintenance",
                table: "MaintenanceRoutines",
                columns: new[] { "VehicleId", "RoutineTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdometerReadings_VehicleId_ReceivedAt",
                schema: "maintenance",
                table: "OdometerReadings",
                columns: new[] { "VehicleId", "ReceivedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                schema: "maintenance",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "FailedAt", "Sequence" },
                filter: "[ProcessedAt] IS NULL AND [FailedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_RoutineTypes_Code",
                schema: "maintenance",
                table: "RoutineTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "EventCheckpoints",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "OdometerReadings",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "MaintenanceRoutines",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "MaintenancePlans",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "RoutineTypes",
                schema: "maintenance");
        }
    }
}
