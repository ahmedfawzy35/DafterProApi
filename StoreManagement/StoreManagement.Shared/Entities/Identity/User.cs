using Microsoft.AspNetCore.Identity;

namespace StoreManagement.Shared.Entities.Identity;

/// <summary>
/// كيان المستخدم الممتد من IdentityUser مع إضافة CompanyId و BranchId
/// </summary>
public class User : IdentityUser<int>
{
    // تحديد ما إذا كان المستخدم يتبع للمنصة (Platform) أم لشركة (Tenant)
    public bool IsPlatformUser { get; set; } = false;

    // معرف الشركة التابع لها المستخدم (اختياري لمستخدمي المنصة)
    public int? CompanyId { get; set; }

    // علاقة بالشركة
    public Company? Company { get; set; }

    // معرف الفرع (اختياري)
    public int? BranchId { get; set; }

    // علاقة بالفرع
    public Branch? Branch { get; set; }
}

