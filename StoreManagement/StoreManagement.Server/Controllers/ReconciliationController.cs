using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Entities.Diagnostics;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Assuming platform users / admin only
public class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ICurrentUserService _currentUserService;

    public ReconciliationController(IReconciliationService reconciliationService, ICurrentUserService currentUserService)
    {
        _reconciliationService = reconciliationService;
        _currentUserService = currentUserService;
    }

    [HttpGet("findings")]
    public async Task<IActionResult> GetFindings(
        [FromQuery] FindingStatus? status,
        [FromQuery] string? category,
        [FromQuery] string? severity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var companyId = _currentUserService.CompanyId;
        if (companyId == null) return Forbid();

        var findings = await _reconciliationService.GetStoredFindingsAsync(companyId.Value, status, category, severity, from, to);
        return Ok(findings);
    }

    [HttpGet("findings/{id}")]
    public async Task<IActionResult> GetFindingById(int id)
    {
        var companyId = _currentUserService.CompanyId;
        if (companyId == null) return Forbid();

        var finding = await _reconciliationService.GetFindingByIdAsync(id, companyId.Value);
        if (finding == null) return NotFound();

        return Ok(finding);
    }
}
