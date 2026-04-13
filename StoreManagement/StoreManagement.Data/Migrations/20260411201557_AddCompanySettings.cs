using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Suppliers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "StockTransfers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockTransferItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "StockTransferItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "StockTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockAdjustments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "StockAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockAdjustmentItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "StockAdjustmentItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SalaryAdjustments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "SalaryAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RecurringAdjustments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "RecurringAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ReconciliationFindings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "ReconciliationFindings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProductCostHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "ProductCostHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProductCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "ProductCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PayrollRuns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "PayrollRuns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PayrollRunItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "PayrollRunItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LoanInstallments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "LoanInstallments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmployeeSalaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "EmployeeSalaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmployeeLoans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "EmployeeLoans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmployeeActions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "EmployeeActions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CustomerReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "CustomerReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CompanyPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "CompanyPolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CashTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CashRegisterShifts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "CashRegisterShifts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AccountSettlements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "AccountSettlements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingsVersion = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    HasBranches = table.Column<bool>(type: "bit", nullable: false),
                    DefaultBranchId = table.Column<int>(type: "int", nullable: true),
                    MultiUserMode = table.Column<bool>(type: "bit", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    EnableTaxes = table.Column<bool>(type: "bit", nullable: false),
                    PricesIncludeTax = table.Column<bool>(type: "bit", nullable: false),
                    RequireNotesOnManualActions = table.Column<bool>(type: "bit", nullable: false),
                    EnableSales = table.Column<bool>(type: "bit", nullable: false),
                    AllowCashSales = table.Column<bool>(type: "bit", nullable: false),
                    AllowCreditSales = table.Column<bool>(type: "bit", nullable: false),
                    AllowInstallmentSales = table.Column<bool>(type: "bit", nullable: false),
                    RequireCustomerOnSale = table.Column<bool>(type: "bit", nullable: false),
                    AllowAnonymousCustomer = table.Column<bool>(type: "bit", nullable: false),
                    AllowPriceOverride = table.Column<bool>(type: "bit", nullable: false),
                    AllowDiscount = table.Column<bool>(type: "bit", nullable: false),
                    MaxDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AllowBelowCostSale = table.Column<bool>(type: "bit", nullable: false),
                    AutoReserveStockOnDraft = table.Column<bool>(type: "bit", nullable: false),
                    EnablePurchases = table.Column<bool>(type: "bit", nullable: false),
                    AllowCreditPurchases = table.Column<bool>(type: "bit", nullable: false),
                    RequireSupplierOnPurchase = table.Column<bool>(type: "bit", nullable: false),
                    AllowEditCostAfterPosting = table.Column<bool>(type: "bit", nullable: false),
                    EnablePurchaseReturns = table.Column<bool>(type: "bit", nullable: false),
                    EnableInventory = table.Column<bool>(type: "bit", nullable: false),
                    TrackStockByBranch = table.Column<bool>(type: "bit", nullable: false),
                    AllowNegativeStock = table.Column<bool>(type: "bit", nullable: false),
                    EnableStockTransfers = table.Column<bool>(type: "bit", nullable: false),
                    EnableStockAdjustments = table.Column<bool>(type: "bit", nullable: false),
                    EnableBatchOrExpiryTracking = table.Column<bool>(type: "bit", nullable: false),
                    EnableBarcode = table.Column<bool>(type: "bit", nullable: false),
                    AutoGenerateSku = table.Column<bool>(type: "bit", nullable: false),
                    EnableReturns = table.Column<bool>(type: "bit", nullable: false),
                    ReturnMode = table.Column<int>(type: "int", nullable: false),
                    RequireReferenceInvoice = table.Column<bool>(type: "bit", nullable: false),
                    AllowCashRefund = table.Column<bool>(type: "bit", nullable: false),
                    AllowStoreCreditRefund = table.Column<bool>(type: "bit", nullable: false),
                    AllowExchange = table.Column<bool>(type: "bit", nullable: false),
                    MaxReturnDays = table.Column<int>(type: "int", nullable: false),
                    RequireApprovalForReturns = table.Column<bool>(type: "bit", nullable: false),
                    EnableInstallments = table.Column<bool>(type: "bit", nullable: false),
                    AllowPartialInstallmentPayment = table.Column<bool>(type: "bit", nullable: false),
                    AllowEarlySettlement = table.Column<bool>(type: "bit", nullable: false),
                    AllowReschedule = table.Column<bool>(type: "bit", nullable: false),
                    ApplyLateFees = table.Column<bool>(type: "bit", nullable: false),
                    DefaultLateFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultInstallmentCount = table.Column<int>(type: "int", nullable: false),
                    DefaultFrequency = table.Column<int>(type: "int", nullable: false),
                    RequireApprovalForReschedule = table.Column<bool>(type: "bit", nullable: false),
                    EnableApprovals = table.Column<bool>(type: "bit", nullable: false),
                    RequireApprovalForHighDiscount = table.Column<bool>(type: "bit", nullable: false),
                    DiscountApprovalThresholdPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequireApprovalForExpense = table.Column<bool>(type: "bit", nullable: false),
                    ExpenseApprovalThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequireApprovalForStockAdjustment = table.Column<bool>(type: "bit", nullable: false),
                    RequireApprovalForCreditOverLimit = table.Column<bool>(type: "bit", nullable: false),
                    EnableTreasury = table.Column<bool>(type: "bit", nullable: false),
                    EnableExpenses = table.Column<bool>(type: "bit", nullable: false),
                    EnableRevenues = table.Column<bool>(type: "bit", nullable: false),
                    AllowMultipleCashboxes = table.Column<bool>(type: "bit", nullable: false),
                    EnableShiftTracking = table.Column<bool>(type: "bit", nullable: false),
                    RequireShiftOpenBeforeSale = table.Column<bool>(type: "bit", nullable: false),
                    EnableEmployees = table.Column<bool>(type: "bit", nullable: false),
                    EnableAttendance = table.Column<bool>(type: "bit", nullable: false),
                    EnablePayroll = table.Column<bool>(type: "bit", nullable: false),
                    EnableAdvances = table.Column<bool>(type: "bit", nullable: false),
                    ShowAdvancedMenus = table.Column<bool>(type: "bit", nullable: false),
                    UseSimpleDashboard = table.Column<bool>(type: "bit", nullable: false),
                    EnableQuickSaleScreen = table.Column<bool>(type: "bit", nullable: false),
                    EnableKeyboardShortcuts = table.Column<bool>(type: "bit", nullable: false),
                    EnableTouchMode = table.Column<bool>(type: "bit", nullable: false),
                    ShowCostPriceToAuthorizedUsersOnly = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true),
                    EditCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_CompanyId",
                table: "CompanySettings",
                column: "CompanyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockAdjustmentItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockAdjustmentItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SalaryAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SalaryAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RecurringAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RecurringAdjustments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ReconciliationFindings");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ReconciliationFindings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductCostHistories");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ProductCostHistories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PayrollRunItems");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PayrollRunItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LoanInstallments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "LoanInstallments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmployeeSalaries");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmployeeSalaries");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmployeeLoans");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmployeeLoans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmployeeActions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmployeeActions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyPolicies");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CompanyPolicies");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CashRegisterShifts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CashRegisterShifts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AccountSettlements");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AccountSettlements");
        }
    }
}
