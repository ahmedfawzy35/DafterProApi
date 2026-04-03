using StoreManagement.Shared.Enums;

using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// كيان حركات المخزون - يُسجل أي دخول أو خروج للمنتجات
/// </summary>
public class StockTransaction : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    // معرف المنتج
    public int ProductId { get; set; }

    // علاقة بالمنتج
    public Product Product { get; set; } = null!;

    // معرف عنصر الفاتورة المرتبط (اختياري)
    public int? InvoiceItemId { get; set; }

    // علاقة بعنصر الفاتورة
    public InvoiceItem? InvoiceItem { get; set; }

    // نوع الحركة: وارد أو صادر
    public StockMovementType MovementType { get; set; }

    // الكمية المتحركة
    public double Quantity { get; set; }

    // تاريخ حدوث الحركة
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // ملاحظات إضافية
    public string? Notes { get; set; }

    // ===== التفاصيل المرجعية والتتبع =====

    // نوع المستند المرجعي (فاتورة، تسوية، الخ)
    public StockReferenceType? ReferenceType { get; set; }

    // معرف المستند المرجعي المرتبط (على سبيل المثال InvoiceId أو AdjustmentId)
    public int? ReferenceId { get; set; }

    // سبب التسوية في حال كانت تسوية يدوية (Adjustment)
    public StockAdjustmentReason? ReasonType { get; set; }

    // الكمية قبل الحركة (Audit)
    public double BeforeQuantity { get; set; }

    // الكمية بعد الحركة (Audit)
    public double AfterQuantity { get; set; }

    // معرف المستخدم الذي سجّل الحركة
    public int UserId { get; set; }

    // علاقة بالمستخدم
    public User User { get; set; } = null!;
}
