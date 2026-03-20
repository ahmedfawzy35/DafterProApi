namespace StoreManagement.Shared.Entities.Core;

/// <summary>
/// كيان سجل التدقيق لتتبع التغييرات على البيانات
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    // معرف المستخدم الذي نفّذ التغيير
    public int? UserId { get; set; }

    // اسم المستخدم
    public string? UserName { get; set; }

    // اسم الجدول/الكيان الذي تم تعديله
    public string EntityName { get; set; } = string.Empty;

    // معرف السجل المعدَّل
    public string EntityId { get; set; } = string.Empty;

    // نوع العملية (Create / Update / Delete)
    public string Action { get; set; } = string.Empty;

    // القيم القديمة قبل التعديل (JSON)
    public string? OldValues { get; set; }

    // القيم الجديدة بعد التعديل (JSON)
    public string? NewValues { get; set; }

    // معرف الشركة لدعم عزل البيانات
    public int CompanyId { get; set; }

    // تاريخ التغيير
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // عنوان الطلب الذي نفّذ التغيير
    public string? RequestPath { get; set; }
}
