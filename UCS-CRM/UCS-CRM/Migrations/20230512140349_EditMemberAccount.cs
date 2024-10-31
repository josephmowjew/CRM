using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCS_CRM.Migrations
{
    /// <inheritdoc />
    public partial class EditMemberAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberAccounts_AccountTypes_AccountTypeId",
                table: "MemberAccounts");

            migrationBuilder.DropIndex(
                name: "IX_MemberAccounts_AccountTypeId",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "AccountTypeId",
                table: "MemberAccounts");

            migrationBuilder.RenameColumn(
                name: "AccountBalance",
                table: "MemberAccounts",
                newName: "Balance");

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "MemberAccounts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "");

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "MemberAccounts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "MemberAccounts");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "MemberAccounts");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "MemberAccounts",
                newName: "AccountBalance");

            migrationBuilder.AddColumn<int>(
                name: "AccountTypeId",
                table: "MemberAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MemberAccounts_AccountTypeId",
                table: "MemberAccounts",
                column: "AccountTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberAccounts_AccountTypes_AccountTypeId",
                table: "MemberAccounts",
                column: "AccountTypeId",
                principalTable: "AccountTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
