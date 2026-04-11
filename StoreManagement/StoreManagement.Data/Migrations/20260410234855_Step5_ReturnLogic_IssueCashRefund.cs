using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step5_ReturnLogic_IssueCashRefund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IssueCashRefund",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssueCashRefund",
                table: "Invoices");
        }
    }
}
