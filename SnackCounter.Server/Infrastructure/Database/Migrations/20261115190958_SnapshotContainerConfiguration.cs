using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackCounter.Server.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotContainerConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "SnackTickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SnackName",
                table: "SnackTickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SnackTickets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "SnackTickets");

            migrationBuilder.DropColumn(
                name: "SnackName",
                table: "SnackTickets");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SnackTickets");
        }
    }
}
