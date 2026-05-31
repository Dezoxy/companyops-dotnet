using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestFulfilledAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FulfilledAssetId",
                table: "requests",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FulfilledAssetId",
                table: "requests");
        }
    }
}
