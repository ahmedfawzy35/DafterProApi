using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class PlansController : ControllerBase
{
    private readonly StoreDbContext _context;

    public PlansController(StoreDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<PlanReadDto>>>> GetAll()
    {
        var plans = await _context.Plans
            .Include(p => p.Features)
            .Where(p => p.IsActive)
            .Select(p => new PlanReadDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                MonthlyPrice = p.MonthlyPrice,
                AnnualPrice = p.AnnualPrice,
                MaxUsers = p.MaxUsers,
                MaxBranches = p.MaxBranches,
                Features = p.Features.Where(f => f.IsEnabled).Select(f => f.FeatureKey).ToList()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PlanReadDto>>.SuccessResult(plans));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Plan>>> Create([FromBody] CreatePlanDto dto)
    {
        var plan = new Plan
        {
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            MonthlyPrice = dto.MonthlyPrice,
            AnnualPrice = dto.AnnualPrice,
            MaxUsers = dto.MaxUsers,
            MaxBranches = dto.MaxBranches
        };

        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<Plan>.SuccessResult(plan, "تم إنشاء الخطة بنجاح"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var plan = await _context.Plans.FindAsync(id);
        if (plan == null) return NotFound(ApiResponse<object>.Failure("الخطة غير موجودة"));

        plan.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("تم تعطيل الخطة بنجاح"));
    }
}
