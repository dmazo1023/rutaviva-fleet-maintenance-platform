using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaViva.Workshops.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkshops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workshops");

            migrationBuilder.CreateTable(
                name: "EventCheckpoints",
                schema: "workshops",
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
                schema: "workshops",
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
                name: "Workshops",
                schema: "workshops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workshops", x => x.Id);
                    table.CheckConstraint("CK_Workshops_Status", "[Status] IN ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                schema: "workshops",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "FailedAt", "Sequence" },
                filter: "[ProcessedAt] IS NULL AND [FailedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Workshops_TaxId",
                schema: "workshops",
                table: "Workshops",
                column: "TaxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventCheckpoints",
                schema: "workshops");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "workshops");

            migrationBuilder.DropTable(
                name: "Workshops",
                schema: "workshops");
        }
    }
}
