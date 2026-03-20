using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان التسوية المالية المستمرة (بدلات أو تخفيضات دائمة)
/// </summary>
public class RecurringAdjustment : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // المبلغ
    public decimal Amount { get; set; }

    // نوع التسوية المستمرة (بدل أو تخفيض راتب)
    public RecurringAdjustmentType Type { get; set; }

    // هل هي إضافة أم خصم
    public AdjustmentType Mode { get; set; }

    // تاريخ البدء
    public DateTime EffectiveFrom { get; set; }

    // تاريخ الانتهاء (اختياري)
    public DateTime? EffectiveTo { get; set; }

    // هل التسوية فعالة حالياً
    public bool IsActive { get; set; } = true;

    // ملاحظات
    public string? Notes { get; set; }
}
