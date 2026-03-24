using System.Security.Claims;
using StoreManagement.Shared.Common;
using System.Text.Json;
using StoreManagement.Shared.Constants;
using StoreManagement.Server.Extensions;

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
        if (context.Request.Path.Value.IsSafePublicPath(_publicEndpoints))
        {
            await _next(context);
            return;
        }

        // التحقق من وجود المستخدم مصادق عليه
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var companyIdClaim = context.User.FindFirst(AppClaims.CompanyId)?.Value ?? context.User.FindFirst("companyId")?.Value;
            var isPlatformClaim = context.User.FindFirst(AppClaims.IsPlatformUser)?.Value ?? context.User.FindFirst("isPlatformUser")?.Value;
            var isPlatformUser = isPlatformClaim == "1";

            if (!isPlatformUser && (string.IsNullOrWhiteSpace(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId)))
            {
                _logger.LogWarning("طلب مرفوض: CompanyId غير موجود في الـ JWT للمستخدم {User}",
                    context.User.FindFirst(ClaimTypes.Email)?.Value);

                await WriteUnauthorizedResponse(context, "CompanyId مفقود في الرمز المميز");
                return;
            }

            if (int.TryParse(companyIdClaim, out var parsedCompanyId))
            {
                context.Items["CompanyId"] = parsedCompanyId;
                _logger.LogDebug("تم تحديد الـ Tenant: CompanyId={CompanyId}", parsedCompanyId);
            }
            context.Items["IsPlatformUser"] = isPlatformUser;

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            string scopeType = isPlatformUser ? "Platform" : "Tenant";

            // إثراء السجلات بالمعلومات الحالية (Request Logging)
            using (Serilog.Context.LogContext.PushProperty("UserId", userId))
            using (Serilog.Context.LogContext.PushProperty("CompanyId", companyIdClaim))
            using (Serilog.Context.LogContext.PushProperty("IsPlatformUser", isPlatformUser))
            using (Serilog.Context.LogContext.PushProperty("ScopeType", scopeType))
            {
                await _next(context);
                return; // تأكد من الخروج بعد استدعاء next داخل using block
            }
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
