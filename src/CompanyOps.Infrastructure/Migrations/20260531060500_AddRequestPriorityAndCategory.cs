using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestPriorityAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Medium");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "requests");
        }
    }
}
