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
using StoreManagement.Shared.Interfaces;
using System.Text.Json;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة الاشتراكات - للـ SuperAdmin فقط
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SubscriptionsController : ControllerBase
{
    private readonly StoreDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(StoreDbContext context, ISubscriptionService subscriptionService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// الحصول على قائمة جميع الخطط المتاحة
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<PlanReadDto>>>> GetPlans()
    {
        var plans = await _context.Plans
            .Include(p => p.Features)
            .Where(p => p.IsActive)
            .Select(p => new PlanReadDto
            {
                Id = p.Id, Name = p.Name, DisplayName = p.DisplayName,
                MonthlyPrice = p.MonthlyPrice, AnnualPrice = p.AnnualPrice,
                MaxUsers = p.MaxUsers, MaxBranches = p.MaxBranches,
                Features = p.Features.Where(f => f.IsEnabled).Select(f => f.FeatureKey).ToList()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PlanReadDto>>.SuccessResult(plans));
    }

    /// <summary>
    /// حالة اشتراك شركة معينة
    /// </summary>
    [HttpGet("{companyId:int}/status")]
    public async Task<ActionResult<ApiResponse<SubscriptionStatusDto>>> GetStatus(int companyId)
    {
        var status = await _subscriptionService.GetSubscriptionStatusAsync(companyId);

        if (status is null)
            return NotFound(ApiResponse<SubscriptionStatusDto>.Failure("الشركة لا تملك اشتراكاً نشطاً"));

        return Ok(ApiResponse<SubscriptionStatusDto>.SuccessResult(status));
    }

    /// <summary>
    /// إنشاء اشتراك جديد لشركة
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateSubscriptionDto dto)
    {
        var plan = await _context.Plans.FindAsync(dto.PlanId);
        if (plan is null)
            return NotFound(ApiResponse<object>.Failure("الخطة غير موجودة"));

        // إلغاء الاشتراك الحالي إذا وجد
        var existingSubscriptions = await _context.CompanySubscriptions
            .IgnoreQueryFilters()
            .Where(cs => cs.CompanyId == dto.CompanyId && cs.IsActive)
            .ToListAsync();

        foreach (var sub in existingSubscriptions)
            sub.IsActive = false;

        // إنشاء الاشتراك الجديد
        var newSubscription = new CompanySubscription
        {
            CompanyId = dto.CompanyId,
            PlanId = dto.PlanId,
            StartDate = dto.StartDate,
            EndDate = dto.StartDate.AddMonths(dto.DurationMonths),
            IsActive = true
        };

        _context.CompanySubscriptions.Add(newSubscription);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("تم إنشاء الاشتراك بنجاح"));
    }
}
