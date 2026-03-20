namespace StoreManagement.Shared.Entities.HR;

/// <summary>
/// كيان قسط القرض (سجل استقطاع محدد)
/// </summary>
public class LoanInstallment : BaseEntity
{
    public int LoanId { get; set; }
    public EmployeeLoan Loan { get; set; } = null!;

    // الشهر المرتبط (للاستقطاع التلقائي)
    public int Month { get; set; }

    // السنة
    public int Year { get; set; }

    // مبلغ القسط
    public decimal Amount { get; set; }

    // هل تم استقطاعه/دفعه
    public bool IsPaid { get; set; } = false;

    // تاريخ الدفع/الاستقطاع
    public DateTime? PaidAt { get; set; }
}
