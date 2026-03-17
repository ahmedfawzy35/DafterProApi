using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة استخراج بيانات المستخدم الحالي من الـ JWT عبر HttpContext
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // الحصول على الـ ClaimsPrincipal الخاص بالطلب الحالي
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    // معرف المستخدم من الـ Claims
    public int? UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : null;
        }
    }

    // معرف الشركة من الـ Claims (عنصر أساسي في عزل البيانات)
    public int CompanyId
    {
        get
        {
            var companyIdClaim = User?.FindFirst("CompanyId")?.Value;
            return int.TryParse(companyIdClaim, out var id) ? id : 0;
        }
    }

    // اسم المستخدم
    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;

    // الأدوار الوظيفية للمستخدم
    public IEnumerable<string> Roles
        => User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];

    // هل المستخدم Super Admin (يمكنه رؤية كل البيانات بتجاوز الفلاتر)
    public bool IsSuperAdmin
        => User?.IsInRole("SuperAdmin") ?? false;
}
