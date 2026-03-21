namespace StoreManagement.Shared.Interfaces;

/// <summary>
/// واجهة استخراج بيانات المستخدم الحالي من الـ HTTP Context
/// </summary>
public interface ICurrentUserService
{
    // معرف المستخدم الحالي
    int? UserId { get; }

    // معرف شركة المستخدم الحالي (من الـ Token)
    int? CompanyId { get; }

    // معرف الشركة المحدد للـ Platform User بشكل صريح لمحدودية النطاق (Scoped Company)
    int? ScopedCompanyId { get; set; }

    // هل المستخدم تابع للمنصة؟
    bool IsPlatformUser { get; }

    // اسم المستخدم
    string? UserName { get; }

    // أدوار المستخدم
    IEnumerable<string> Roles { get; }

    // هل المستخدم Super Admin (يمكنه تجاوز عوامل التصفية)
    bool IsSuperAdmin { get; }
}
