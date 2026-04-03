using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Finance;

/// <summary>
/// كيان وردية الكاشير (يحتوي على إجماليات لحظة الإغلاق للاحتفاظ بالسجل التاريخي بدقة).
/// </summary>
public class CashRegisterShift : BaseEntity, IBranchEntity
{
    // الفرع الذي تتبعه الوردية
    public int BranchId { get; set; }

    // المستخدم (الكاشير) الذي فتح الوردية
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // رصيد بداية الوردية (الافتتاحي)
    public decimal OpeningBalance { get; set; }

    // إجمالي الداخل للوردية (Snapshots)
    public decimal TotalCashIn { get; set; }

    // إجمالي الخارج من الوردية (Snapshots)
    public decimal TotalCashOut { get; set; }

    // الرصيد المحسوب نهاية الوردية (Opening + In - Out)
    public decimal? ClosingBalance { get; set; }

    // الرصيد الفعلي الذي تمت عده يدويا عند الإغلاق
    public decimal? ActualClosingBalance { get; set; }

    // الفارق (العجز أو الزيادة)
    public decimal? Difference { get; set; }

    // توقيت فتح وإغلاق الوردية
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    // حالة الوردية
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    // المعاملات المرتبطة بهذه الوردية
    public ICollection<CashTransaction> CashTransactions { get; set; } = [];
}
