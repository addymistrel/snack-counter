using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackCounter.Server.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneActiveTicketPerContainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_tickets_active_container",
                table: "SnackTickets",
                column: "ContainerId",
                unique: true,
                filter: "\"Status\" IN ('Open', 'Acknowledged')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_tickets_active_container",
                table: "SnackTickets");
        }
    }
}
