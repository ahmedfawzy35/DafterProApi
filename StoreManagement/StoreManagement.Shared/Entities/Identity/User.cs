using Microsoft.AspNetCore.Identity;

namespace StoreManagement.Shared.Entities.Identity;

/// <summary>
/// كيان المستخدم الممتد من IdentityUser مع إضافة CompanyId و BranchId
/// </summary>
public class User : IdentityUser<int>
{
    // معرف الشركة التابع لها المستخدم
    public int CompanyId { get; set; }

    // علاقة بالشركة
    public Company Company { get; set; } = null!;

    // معرف الفرع (اختياري)
    public int? BranchId { get; set; }

    // علاقة بالفرع
    public Branch? Branch { get; set; }
}

