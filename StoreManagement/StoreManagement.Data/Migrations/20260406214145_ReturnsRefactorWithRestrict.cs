using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReturnsRefactorWithRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNotes",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReturnMode",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalInvoiceItemId",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems",
                column: "OriginalInvoiceItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems",
                column: "OriginalInvoiceItemId",
                principalTable: "InvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ApprovalNotes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReturnMode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceItemId",
                table: "InvoiceItems");
        }
    }
}
