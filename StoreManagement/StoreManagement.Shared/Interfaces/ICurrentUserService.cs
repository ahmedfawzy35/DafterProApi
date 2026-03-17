namespace StoreManagement.Shared.Interfaces;

/// <summary>
/// واجهة استخراج بيانات المستخدم الحالي من الـ HTTP Context
/// </summary>
public interface ICurrentUserService
{
    // معرف المستخدم الحالي
    int? UserId { get; }

    // معرف شركة المستخدم الحالي
    int CompanyId { get; }

    // اسم المستخدم
    string? UserName { get; }

    // أدوار المستخدم
    IEnumerable<string> Roles { get; }

    // هل المستخدم Super Admin (يمكنه تجاوز عوامل التصفية)
    bool IsSuperAdmin { get; }
}
