using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCS_CRM.Migrations
{
    /// <inheritdoc />
    public partial class removedEscalationLevelToTicketEscalations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalationLevel",
                table: "TicketEscalations");

            migrationBuilder.DropColumn(
                name: "SecondEscalationReason",
                table: "TicketEscalations");


            migrationBuilder.AddColumn<string>(
                name: "EscalatedToId",
                table: "TicketEscalations",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEscalations_EscalatedToId",
                table: "TicketEscalations",
                column: "EscalatedToId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketEscalations_Users_EscalatedToId",
                table: "TicketEscalations",
                column: "EscalatedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketEscalations_Users_EscalatedToId",
                table: "TicketEscalations");

            migrationBuilder.DropIndex(
                name: "IX_TicketEscalations_EscalatedToId",
                table: "TicketEscalations");



            migrationBuilder.DropColumn(
                name: "EscalatedToId",
                table: "TicketEscalations");

            migrationBuilder.AddColumn<int>(
                name: "EscalationLevel",
                table: "TicketEscalations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SecondEscalationReason",
                table: "TicketEscalations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
