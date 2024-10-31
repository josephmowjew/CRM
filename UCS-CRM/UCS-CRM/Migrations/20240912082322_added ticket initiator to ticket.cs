using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCS_CRM.Migrations
{
    /// <inheritdoc />
    public partial class addedticketinitiatortoticket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InitiatorMemberId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitiatorUserId",
                table: "Tickets",
                type: "varchar(200)",
                nullable: true)
                .Annotation("MySql:CharSet", "");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_InitiatorMemberId",
                table: "Tickets",
                column: "InitiatorMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_InitiatorUserId",
                table: "Tickets",
                column: "InitiatorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Members_InitiatorMemberId",
                table: "Tickets",
                column: "InitiatorMemberId",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_InitiatorUserId",
                table: "Tickets",
                column: "InitiatorUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Members_InitiatorMemberId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_InitiatorUserId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_InitiatorMemberId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_InitiatorUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "InitiatorMemberId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "InitiatorUserId",
                table: "Tickets");
        }
    }
}
