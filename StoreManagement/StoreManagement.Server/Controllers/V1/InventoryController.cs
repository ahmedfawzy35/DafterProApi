using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة المخزون (تعديلات وحركات)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockTransactionReadDto>>>> GetHistory(
        [FromQuery] PaginationQueryDto query, [FromQuery] int? productId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _inventoryService.GetHistoryAsync(query, productId, from, to);
        return Ok(ApiResponse<PagedResult<StockTransactionReadDto>>.SuccessResult(result));
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<ApiResponse<string>>> Adjust([FromBody] CreateStockAdjustmentDto dto)
    {
        await _inventoryService.CreateAdjustmentAsync(dto);
        return Ok(ApiResponse<string>.SuccessResult("تم تسجيل حركة المخزون بنجاح"));
    }

    [HttpPost("initial")]
    public async Task<ActionResult<ApiResponse<string>>> RegisterInitial([FromQuery] int productId, [FromQuery] double quantity, [FromQuery] int branchId)
    {
        await _inventoryService.RegisterInitialStockAsync(productId, quantity, branchId);
        return Ok(ApiResponse<string>.SuccessResult("تم تسجيل الرصيد الافتتاحي بنجاح"));
    }
}
