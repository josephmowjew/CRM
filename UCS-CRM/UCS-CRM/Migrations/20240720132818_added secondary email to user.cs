using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UCS_CRM.Migrations
{
    /// <inheritdoc />
    public partial class addedsecondaryemailtouser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecondaryEmail",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondaryEmail",
                table: "Users");
        }
    }
}
