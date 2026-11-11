using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaViva.EventBus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEventBus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "eventbus");

            migrationBuilder.CreateTable(
                name: "DeadLetters",
                schema: "eventbus",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriberName = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Offset = table.Column<long>(type: "bigint", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "eventbus",
                columns: table => new
                {
                    Offset = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stream = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Type = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    Subject = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    StoredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Offset);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Stream_Offset",
                schema: "eventbus",
                table: "Events",
                columns: new[] { "Stream", "Offset" });

            migrationBuilder.CreateIndex(
                name: "UX_Events_MessageId",
                schema: "eventbus",
                table: "Events",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeadLetters",
                schema: "eventbus");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "eventbus");
        }
    }
}
