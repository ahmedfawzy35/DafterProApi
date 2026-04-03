using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCashRegisterShiftAndEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "SupplierPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "CustomerReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "CashTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Reason",
                table: "AccountSettlements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CashRegisterShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCashIn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCashOut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualClosingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    EditCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisterShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashRegisterShifts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_ShiftId",
                table: "CashTransactions",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterShifts_UserId",
                table: "CashRegisterShifts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashTransactions_CashRegisterShifts_ShiftId",
                table: "CashTransactions",
                column: "ShiftId",
                principalTable: "CashRegisterShifts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashTransactions_CashRegisterShifts_ShiftId",
                table: "CashTransactions");

            migrationBuilder.DropTable(
                name: "CashRegisterShifts");

            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_ShiftId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "CustomerReceipts");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AccountSettlements");
        }
    }
}
