using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم لوحة التحكم (الإحصائيات والملخصات)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("daily-stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDailyStats()
    {
        var result = await _dashboardService.GetDailyStatsAsync();
        return Ok(ApiResponse<DashboardStatsDto>.SuccessResult(result));
    }

    [HttpGet("financial-summary")]
    public async Task<ActionResult<ApiResponse<FinancialSummaryDto>>> GetFinancialSummary()
    {
        var result = await _dashboardService.GetFinancialSummaryAsync();
        return Ok(ApiResponse<FinancialSummaryDto>.SuccessResult(result));
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<ApiResponse<List<TopProductDto>>>> GetTopProducts([FromQuery] int count = 5)
    {
        var result = await _dashboardService.GetTopSellingProductsAsync(count);
        return Ok(ApiResponse<List<TopProductDto>>.SuccessResult(result));
    }

    [HttpGet("debt-alerts")]
    public async Task<ActionResult<ApiResponse<List<DebtAlertDto>>>> GetDebtAlerts()
    {
        var result = await _dashboardService.GetDebtAlertsAsync();
        return Ok(ApiResponse<List<DebtAlertDto>>.SuccessResult(result));
    }
}
