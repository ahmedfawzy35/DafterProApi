using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet("low-stock")]
    [Authorize(Policy = "RequirePermission:purchases.view")]
    public async Task<ActionResult<ApiResponse<PagedResult<LowStockAlertDto>>>> GetLowStock([FromQuery] PaginationQueryDto query, [FromQuery] int? branchId = null)
    {
        var result = await _alertService.GetLowStockAlertsAsync(query, branchId);
        return Ok(ApiResponse<PagedResult<LowStockAlertDto>>.SuccessResult(result));
    }

    [HttpGet("overdue")]
    [Authorize(Policy = "RequirePermission:sales.view")]
    public async Task<ActionResult<ApiResponse<PagedResult<OverdueCustomerAlertDto>>>> GetOverdue([FromQuery] PaginationQueryDto query, [FromQuery] int dayThreshold = 30)
    {
        var result = await _alertService.GetOverdueInvoicesAlertsAsync(query, dayThreshold);
        return Ok(ApiResponse<PagedResult<OverdueCustomerAlertDto>>.SuccessResult(result));
    }

    [HttpGet("high-debt")]
    [Authorize(Policy = "RequirePermission:sales.view")]
    public async Task<ActionResult<ApiResponse<PagedResult<HighDebtCustomerAlertDto>>>> GetHighDebt([FromQuery] PaginationQueryDto query)
    {
        var result = await _alertService.GetHighDebtCustomersAlertsAsync(query);
        return Ok(ApiResponse<PagedResult<HighDebtCustomerAlertDto>>.SuccessResult(result));
    }
}
