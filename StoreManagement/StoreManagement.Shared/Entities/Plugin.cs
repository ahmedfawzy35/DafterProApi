namespace StoreManagement.Shared.Entities;

/// <summary>
/// كيان الإضافة (Plugin) - نظام Data-driven لإدارة الميزات الاختيارية
/// </summary>
public class Plugin
{
    public int Id { get; set; }

    // اسم الإضافة (مثل: DigitalWallet)
    public string Name { get; set; } = string.Empty;

    // عنوان الإضافة المعروض للمستخدم
    public string DisplayName { get; set; } = string.Empty;

    // وصف الإضافة
    public string? Description { get; set; }

    // هل الإضافة مفعّلة
    public bool IsEnabled { get; set; } = false;

    // إعدادات الإضافة بصيغة JSON
    public string? ConfigJson { get; set; }

    // معرف الشركة المرتبطة بالإضافة
    public int CompanyId { get; set; }

    // تاريخ التفعيل
    public DateTime? EnabledAt { get; set; }
}
