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
    [Authorize(Policy = "RequirePurchasesPermission")]
    public async Task<ActionResult<PagedResult<LowStockAlertDto>>> GetLowStock([FromQuery] PaginationQueryDto query)
    {
        var result = await _alertService.GetLowStockAlertsAsync(query);
        return Ok(result);
    }

    [HttpGet("overdue")]
    [Authorize(Policy = "RequireSalesPermission")]
    public async Task<ActionResult<PagedResult<OverdueCustomerAlertDto>>> GetOverdue([FromQuery] PaginationQueryDto query, [FromQuery] int dayThreshold = 30)
    {
        var result = await _alertService.GetOverdueInvoicesAlertsAsync(query, dayThreshold);
        return Ok(result);
    }

    [HttpGet("high-debt")]
    [Authorize(Policy = "RequireSalesPermission")]
    public async Task<ActionResult<PagedResult<HighDebtCustomerAlertDto>>> GetHighDebt([FromQuery] PaginationQueryDto query)
    {
        var result = await _alertService.GetHighDebtCustomersAlertsAsync(query);
        return Ok(result);
    }
}
