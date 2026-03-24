using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiBranchArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Allowances",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Deductions",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "StockTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SalaryAdjustments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "RecurringAdjustments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "PayrollRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentBranchId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "EmployeeLoans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "EmployeeActions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "CashTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AccountSettlements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CurrentBranchId",
                table: "Employees",
                column: "CurrentBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Branches_CurrentBranchId",
                table: "Employees",
                column: "CurrentBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Branches_CurrentBranchId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CurrentBranchId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SalaryAdjustments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "RecurringAdjustments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CurrentBranchId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "EmployeeLoans");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "EmployeeActions");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AccountSettlements");

            migrationBuilder.AddColumn<decimal>(
                name: "Allowances",
                table: "Employees",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Deductions",
                table: "Employees",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
