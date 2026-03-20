using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان بند في حساب الراتب (تفاصيل الاستحقاقات والاستقطاعات)
/// </summary>
public class PayrollRunItem : BaseEntity
{
    public int PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; } = null!;

    // مسمى البند (مثال: "راتب أساسي"، "بدل سكن"، "قسط قرض")
    public string Label { get; set; } = string.Empty;

    // قيمة البند
    public decimal Amount { get; set; }

    // نوع البند (إضافة أو خصم)
    public AdjustmentType Type { get; set; }

    // تصنيف البند (اختياري، مثلاً "الراتب الأساسي"، "بدلات"، "قروض")
    public string? Category { get; set; }
}
