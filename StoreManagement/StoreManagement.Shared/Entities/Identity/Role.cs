using Microsoft.AspNetCore.Identity;

namespace StoreManagement.Shared.Entities.Identity;

/// <summary>
/// كيان الدور الوظيفي (Admin, Accountant, Sales, SuperAdmin)
/// </summary>
public class Role : IdentityRole<int>
{
    // وصف الدور الوظيفي
    public string? Description { get; set; }
    
    // معرف الشركة التابع لها الدور (null للأدوار العامة للنظام)
    public int? CompanyId { get; set; }
}
