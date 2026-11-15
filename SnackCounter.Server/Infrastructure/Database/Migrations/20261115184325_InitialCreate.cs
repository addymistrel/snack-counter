using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackCounter.Server.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MonthlyBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnackTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OfficeId = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ReservedBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VolunteerId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcknowledgementDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnackTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnackTickets_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SnackTickets_ContainerId_Status",
                table: "SnackTickets",
                columns: new[] { "ContainerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SnackTickets_OfficeId",
                table: "SnackTickets",
                column: "OfficeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SnackTickets");

            migrationBuilder.DropTable(
                name: "Offices");
        }
    }
}
