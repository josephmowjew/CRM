using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCS_CRM.Migrations
{
    /// <inheritdoc />
    public partial class addedbranchandemployertomember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Branch",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "");

            migrationBuilder.AddColumn<string>(
                name: "Employer",
                table: "Members",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Branch",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Employer",
                table: "Members");
        }
    }
}
