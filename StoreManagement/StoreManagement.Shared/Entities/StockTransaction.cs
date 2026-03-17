using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities;

/// <summary>
/// كيان حركات المخزون - يُسجل أي دخول أو خروج للمنتجات
/// </summary>
public class StockTransaction : BaseEntity
{
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

    // معرف المستخدم الذي سجّل الحركة
    public int UserId { get; set; }

    // علاقة بالمستخدم
    public User User { get; set; } = null!;
}
