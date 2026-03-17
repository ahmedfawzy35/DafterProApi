using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة Feature Flags مع دعم Plan Features + Company Overrides
/// </summary>
public class FeatureService : IFeatureService
{
    private readonly ISubscriptionService _subscriptionService;

    public FeatureService(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public async Task<bool> IsFeatureEnabledAsync(int companyId, string featureKey)
    {
        var subscription = await _subscriptionService.GetActiveSubscriptionAsync(companyId);

        // إذا لم يوجد اشتراك نشط، لا يمكن الوصول لأي ميزة
        if (subscription is null || !subscription.IsActive || subscription.EndDate < DateTime.UtcNow)
            return false;

        // 1️⃣ التحقق من Override مخصص للشركة أولاً (أعلى أولوية)
        var override_ = subscription.FeatureOverrides
            .FirstOrDefault(o => o.FeatureKey.Equals(featureKey, StringComparison.OrdinalIgnoreCase));

        if (override_ is not null)
            return override_.IsEnabled;

        // 2️⃣ التحقق من ميزات الخطة الأساسية
        var planFeature = subscription.Plan?.Features
            .FirstOrDefault(f => f.FeatureKey.Equals(featureKey, StringComparison.OrdinalIgnoreCase));

        return planFeature?.IsEnabled ?? false;
    }
}
