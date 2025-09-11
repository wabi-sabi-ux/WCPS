using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WCPS.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "AspNetUsers");
        }
    }
}
