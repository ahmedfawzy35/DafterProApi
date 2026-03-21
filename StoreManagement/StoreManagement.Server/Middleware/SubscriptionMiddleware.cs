using StoreManagement.Shared.Common;
using StoreManagement.Shared.Interfaces;
using System.Text.Json;

namespace StoreManagement.Server.Middleware;

/// <summary>
/// Middleware التحقق من صلاحية الاشتراك - يعمل بعد Authentication مباشرة
/// يرجع 403 Forbidden إذا الاشتراك منته
/// </summary>
public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionMiddleware> _logger;

    private static readonly string[] _exemptPaths =
        ["/api/v1/auth", "/health", "/swagger", "/hangfire", "/api/v1/subscriptions", "/api/v1/company/my"];

    public SubscriptionMiddleware(
        RequestDelegate next,
        ILogger<SubscriptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService, ICurrentUserService currentUser)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // تجاوز التحقق للمسارات المستثناة
        if (_exemptPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        // تجاوز التحقق إذا المستخدم غير مصادق
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // السوبر أدمن يتخطى فحص الاشتراك
        if (currentUser.IsSuperAdmin)
        {
            await _next(context);
            return;
        }

        var companyId = currentUser.CompanyId;
        if (companyId <= 0 || companyId == null)
        {
            await _next(context);
            return;
        }

        // التحقق من صلاحية الاشتراك
        var isActive = await subscriptionService.IsSubscriptionActiveAsync(companyId.Value);

        if (!isActive)
        {
            _logger.LogWarning("اشتراك منته للشركة {CompanyId}", companyId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Failure("اشتراك الشركة منته. يرجى تجديد الاشتراك للمتابعة");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            return;
        }

        await _next(context);
    }
}
