using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان التسوية المالية لمرة واحدة (إضافة أو خصم لشهر محدد)
/// </summary>
public class SalaryAdjustment : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // المبلغ
    public decimal Amount { get; set; }

    // نوع التسوية (إضافة أو خصم)
    public AdjustmentType Type { get; set; }

    // الشهر المرتبط
    public int Month { get; set; }

    // السنة المرتبطة
    public int Year { get; set; }

    // ملاحظات
    public string? Notes { get; set; }
}
