using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
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
using Microsoft.Extensions.Caching.Memory;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة التحقق من حالة اشتراك الشركة مع Caching داخل Request Scope
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly StoreDbContext _context;
    private readonly ICacheService _cacheService;

    public SubscriptionService(StoreDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<bool> IsSubscriptionActiveAsync(int companyId)
    {
        var subscription = await GetActiveSubscriptionAsync(companyId);
        return subscription is not null
            && subscription.IsActive
            && subscription.EndDate > DateTime.UtcNow;
    }

    public async Task<CompanySubscription?> GetActiveSubscriptionAsync(int companyId)
    {
        var cacheKey = $"subscription:{companyId}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _context.CompanySubscriptions
                .Include(cs => cs.Plan)
                    .ThenInclude(p => p.Features)
                .Include(cs => cs.FeatureOverrides)
                .IgnoreQueryFilters()   // بيانات الاشتراك غير مفلترة بـ CompanyId
                .Where(cs => cs.CompanyId == companyId && cs.IsActive)
                .OrderByDescending(cs => cs.EndDate)
                .FirstOrDefaultAsync();
        }, TimeSpan.FromMinutes(5)); // Cache لمدة 5 دقائق لكل Request
    }

    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(int companyId)
    {
        var subscription = await GetActiveSubscriptionAsync(companyId);

        if (subscription is null) return null;

        var enabledFeatures = subscription.Plan?.Features
            .Where(f => f.IsEnabled).Select(f => f.FeatureKey).ToList() ?? [];

        // تطبيق الـ Overrides
        foreach (var ov in subscription.FeatureOverrides)
        {
            if (ov.IsEnabled && !enabledFeatures.Contains(ov.FeatureKey))
                enabledFeatures.Add(ov.FeatureKey);
            else if (!ov.IsEnabled)
                enabledFeatures.Remove(ov.FeatureKey);
        }

        return new SubscriptionStatusDto
        {
            IsActive = subscription.IsActive && subscription.EndDate > DateTime.UtcNow,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            DaysRemaining = Math.Max(0, (int)(subscription.EndDate - DateTime.UtcNow).TotalDays),
            PlanName = subscription.Plan?.DisplayName ?? "",
            EnabledFeatures = enabledFeatures
        };
    }
}
