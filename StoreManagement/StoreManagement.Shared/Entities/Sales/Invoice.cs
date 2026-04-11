using StoreManagement.Shared.Enums;

using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Entities.Finance;

namespace StoreManagement.Shared.Entities.Sales;

/// <summary>
/// كيان الفاتورة الرئيسي (مبيعات أو مشتريات)
/// </summary>
public class Invoice : BaseEntity, IBranchEntity
{
    // معرف الفرع
    public int BranchId { get; set; }

    // نوع الفاتورة (مبيعات / مشتريات)
    public InvoiceType Type { get; set; }

    // حالة الفاتورة (مسودة / مؤكدة / ملغية)
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    // حالة السداد بالاعتماد على المدفوعات المخصصة
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    // هل الفاتورة مرتجع؟
    public bool IsReturn => Type == InvoiceType.SalesReturn || Type == InvoiceType.PurchaseReturn;

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

    // الضريبة (الضريبة على القيمة المضافة وغيرها)
    public decimal Tax { get; set; } = 0;

    // الصافي الإجمالي للفاتورة
    public decimal NetTotal => TotalValue - Discount + Tax;

    // المبلغ المدفوع (متروك مؤقتاً للتوافقية المكتسبة)
    public decimal Paid { get; set; } = 0;

    // مقدار ما تم سداده وتخصيصه للفاتورة عبر السندات
    public decimal AllocatedAmount { get; set; } = 0;

    // المتبقي المستحق على الفاتورة
    public decimal RemainingAmount => NetTotal - AllocatedAmount;

    // هل تدفع على أقساط
    public bool IsInstallment { get; set; } = false;

    // ملاحظات
    public string? Notes { get; set; }

    // المرجع الأصلي للفاتورة (في حالة المرتجعات)
    public int? OriginalInvoiceId { get; set; }
    
    // الفاتورة الأصلية المرتبطة بها هذه الفاتورة (للمرتجعات)
    public Invoice? OriginalInvoice { get; set; }

    // ===== حقول المرتجعات =====
    public ReturnMode? ReturnMode { get; set; }
    public string? ReturnReason { get; set; }

    /// <summary>
    /// يُحدد إذا كان المرتجع يُنشئ استرداد نقدي فعلي (Refund) أم إشعار دائن فقط (CreditNote).
    /// القيمة الافتراضية false تعني CreditNote (بدون حركة خزنة).
    /// يتم تخزين هذا القرار لاستخدامه عند اعتماد المرتجعات اليدوية.
    /// </summary>
    public bool IssueCashRefund { get; set; } = false;

    // ===== حقول Approval Workflow ======
    public bool RequiresApproval { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectionReason { get; set; }

    // عناصر الفاتورة التفصيلية
    public ICollection<InvoiceItem> Items { get; set; } = [];

    // الإيصالات المخصصة من طرف العميل
    public ICollection<CustomerReceiptAllocation> CustomerAllocations { get; set; } = [];

    // الإيصالات المخصصة من طرف المورد
    public ICollection<SupplierPaymentAllocation> SupplierAllocations { get; set; } = [];

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }

    // ===== ERP Traceability & Financial Integrity =====
    public string? IdempotencyKey { get; set; } 
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
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
    public decimal Quantity { get; set; }

    // سعر الوحدة عند البيع/الشراء
    public decimal UnitPrice { get; set; }

    // الإجمالي الفرعي
    public decimal Subtotal => Quantity * UnitPrice;

    // ===== ربط سطر المرتجع بالسطر الأصلي =====
    public int? OriginalInvoiceItemId { get; set; }
    public InvoiceItem? OriginalInvoiceItem { get; set; }

    // ===== نظام حساب الأرباح =====

    // سعر التكلفة للوحدة الواحدة "وقت البيع" (حتى لا تتأثر الحسابات السابقة بتغير تكلفة المنتج لاحقاً)
    public decimal CostPriceAtSale { get; set; }

    // إجمالي التكلفة لهذا السطر
    public decimal TotalCost => (decimal)Quantity * CostPriceAtSale;

    // الربح من هذا السطر (للبيع فقط، للمشتريات سيكون 0 عادةً)
    public decimal Profit => Subtotal - TotalCost;

    // هامش الربح (كنسبة مئوية)
    public decimal ProfitMargin => Subtotal > 0 ? (Profit / Subtotal) * 100 : 0;
}
