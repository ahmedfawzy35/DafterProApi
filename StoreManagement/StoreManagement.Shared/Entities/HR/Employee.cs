using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Entities.Configuration;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان الموظف
/// </summary>
public class Employee : BaseEntity
{
    // اسم الموظف
    public string Name { get; set; } = string.Empty;

    // نوع الموظف (طريقة الدفع)
    public EmployeeType Type { get; set; } = EmployeeType.Monthly;

    // الراتب الأساسي
    public decimal Salary { get; set; }

    // ملاحظة: البدلات والاستقطاعات تُدار عبر RecurringAdjustment - لا تُخزن هنا

    // هل الموظف فعّال
    public bool IsEnabled { get; set; } = true;

    // رقم الهاتف
    public string? Phone { get; set; }

    // الفرع الحالي الذي يعمل فيه الموظف
    public int? CurrentBranchId { get; set; }
    public Branch? CurrentBranch { get; set; }

    // سجلات الحضور والغياب
    public ICollection<Attendance> Attendances { get; set; } = [];

    // التوقيتات الوظيفية (تاريخ الحالات)
    public ICollection<EmployeeAction> Actions { get; set; } = [];

    // تاريخ تعديلات الراتب
    public ICollection<EmployeeSalary> SalaryHistory { get; set; } = [];

    // التسويات المباشرة
    public ICollection<SalaryAdjustment> Adjustments { get; set; } = [];

    // التسويات المستمرة (بدلات / استقطاعات دورية)
    public ICollection<RecurringAdjustment> RecurringAdjustments { get; set; } = [];

    // القروض
    public ICollection<EmployeeLoan> Loans { get; set; } = [];

    // تشغيلات الرواتب (Snapshots)
    public ICollection<PayrollRun> PayrollRuns { get; set; } = [];

    // سجلات الرواتب القديمة (Legacy - للتوافق مع البيانات القديمة)
    public ICollection<Payroll> Payrolls { get; set; } = [];
}


/// <summary>
/// كيان تسجيل حضور وغياب الموظف
/// </summary>
public class AttendanceRecord
{
    public int Id { get; set; }

    // معرف الموظف
    public int EmployeeId { get; set; }

    // تاريخ الحضور/الغياب
    public DateTime Date { get; set; }

    // حالة الحضور
    public AttendanceStatus Status { get; set; }

    // ملاحظات
    public string? Notes { get; set; }

    // علاقة بالموظف
    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// كيان الراتب الشهري المصروف للموظف
/// </summary>
public class Payroll
{
    public int Id { get; set; }

    // معرف الموظف
    public int EmployeeId { get; set; }

    // علاقة بالموظف
    public Employee Employee { get; set; } = null!;

    // الشهر المرتبط بالراتب
    public int Month { get; set; }

    // السنة
    public int Year { get; set; }

    // الراتب الأساسي
    public decimal Salary { get; set; }

    // المكافآت الإضافية
    public decimal Bonuses { get; set; } = 0;

    // الاستقطاعات المطبّقة
    public decimal Deductions { get; set; } = 0;

    // صافي الراتب
    public decimal NetSalary => Salary + Bonuses - Deductions;

    // تاريخ الصرف
    public DateTime? PaidDate { get; set; }
}
