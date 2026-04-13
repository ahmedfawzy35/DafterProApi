using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[Route("api/v1/reports/installments")]
[Authorize(Policy = "RequirePermission:reports.view")]
public class InstallmentReportsController : ControllerBase
{
    private readonly IInstallmentReportService _reportService;

    public InstallmentReportsController(IInstallmentReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<InstallmentSummaryDto>>> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _reportService.GetInstallmentSummaryAsync(from, to);
        return Ok(ApiResponse<InstallmentSummaryDto>.SuccessResult(result));
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<ApiResponse<PagedResult<InstallmentReportItemDto>>>> GetOverdueInstallments(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] int? customerId)
    {
        var result = await _reportService.GetOverdueInstallmentsAsync(query, customerId);
        return Ok(ApiResponse<PagedResult<InstallmentReportItemDto>>.SuccessResult(result));
    }
}
