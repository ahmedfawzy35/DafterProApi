using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class PlanFeaturesController : ControllerBase
{
    private readonly StoreDbContext _context;

    public PlanFeaturesController(StoreDbContext context)
    {
        _context = context;
    }

    [HttpGet("plan/{planId}")]
    public async Task<ActionResult<ApiResponse<List<PlanFeature>>>> GetByPlan(int planId)
    {
        var features = await _context.PlanFeatures
            .Where(f => f.PlanId == planId)
            .ToListAsync();

        return Ok(ApiResponse<List<PlanFeature>>.SuccessResult(features));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PlanFeature>>> Create([FromBody] CreatePlanFeatureDto dto)
    {
        var feature = new PlanFeature
        {
            PlanId = dto.PlanId,
            FeatureKey = dto.FeatureKey,
            IsEnabled = dto.IsEnabled
        };

        _context.PlanFeatures.Add(feature);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PlanFeature>.SuccessResult(feature, "تم إضافة الميزة بنجاح"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var feature = await _context.PlanFeatures.FindAsync(id);
        if (feature == null) return NotFound(ApiResponse<object>.Failure("الميزة غير موجودة"));

        _context.PlanFeatures.Remove(feature);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("تم حذف الميزة بنجاح"));
    }
}
