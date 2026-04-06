using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة المخزون — تسويات وتحويلات وسجل الحركات
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IBranchInventoryService _branchInventoryService;

    public InventoryController(IInventoryService inventoryService, IBranchInventoryService branchInventoryService)
    {
        _inventoryService = inventoryService;
        _branchInventoryService = branchInventoryService;
    }

    // =========================================================================
    // سجل حركات المخزون (Stock History)
    // =========================================================================

    /// <summary>GET /api/v1/inventory/history</summary>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockTransactionReadDto>>>> GetHistory(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] int? productId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _inventoryService.GetHistoryAsync(query, productId, from, to);
        return Ok(ApiResponse<PagedResult<StockTransactionReadDto>>.SuccessResult(result));
    }

    // =========================================================================
    // التسويات (Adjustments)
    // =========================================================================

    /// <summary>POST /api/v1/inventory/adjustments — إنشاء مستند تسوية مخزون</summary>
    [HttpPost("adjustments")]
    [Authorize(Roles = "admin,accountant,storekeeper")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentReadDto>>> CreateAdjustment(
        [FromBody] CreateStockAdjustmentDto dto)
    {
        var result = await _inventoryService.CreateStockAdjustmentAsync(dto);
        return Ok(ApiResponse<StockAdjustmentReadDto>.SuccessResult(result, "تم إنشاء مستند التسوية بنجاح."));
    }

    /// <summary>GET /api/v1/inventory/adjustments — قائمة مستندات التسوية</summary>
    [HttpGet("adjustments")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockAdjustmentReadDto>>>> GetAllAdjustments(
        [FromQuery] PaginationQueryDto query)
    {
        var result = await _inventoryService.GetAllAdjustmentsAsync(query);
        return Ok(ApiResponse<PagedResult<StockAdjustmentReadDto>>.SuccessResult(result));
    }

    /// <summary>GET /api/v1/inventory/adjustments/{id} — تفاصيل مستند تسوية</summary>
    [HttpGet("adjustments/{id:int}")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentReadDto>>> GetAdjustmentById(int id)
    {
        var result = await _inventoryService.GetAdjustmentByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<StockAdjustmentReadDto>.Failure("مستند التسوية غير موجود."));
        return Ok(ApiResponse<StockAdjustmentReadDto>.SuccessResult(result));
    }

    // =========================================================================
    // التحويلات بين الفروع (Transfers)
    // =========================================================================

    /// <summary>POST /api/v1/inventory/transfers — إنشاء مستند نقل مخزون بين فرعين</summary>
    [HttpPost("transfers")]
    [Authorize(Roles = "admin,accountant,storekeeper")]
    public async Task<ActionResult<ApiResponse<StockTransferReadDto>>> CreateTransfer(
        [FromBody] CreateStockTransferDto dto)
    {
        var result = await _inventoryService.CreateStockTransferAsync(dto);
        return Ok(ApiResponse<StockTransferReadDto>.SuccessResult(result, "تم إنشاء مستند التحويل وتنفيذ حركات المخزون بنجاح."));
    }

    /// <summary>GET /api/v1/inventory/transfers — قائمة مستندات التحويل</summary>
    [HttpGet("transfers")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockTransferReadDto>>>> GetAllTransfers(
        [FromQuery] PaginationQueryDto query)
    {
        var result = await _inventoryService.GetAllTransfersAsync(query);
        return Ok(ApiResponse<PagedResult<StockTransferReadDto>>.SuccessResult(result));
    }

    /// <summary>GET /api/v1/inventory/transfers/{id} — تفاصيل مستند تحويل</summary>
    [HttpGet("transfers/{id:int}")]
    public async Task<ActionResult<ApiResponse<StockTransferReadDto>>> GetTransferById(int id)
    {
        var result = await _inventoryService.GetTransferByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<StockTransferReadDto>.Failure("مستند التحويل غير موجود."));
        return Ok(ApiResponse<StockTransferReadDto>.SuccessResult(result));
    }

    // =========================================================================
    // الرصيد الافتتاحي (Initial Stock)
    // =========================================================================

    /// <summary>POST /api/v1/inventory/branches/init-stocks — تهيئة وتعبئة أرصدة الفروع (Backfill)</summary>
    [HttpPost("branches/init-stocks")]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<ApiResponse<BranchStockInitializationResultDto>>> InitBranchStocks(
        [FromQuery] bool forceReset = false,
        [FromQuery] bool dryRun = false,
        [FromQuery] int? companyId = null)
    {
        // دعم قراءة X-Dry-Run من الـ Header كبديل
        if (Request.Headers.TryGetValue("X-Dry-Run", out var headerDryRun) && bool.TryParse(headerDryRun, out var isDryRun))
        {
            dryRun = dryRun || isDryRun;
        }

        var result = await _branchInventoryService.InitializeFromTransactionsAsync(companyId, forceReset, dryRun);
        var msg = dryRun 
            ? $"تم تشغيل الفحص الوهمي (Dry Run) بنجاح. السجلات المتأثرة: {result.BranchStockRowsCreated + result.BranchStockRowsUpdated} - التحذيرات: {result.Warnings.Count} - استغرق: {result.DurationMs} ms" 
            : $"تم تعمير الأرصدة بنجاح. السجلات المتأثرة: {result.BranchStockRowsCreated + result.BranchStockRowsUpdated} - التحذيرات: {result.Warnings.Count} - استغرق: {result.DurationMs} ms";
        return Ok(ApiResponse<BranchStockInitializationResultDto>.SuccessResult(result, msg));
    }

    /// <summary>POST /api/v1/inventory/initial — تسجيل رصيد أول المدة</summary>
    [HttpPost("initial")]
    [Authorize(Roles = "admin,accountant")]
    public async Task<ActionResult<ApiResponse<string>>> RegisterInitial(
        [FromQuery] int productId,
        [FromQuery] double quantity,
        [FromQuery] int branchId)
    {
        await _inventoryService.RegisterInitialStockAsync(productId, quantity, branchId);
        return Ok(ApiResponse<string>.SuccessResult("تم تسجيل الرصيد الافتتاحي بنجاح."));
    }
}
