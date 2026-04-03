using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة الفواتير - Business Logic في IInvoiceService
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] InvoiceType? type,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _invoiceService.GetAllAsync(query, type, fromDate, toDate);
        return Ok(ApiResponse<PagedResult<InvoiceReadDto>>.SuccessResult(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InvoiceReadDto>>> GetById(int id)
    {
        var result = await _invoiceService.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<InvoiceReadDto>.Failure("الفاتورة غير موجودة"));
        return Ok(ApiResponse<InvoiceReadDto>.SuccessResult(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant,Sales")]
    public async Task<ActionResult<ApiResponse<InvoiceReadDto>>> Create([FromBody] CreateInvoiceDto dto)
    {
        var invoice = await _invoiceService.CreateAsync(dto);
        return Ok(ApiResponse<InvoiceReadDto>.SuccessResult(invoice, "تم إنشاء الفاتورة بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _invoiceService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResult(null, "تم حذف الفاتورة بنجاح"));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "admin,accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(int id)
    {
        await _invoiceService.CancelAsync(id);
        return Ok(ApiResponse<object>.SuccessResult(null, "تم إلغاء الفاتورة بنجاح وتم فك التخصيص واسترجاع الكميات."));
    }
}
