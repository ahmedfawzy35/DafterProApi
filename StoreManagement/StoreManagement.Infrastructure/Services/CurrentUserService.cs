using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Constants;

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
    public int? CompanyId
    {
        get
        {
            var companyIdClaim = User?.FindFirst(AppClaims.CompanyId)?.Value 
                                 ?? User?.FindFirst("companyId")?.Value;
            return int.TryParse(companyIdClaim, out var id) ? id : null;
        }
    }

    // معرف الفرع: يُقرأ أولاً من الـ JWT Claim، ثم من الـ Header x-branch-id (للمديرين)
    public int? BranchId
    {
        get
        {
            // JWT Claim (للموظفين المرتبطين بفرع واحد)
            var branchIdClaim = User?.FindFirst(AppClaims.BranchId)?.Value
                             ?? User?.FindFirst("branchId")?.Value;
            if (int.TryParse(branchIdClaim, out var fromToken))
                return fromToken;

            // HTTP Header (للمدير الذي يختار الفرع في كل طلب)
            var headerVal = _httpContextAccessor.HttpContext?.Request.Headers["x-branch-id"].FirstOrDefault();
            return int.TryParse(headerVal, out var fromHeader) ? fromHeader : null;
        }
    }

    // معرف النطاق المؤقت لمدير النظام الأساسي (يُعين في الـ Action Filter)
    public int? ScopedCompanyId { get; set; }

    // هل المستخدم منصة
    public bool IsPlatformUser
    {
        get
        {
            var platformClaim = User?.FindFirst(AppClaims.IsPlatformUser)?.Value;
            return platformClaim == "1";
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
