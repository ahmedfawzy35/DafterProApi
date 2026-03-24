using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان قرض الموظف (سُلفة أو قرض طويل)
/// </summary>
public class EmployeeLoan : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // المبلغ الإجمالي للقرض
    public decimal TotalAmount { get; set; }

    // مبلغ القسط الشهري
    public decimal InstallmentAmount { get; set; }

    // عدد الأشهر (الأقساط)
    public int NumberOfMonths { get; set; }

    // تاريخ بدء استقطاع الأقساط
    public DateTime StartDate { get; set; }

    // حالة القرض (نشط، مسدد، مغلق)
    public LoanStatus Status { get; set; } = LoanStatus.Active;

    // ملاحظات
    public string? Notes { get; set; }

    // قائمة الأقساط المرتبطة
    public ICollection<LoanInstallment> Installments { get; set; } = [];
}
