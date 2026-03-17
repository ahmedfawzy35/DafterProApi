using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Entities;
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
}
