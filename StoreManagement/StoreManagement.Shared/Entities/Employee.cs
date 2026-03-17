using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities;

/// <summary>
/// كيان الموظف
/// </summary>
public class Employee : BaseEntity
{
    // اسم الموظف
    public string Name { get; set; } = string.Empty;

    // الراتب الأساسي
    public double Salary { get; set; }

    // البدلات الإضافية
    public double Allowances { get; set; } = 0;

    // الاستقطاعات
    public double Deductions { get; set; } = 0;

    // هل الموظف فعّال
    public bool IsEnabled { get; set; } = true;

    // رقم الهاتف
    public string? Phone { get; set; }

    // سجلات الحضور والغياب
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];

    // سجلات الرواتب المصروفة
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
    public double Salary { get; set; }

    // المكافآت الإضافية
    public double Bonuses { get; set; } = 0;

    // الاستقطاعات المطبّقة
    public double Deductions { get; set; } = 0;

    // صافي الراتب
    public double NetSalary => Salary + Bonuses - Deductions;

    // تاريخ الصرف
    public DateTime? PaidDate { get; set; }
}
