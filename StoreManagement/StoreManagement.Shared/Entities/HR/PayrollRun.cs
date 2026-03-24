using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان تشغيل الراتب (سجل توثيقي لراتب شهر محدد للموظف)
/// </summary>
public class PayrollRun : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // الشهر
    public int Month { get; set; }

    // السنة
    public int Year { get; set; }

    // الراتب الأساسي المستخدم في وقت الحساب
    public decimal BasicSalary { get; set; }

    // إجمالي البدلات (من Adjustments و Recurring)
    public decimal TotalAllowances { get; set; }

    // إجمالي الخصومات (من Adjustments و Recurring)
    public decimal TotalDeductions { get; set; }

    // إجمالي استقطاعات القروض
    public decimal LoanDeductions { get; set; }

    // صافي الراتب النهائي
    public decimal NetSalary { get; set; }

    // هل تم اعتماد/قفل الراتب (لا يمكن التعديل بعد ذلك)
    public bool IsLocked { get; set; } = false;

    // تاريخ الحساب/التوليد
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // تفاصيل البنود المكونة لهذا الراتب
    public ICollection<PayrollRunItem> Items { get; set; } = [];
}
