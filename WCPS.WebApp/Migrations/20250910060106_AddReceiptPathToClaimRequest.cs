using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WCPS.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptPathToClaimRequest : Migration
    {   
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptPath",
                table: "ClaimRequests",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptPath",
                table: "ClaimRequests");
        }
    }
}
