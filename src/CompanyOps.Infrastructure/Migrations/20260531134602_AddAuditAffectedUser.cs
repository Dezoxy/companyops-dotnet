using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAffectedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AffectedUserId",
                table: "audit_logs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffectedUserId",
                table: "audit_logs");
        }
    }
}
