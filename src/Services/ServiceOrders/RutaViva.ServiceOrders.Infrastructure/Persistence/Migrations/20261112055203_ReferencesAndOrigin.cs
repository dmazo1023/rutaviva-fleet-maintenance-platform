using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReferencesAndOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                schema: "serviceorders",
                table: "ServiceOrders",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleApproverId",
                schema: "serviceorders",
                table: "ServiceOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApproverReferences",
                schema: "serviceorders",
                columns: table => new
                {
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApproverReferences", x => x.ApproverId);
                });

            migrationBuilder.CreateTable(
                name: "RoutineReferences",
                schema: "serviceorders",
                columns: table => new
                {
                    RoutineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoutineTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineReferences", x => x.RoutineId);
                });

            migrationBuilder.CreateTable(
                name: "VehicleReferences",
                schema: "serviceorders",
                columns: table => new
                {
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Plate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleReferences", x => x.VehicleId);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopReferences",
                schema: "serviceorders",
                columns: table => new
                {
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopReferences", x => x.WorkshopId);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceOrders_Origin",
                schema: "serviceorders",
                table: "ServiceOrders",
                sql: "[Origin] IN ('Automatic', 'Manual')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApproverReferences",
                schema: "serviceorders");

            migrationBuilder.DropTable(
                name: "RoutineReferences",
                schema: "serviceorders");

            migrationBuilder.DropTable(
                name: "VehicleReferences",
                schema: "serviceorders");

            migrationBuilder.DropTable(
                name: "WorkshopReferences",
                schema: "serviceorders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceOrders_Origin",
                schema: "serviceorders",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "Origin",
                schema: "serviceorders",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "ResponsibleApproverId",
                schema: "serviceorders",
                table: "ServiceOrders");
        }
    }
}
