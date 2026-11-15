using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SnackCounter.Server.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProofAndReimbursementClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemsJson",
                table: "SnackTickets",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "ProofExceedsEstimate",
                table: "SnackTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ReimbursementClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    VolunteerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReimbursementClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReimbursementClaims_SnackTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SnackTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Offices",
                columns: new[] { "Id", "Currency", "MonthlyBudget", "Name" },
                values: new object[,]
                {
                    { "bengaluru", "INR", 15000m, "Bengaluru" },
                    { "oslo", "NOK", 6000m, "Oslo" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_ReferenceId",
                table: "ReimbursementClaims",
                column: "ReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_TicketId",
                table: "ReimbursementClaims",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReimbursementClaims");

            migrationBuilder.DeleteData(
                table: "Offices",
                keyColumn: "Id",
                keyValue: "bengaluru");

            migrationBuilder.DeleteData(
                table: "Offices",
                keyColumn: "Id",
                keyValue: "oslo");

            migrationBuilder.DropColumn(
                name: "ItemsJson",
                table: "SnackTickets");

            migrationBuilder.DropColumn(
                name: "ProofExceedsEstimate",
                table: "SnackTickets");
        }
    }
}
