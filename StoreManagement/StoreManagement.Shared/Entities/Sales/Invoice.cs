using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Sales;

/// <summary>
/// كيان الفاتورة الرئيسي (مبيعات أو مشتريات)
/// </summary>
public class Invoice : BaseEntity
{
    // نوع الفاتورة (مبيعات / مشتريات)
    public InvoiceType Type { get; set; }

    // معرف العميل (للمبيعات)
    public int? CustomerId { get; set; }

    // علاقة بالعميل
    public Customer? Customer { get; set; }

    // معرف المورد (للمشتريات)
    public int? SupplierId { get; set; }

    // علاقة بالمورد
    public Supplier? Supplier { get; set; }

    // تاريخ الفاتورة
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // إجمالي قيمة الفاتورة
    public decimal TotalValue { get; set; }

    // الخصم
    public decimal Discount { get; set; } = 0;

    // المبلغ المدفوع
    public decimal Paid { get; set; } = 0;

    // هل تدفع على أقساط
    public bool IsInstallment { get; set; } = false;

    // ملاحظات
    public string? Notes { get; set; }

    // عناصر الفاتورة التفصيلية
    public ICollection<InvoiceItem> Items { get; set; } = [];
}

/// <summary>
/// تفاصيل عناصر الفاتورة (سطر لكل منتج)
/// </summary>
public class InvoiceItem
{
    public int Id { get; set; }

    // معرف الفاتورة
    public int InvoiceId { get; set; }

    // علاقة بالفاتورة
    public Invoice Invoice { get; set; } = null!;

    // معرف المنتج
    public int ProductId { get; set; }

    // علاقة بالمنتج
    public Product Product { get; set; } = null!;

    // الكمية
    public double Quantity { get; set; }

    // سعر الوحدة عند البيع/الشراء
    public decimal UnitPrice { get; set; }

    // الإجمالي الفرعي
    public decimal Subtotal => (decimal)Quantity * UnitPrice;
}
