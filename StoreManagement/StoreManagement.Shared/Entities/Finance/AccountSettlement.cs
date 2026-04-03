using StoreManagement.Shared.Enums;

using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Finance;

/// <summary>
/// كيان تسوية الحسابات (خصم أو إضافة رصيد يدوي دون حركة نقدية)
/// WARNING: Do NOT use this to modify CashBalance. CashBalance is legacy opening snapshot only.
/// </summary>
public class AccountSettlement : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    // نوع المصدر (عميل أو مورد)
    public SettlementSource SourceType { get; set; }

    // معرف العميل أو المورد
    public int RelatedEntityId { get; set; }

    // نوع التسوية (إضافة أو خصم)
    public SettlementType Type { get; set; }

    // سبب التسوية
    public SettlementReason Reason { get; set; }

    // مبلغ التسوية
    public decimal Amount { get; set; }

    // تاريخ التسوية
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // ملاحظات
    public string? Notes { get; set; }

    // معرف المستخدم الذي أجرى التسوية
    public int UserId { get; set; }

    // علاقة بالمستخدم
    public User User { get; set; } = null!;
}
