using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Filters;

/// <summary>
/// Action Filter للتحقق من توفر ميزة معينة في خطة اشتراك الشركة
/// يرجع 403 Forbidden إذا الميزة غير متاحة
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _featureKey;

    public RequireFeatureAttribute(string featureKey)
    {
        _featureKey = featureKey;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var featureService = context.HttpContext.RequestServices.GetRequiredService<IFeatureService>();
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        // السوبر أدمن يتجاوز فحص الميزات
        if (currentUser.IsSuperAdmin)
        {
            await next();
            return;
        }

        var companyId = currentUser.CompanyId;
        if (companyId <= 0)
        {
            await next();
            return;
        }

        var isEnabled = await featureService.IsFeatureEnabledAsync(companyId.Value, _featureKey);

        if (!isEnabled)
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Failure($"الميزة '{_featureKey}' غير متاحة في خطة اشتراككم الحالية"))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

