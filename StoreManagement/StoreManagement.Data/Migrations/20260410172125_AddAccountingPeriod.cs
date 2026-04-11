using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialSourceId",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialSourceType",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialStatus",
                table: "SupplierPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "SupplierPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversalOfId",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "CustomerReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "CustomerReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialSourceId",
                table: "CustomerReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialSourceType",
                table: "CustomerReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialStatus",
                table: "CustomerReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "CustomerReceipts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversalOfId",
                table: "CustomerReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "CashTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialSourceId",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialSourceType",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialStatus",
                table: "CashTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "CashTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversalOfId",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_CompanyId_StartDate_EndDate",
                table: "AccountingPeriods",
                columns: new[] { "CompanyId", "StartDate", "EndDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "FinancialSourceId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "FinancialSourceType",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "FinancialStatus",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ReversalOfId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "FinancialSourceId",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "FinancialSourceType",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "FinancialStatus",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "ReversalOfId",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "FinancialSourceId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "FinancialSourceType",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "FinancialStatus",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "ReversalOfId",
                table: "CashTransactions");
        }
    }
}
