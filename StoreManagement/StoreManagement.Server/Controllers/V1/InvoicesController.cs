using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Constants;
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
    private readonly IAuditLogService _auditLogService;
    private readonly IReturnService _returnService;
    private readonly ISalesPolicyService _salesPolicy;

    public InvoicesController(
        IInvoiceService invoiceService, 
        IAuditLogService auditLogService, 
        IReturnService returnService,
        ISalesPolicyService salesPolicy)
    {
        _invoiceService = invoiceService;
        _auditLogService = auditLogService;
        _returnService = returnService;
        _salesPolicy = salesPolicy;
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
    [Authorize(Policy = Permissions.Sales.Create)]
    public async Task<ActionResult<ApiResponse<InvoiceReadDto>>> Create([FromBody] CreateInvoiceDto dto)
    {
        // 1. التحقق من السياسة (Policy Validation)
        await _salesPolicy.EnsureCanSellAsync(dto.Items.Sum(x => (decimal)x.Quantity * x.UnitPrice));
        
        // 2. التنفيذ (Execution)
        var invoice = await _invoiceService.CreateAsync(dto);
        return Ok(ApiResponse<InvoiceReadDto>.SuccessResult(invoice, "تم إنشاء الفاتورة بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Sales.Delete)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _invoiceService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResult(null, "تم حذف الفاتورة بنجاح"));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = Permissions.Sales.Delete)] // أو إضافة Permissions.Sales.Cancel لاحقاً
    public async Task<ActionResult<ApiResponse<object>>> Cancel(int id)
    {
        await _invoiceService.CancelAsync(id);
        return Ok(ApiResponse<object>.SuccessResult(null, "تم إلغاء الفاتورة بنجاح وتم فك التخصيص واسترجاع الكميات."));
    }

    /// <summary>
    /// GET /api/v1/invoices/{id}/history
    /// جلب سجل التاريخ (Audit Logs) للفاتورة
    /// </summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogReadDto>>>> GetHistory(
        int id, [FromQuery] PaginationQueryDto query)
    {
        var result = await _auditLogService.GetAllAsync(query, entityName: "Invoice", entityId: id.ToString());
        return Ok(ApiResponse<PagedResult<AuditLogReadDto>>.SuccessResult(result));
    }

    [HttpPost("{invoiceId:int}/approve")]
    [Authorize(Policy = Permissions.Returns.Approve)]
    public async Task<ActionResult<ApiResponse<InvoiceReadDto>>> ApproveReturn(int invoiceId, [FromBody] ApproveReturnDto dto)
    {
        var result = await _returnService.ApproveManualReturnAsync(invoiceId, dto.Notes);
        return Ok(ApiResponse<InvoiceReadDto>.SuccessResult(result, "تم اعتماد المرتجع الشاذ بنجاح."));
    }

    [HttpPost("{invoiceId:int}/reject")]
    [Authorize(Policy = Permissions.Returns.Approve)]
    public async Task<ActionResult<ApiResponse<object>>> RejectReturn(int invoiceId, [FromBody] RejectReturnDto dto)
    {
        await _returnService.RejectManualReturnAsync(invoiceId, dto.Reason);
        return Ok(ApiResponse<object>.SuccessResult(null, "تم رفض المرتجع الشاذ بنجاح."));
    }

    [HttpGet("search-for-return")]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceReadDto>>>> SearchForReturn(
        [FromQuery] int? customerId, 
        [FromQuery] int? supplierId, 
        [FromQuery] int? productId, 
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to, 
        [FromQuery] PaginationQueryDto query)
    {
        var result = await _returnService.SearchForOriginalInvoiceAsync(customerId, supplierId, productId, from, to, query);
        return Ok(ApiResponse<PagedResult<InvoiceReadDto>>.SuccessResult(result));
    }
}
