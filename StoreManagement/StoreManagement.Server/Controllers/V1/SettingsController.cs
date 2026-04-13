using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.DTOs.Settings;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ICompanySettingsService _settingsService;

    public SettingsController(ICompanySettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("snapshot")]
    // Exposed to all authenticated users for frontend UI bootstrap decisions
    public async Task<ActionResult<SettingsSnapshotDto>> GetSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await _settingsService.GetSettingsSnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }

    [HttpGet]
    // Will be protected by "RequireSettingsView" policy in Phase 3
    public async Task<ActionResult<CompanySettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPatch("sales")]
    public async Task<IActionResult> UpdateSalesSettings([FromBody] UpdateSalesSettingsDto dto, CancellationToken cancellationToken)
    {
        await _settingsService.UpdateSalesSettingsAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPatch("inventory")]
    public async Task<IActionResult> UpdateInventorySettings([FromBody] UpdateInventorySettingsDto dto, CancellationToken cancellationToken)
    {
        await _settingsService.UpdateInventorySettingsAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPatch("returns")]
    public async Task<IActionResult> UpdateReturnsSettings([FromBody] UpdateReturnsSettingsDto dto, CancellationToken cancellationToken)
    {
        await _settingsService.UpdateReturnsSettingsAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPatch("installments")]
    public async Task<IActionResult> UpdateInstallmentsSettings([FromBody] UpdateInstallmentsSettingsDto dto, CancellationToken cancellationToken)
    {
        await _settingsService.UpdateInstallmentsSettingsAsync(dto, cancellationToken);
        return NoContent();
    }

    [HttpPatch("approvals")]
    public async Task<IActionResult> UpdateApprovalsSettings([FromBody] UpdateApprovalsSettingsDto dto, CancellationToken cancellationToken)
    {
        await _settingsService.UpdateApprovalsSettingsAsync(dto, cancellationToken);
        return NoContent();
    }
}
