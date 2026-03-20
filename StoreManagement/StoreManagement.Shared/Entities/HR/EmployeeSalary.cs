namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان سجل راتب الموظف (لتتبع تاريخ الرواتب)
/// </summary>
public class EmployeeSalary : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // مبلغ الراتب الأساسي
    public decimal Amount { get; set; }

    // تاريخ بدء سريان هذا الراتب
    public DateTime EffectiveFrom { get; set; }

    // تاريخ انتهاء سريان هذا الراتب
    public DateTime? EffectiveTo { get; set; }
}
