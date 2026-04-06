using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم التقارير المتقدمة (أعمار ديون، أرباح، ملخص مبيعات)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// تقرير أعمار ديون العملاء
    /// </summary>
    [HttpGet("aging/customers")]
    public async Task<ActionResult<ApiResponse<List<AgingReportRowDto>>>> GetCustomerAging([FromQuery] bool excludeZero = true)
    {
        var report = await _reportService.GetCustomerAgingReportAsync(excludeZeroBalances: excludeZero);
        return Ok(ApiResponse<List<AgingReportRowDto>>.SuccessResult(report));
    }

    /// <summary>
    /// تقرير أعمار ديون الموردين
    /// </summary>
    [HttpGet("aging/suppliers")]
    public async Task<ActionResult<ApiResponse<List<AgingReportRowDto>>>> GetSupplierAging([FromQuery] bool excludeZero = true)
    {
        var report = await _reportService.GetSupplierAgingReportAsync(excludeZeroBalances: excludeZero);
        return Ok(ApiResponse<List<AgingReportRowDto>>.SuccessResult(report));
    }

    /// <summary>
    /// ملخص المبيعات والأرباح
    /// </summary>
    [HttpGet("sales-summary")]
    public async Task<ActionResult<ApiResponse<SalesSummaryDto>>> GetSalesSummary(
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to)
    {
        var summary = await _reportService.GetSalesSummaryAsync(from, to);
        return Ok(ApiResponse<SalesSummaryDto>.SuccessResult(summary));
    }

    /// <summary>
    /// أرباح الفواتير مفصلة
    /// </summary>
    [HttpGet("invoices-profitability")]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceProfitDto>>>> GetInvoiceProfitability(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to)
    {
        var pagedResult = await _reportService.GetInvoiceProfitabilityAsync(query, from, to);
        return Ok(ApiResponse<PagedResult<InvoiceProfitDto>>.SuccessResult(pagedResult));
    }
    /// <summary>
    /// تقرير أرصدة المنتجات لكل فرع
    /// </summary>
    [HttpGet("stock-per-branch")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockPerBranchReportDto>>>> GetStockPerBranch(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] int? branchId,
        [FromQuery] int? productId)
    {
        var result = await _reportService.GetStockPerBranchReportAsync(query, branchId, productId);
        return Ok(ApiResponse<PagedResult<StockPerBranchReportDto>>.SuccessResult(result));
    }

    /// <summary>
    /// تقرير حركات المخزون للتدقيق
    /// </summary>
    [HttpGet("inventory-movements")]
    public async Task<ActionResult<ApiResponse<PagedResult<BranchInventoryMovementReportDto>>>> GetInventoryMovements(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] int? branchId,
        [FromQuery] int? productId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _reportService.GetBranchInventoryMovementsReportAsync(query, branchId, productId, from, to);
        return Ok(ApiResponse<PagedResult<BranchInventoryMovementReportDto>>.SuccessResult(result));
    }

    /// <summary>
    /// تقرير توزيع المخزون لمنتج معين على جميع الفروع
    /// </summary>
    [HttpGet("product-distribution/{id}")]
    public async Task<ActionResult<ApiResponse<ProductStockDistributionDto>>> GetProductDistribution(int id)
    {
        var result = await _reportService.GetProductStockDistributionAsync(id);
        return Ok(ApiResponse<ProductStockDistributionDto>.SuccessResult(result));
    }
}
