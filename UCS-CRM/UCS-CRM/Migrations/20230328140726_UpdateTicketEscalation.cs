using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCS_CRM.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketEscalation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Resolved",
                table: "TicketEscalations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecondEscalationReason",
                table: "TicketEscalations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resolved",
                table: "TicketEscalations");

            migrationBuilder.DropColumn(
                name: "SecondEscalationReason",
                table: "TicketEscalations");
        }
    }
}
