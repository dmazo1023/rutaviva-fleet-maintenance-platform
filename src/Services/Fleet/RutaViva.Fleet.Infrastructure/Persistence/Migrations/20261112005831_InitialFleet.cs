using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaViva.Fleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFleet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fleet");

            migrationBuilder.CreateTable(
                name: "ApproverReferences",
                schema: "fleet",
                columns: table => new
                {
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApproverReferences", x => x.ApproverId);
                    table.CheckConstraint("CK_ApproverReferences_Status", "[Status] IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "CustomerReferences",
                schema: "fleet",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReferences", x => x.CustomerId);
                    table.CheckConstraint("CK_CustomerReferences_Status", "[Status] IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "EventCheckpoints",
                schema: "fleet",
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
                name: "OutboxMessages",
                schema: "fleet",
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
                name: "Vehicles",
                schema: "fleet",
                columns: table => new
                {
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Plate = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    Vin = table.Column<string>(type: "varchar(17)", unicode: false, maxLength: 17, nullable: true),
                    InitialOdometerKm = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    StatusChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelYear = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleId);
                    table.CheckConstraint("CK_Vehicles_Status", "[Status] IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "WorkshopReferences",
                schema: "fleet",
                columns: table => new
                {
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopReferences", x => x.WorkshopId);
                    table.CheckConstraint("CK_WorkshopReferences_Status", "[Status] IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApproverReferences_Email",
                schema: "fleet",
                table: "ApproverReferences",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerReferences_TaxId",
                schema: "fleet",
                table: "CustomerReferences",
                column: "TaxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                schema: "fleet",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "FailedAt", "Sequence" },
                filter: "[ProcessedAt] IS NULL AND [FailedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CustomerId_Status",
                schema: "fleet",
                table: "Vehicles",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Vehicles_Plate",
                schema: "fleet",
                table: "Vehicles",
                column: "Plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Vehicles_Vin",
                schema: "fleet",
                table: "Vehicles",
                column: "Vin",
                unique: true,
                filter: "[Vin] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_WorkshopReferences_TaxId",
                schema: "fleet",
                table: "WorkshopReferences",
                column: "TaxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApproverReferences",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "CustomerReferences",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "EventCheckpoints",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "Vehicles",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "WorkshopReferences",
                schema: "fleet");
        }
    }
}
