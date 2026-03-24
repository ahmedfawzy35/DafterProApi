using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Middleware;

/// <summary>
/// Middleware للتحقق من أن الشركة والفرع والمستخدم مفعّلون قبل أي معالجة
/// يعمل بعد TenantResolutionMiddleware وبعد المصادقة
/// </summary>
public class TenantStatusMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantStatusMiddleware> _logger;

    // مدة الـ Cache (لتقليل الضغط على قاعدة البيانات)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    // مسارات عامة لا تحتاج للتحقق
    private static readonly string[] _publicPaths =
        ["/api/v1/auth/login", "/health", "/swagger", "/hangfire"];

    public TenantStatusMiddleware(
        RequestDelegate next,
        ILogger<TenantStatusMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser,
        StoreDbContext db, IMemoryCache cache)
    {
        // تجاوز المسارات العامة
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (_publicPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        // تجاوز الطلبات غير المصادق عليها (يعالجها Authentication Middleware)
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // مستخدمو المنصة (SuperAdmin) لا يخضعون لهذا الفلتر
        if (currentUser.IsPlatformUser)
        {
            await _next(context);
            return;
        }

        var companyId = currentUser.CompanyId;
        var branchId = currentUser.BranchId;
        var userId = currentUser.UserId;

        if (companyId is null || userId is null)
        {
            await WriteForbiddenResponse(context, "بيانات المستخدم غير مكتملة في الرمز المميز.");
            return;
        }

        // ===== التحقق من الشركة =====
        var companyCacheKey = $"tenant:company:{companyId}:enabled";
        if (!cache.TryGetValue(companyCacheKey, out bool companyEnabled))
        {
            companyEnabled = await db.Companies
                .IgnoreQueryFilters()
                .Where(c => c.Id == companyId && !c.IsDeleted)
                .Select(c => c.Enabled)
                .FirstOrDefaultAsync();
            cache.Set(companyCacheKey, companyEnabled, CacheDuration);
        }

        if (!companyEnabled)
        {
            _logger.LogWarning("الشركة {CompanyId} معطلة. رفض الطلب.", companyId);
            await WriteForbiddenResponse(context, "حساب الشركة معطّل. يرجى التواصل مع الدعم.");
            return;
        }

        // ===== التحقق من الفرع (إن وُجد) =====
        if (branchId is not null)
        {
            var branchCacheKey = $"tenant:branch:{branchId}:enabled";
            if (!cache.TryGetValue(branchCacheKey, out bool branchEnabled))
            {
                branchEnabled = await db.Branches
                    .Where(b => b.Id == branchId && b.CompanyId == companyId)
                    .Select(b => b.Enabled)
                    .FirstOrDefaultAsync();
                cache.Set(branchCacheKey, branchEnabled, CacheDuration);
            }

            if (!branchEnabled)
            {
                _logger.LogWarning("الفرع {BranchId} معطل. رفض الطلب.", branchId);
                await WriteForbiddenResponse(context, "هذا الفرع معطّل مؤقتاً.");
                return;
            }
        }

        // ===== التحقق من حساب المستخدم =====
        var userCacheKey = $"tenant:user:{userId}:enabled";
        if (!cache.TryGetValue(userCacheKey, out bool userEnabled))
        {
            userEnabled = await db.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == userId)
                .Select(u => u.Enabled)
                .FirstOrDefaultAsync();
            cache.Set(userCacheKey, userEnabled, CacheDuration);
        }

        if (!userEnabled)
        {
            _logger.LogWarning("المستخدم {UserId} معطل. رفض الطلب.", userId);
            await WriteForbiddenResponse(context, "حساب المستخدم معطّل.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteForbiddenResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Failure(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

/// <summary>
/// Extension method لتسجيل TenantStatusMiddleware بشكل أوضح
/// </summary>
public static class TenantStatusMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantStatus(this IApplicationBuilder app)
        => app.UseMiddleware<TenantStatusMiddleware>();
}
