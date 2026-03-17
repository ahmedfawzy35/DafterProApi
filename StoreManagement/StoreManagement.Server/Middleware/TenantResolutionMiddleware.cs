using System.Security.Claims;
using StoreManagement.Shared.Common;
using System.Text.Json;

namespace StoreManagement.Server.Middleware;

/// <summary>
/// Middleware لاستخراج CompanyId من JWT والتحقق منه
/// يعمل قبل أي Middleware يعتمد على CompanyId
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    // Endpoints لا تحتاج للتحقق من الـ Tenant
    private static readonly string[] _publicEndpoints =
        ["/api/v1/auth/login", "/health", "/swagger", "/hangfire"];

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // تجاوز التحقق للـ Endpoints العامة
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (_publicEndpoints.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        // التحقق من وجود المستخدم مصادق عليه
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;

            if (string.IsNullOrWhiteSpace(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
            {
                _logger.LogWarning("طلب مرفوض: CompanyId غير موجود في الـ JWT للمستخدم {User}",
                    context.User.FindFirst(ClaimTypes.Email)?.Value);

                await WriteUnauthorizedResponse(context, "CompanyId مفقود في الرمز المميز");
                return;
            }

            // تخزين CompanyId في HttpContext.Items للوصول السريع
            context.Items["CompanyId"] = companyId;

            _logger.LogDebug("تم تحديد الـ Tenant: CompanyId={CompanyId}", companyId);
        }

        await _next(context);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
