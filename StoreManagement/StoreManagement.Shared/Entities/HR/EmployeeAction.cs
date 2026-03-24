using StoreManagement.Shared.Enums;

using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان إجراء الموظف (الجدول الزمني للحالة الوظيفية)
/// </summary>
public class EmployeeAction : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // نوع الإجراء (تعيين، إنهاء، إجازة بدون راتب، إلخ)
    public EmployeeActionType ActionType { get; set; }

    // تاريخ بدء الإجراء
    public DateTime EffectiveFrom { get; set; }

    // تاريخ انتهاء الإجراء (اختياري)
    public DateTime? EffectiveTo { get; set; }

    // ملاحظات
    public string? Notes { get; set; }
}
