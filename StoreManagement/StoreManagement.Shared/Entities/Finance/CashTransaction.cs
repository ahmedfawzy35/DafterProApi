using StoreManagement.Shared.Enums;

using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Finance;

/// <summary>
/// كيان المعاملة النقدية (قبض / صرف)
/// </summary>
public class CashTransaction : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    // نوع المعاملة (وارد / صادر)
    public TransactionType Type { get; set; }

    // مصدر المعاملة (عميل / مورد / بنك / راتب)
    public TransactionSource SourceType { get; set; }

    // المبلغ
    public decimal Value { get; set; }

    // تاريخ المعاملة
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // ملاحظات
    public string? Notes { get; set; }

    // معرف المستخدم الذي سجّل المعاملة
    public int UserId { get; set; }

    // علاقة بالمستخدم
    public User User { get; set; } = null!;

    // معرف الطرف المرتبط (عميل أو مورد حسب SourceType)
    public int? RelatedEntityId { get; set; }

    // معرف الوردية (لربط الحركة بوردية كاشير معينة)
    public int? ShiftId { get; set; }

    // علاقة الوردية
    public CashRegisterShift? Shift { get; set; }
}
