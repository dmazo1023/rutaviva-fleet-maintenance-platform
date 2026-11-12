using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaViva.ServiceOrders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpenOrderPerRoutine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_ServiceOrders_OpenRoutine",
                schema: "serviceorders",
                table: "ServiceOrders",
                column: "RoutineId",
                unique: true,
                filter: "[Status] IN ('Created', 'Quoted', 'PendingApproval', 'Approved', 'InProgress')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ServiceOrders_OpenRoutine",
                schema: "serviceorders",
                table: "ServiceOrders");
        }
    }
}
