using StoreManagement.Shared.Enums;

using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان الحضور اليومي للموظف
/// </summary>
public class Attendance : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // تاريخ اليوم
    public DateTime Date { get; set; }

    // حالة الحضور (حاضر، غائب، إجازة، عطلة)
    public AttendanceStatus Status { get; set; }

    // عدد ساعات العمل (اختياري)
    public decimal WorkingHours { get; set; } = 0;

    // ملاحظات
    public string? Notes { get; set; }
}
