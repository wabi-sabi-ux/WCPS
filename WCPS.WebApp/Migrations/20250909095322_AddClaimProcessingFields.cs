using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WCPS.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimProcessingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "ClaimRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedById",
                table: "ClaimRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedById",
                table: "ClaimRequests");
        }
    }
}
