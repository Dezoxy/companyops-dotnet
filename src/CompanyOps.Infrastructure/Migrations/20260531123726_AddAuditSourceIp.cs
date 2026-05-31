using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditSourceIp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceIp",
                table: "audit_logs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceIp",
                table: "audit_logs");
        }
    }
}
