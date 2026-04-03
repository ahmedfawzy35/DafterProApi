using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;
using Asp.Versioning;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    // ==========================================
    // Customer Receipts
    // ==========================================

    [HttpPost("customer-receipts")]
    [Authorize]
    public async Task<IActionResult> CreateCustomerReceipt([FromBody] CreateReceiptDto dto)
    {
        var result = await _financeService.CreateCustomerReceiptAsync(dto);
        return Created("", result);
    }

    [HttpPost("customer-receipts/allocate")]
    [Authorize]
    public async Task<IActionResult> AllocateCustomerReceipt([FromBody] AllocateReceiptDto dto)
    {
        await _financeService.AllocateCustomerReceiptAsync(dto);
        return Ok(new { message = "تم تخصيص السند بنجاح" });
    }

    // ==========================================
    // Supplier Payments
    // ==========================================

    [HttpPost("supplier-payments")]
    [Authorize]
    public async Task<IActionResult> CreateSupplierPayment([FromBody] CreateReceiptDto dto)
    {
        var result = await _financeService.CreateSupplierPaymentAsync(dto);
        return Created("", result);
    }

    [HttpPost("supplier-payments/allocate")]
    [Authorize]
    public async Task<IActionResult> AllocateSupplierPayment([FromBody] AllocateReceiptDto dto)
    {
        await _financeService.AllocateSupplierPaymentAsync(dto);
        return Ok(new { message = "تم تخصيص سند المورد بنجاح" });
    }

    // ==========================================
    // Reports & Statements
    // ==========================================

    // ملاحظة: هذا الـ endpoint مُكرَّر في CustomersController (/customers/{id}/statement)
    // يُحتفظ به هنا للتوافق الخلفي فقط
    [HttpGet("customers/{customerId}/statement")]
    [Authorize]
    public async Task<IActionResult> GetCustomerStatement(
        int customerId,
        [FromQuery] StatementQueryDto query)
    {
        var result = await _financeService.GetCustomerStatementAsync(customerId, query);
        return Ok(result);
    }

    [HttpGet("customers/{customerId}/open-invoices")]
    [Authorize]
    public async Task<IActionResult> GetOpenCustomerInvoices(int customerId)
    {
        var result = await _financeService.GetOpenCustomerInvoicesAsync(customerId);
        return Ok(result);
    }

    [HttpGet("customers/{customerId}/unallocated-receipts")]
    [Authorize]
    public async Task<IActionResult> GetUnallocatedCustomerReceipts(int customerId)
    {
        var result = await _financeService.GetUnallocatedCustomerReceiptsAsync(customerId);
        return Ok(result);
    }
}
