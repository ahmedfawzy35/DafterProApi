using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Finance;

/// <summary>
/// سند قبض العميل (يمثل الأموال المحصلة من العميل)
/// </summary>
public class CustomerReceipt : BaseEntity, IBranchEntity
{
    // الفرع
    public int BranchId { get; set; }

    // العميل
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // تاريخ الإيصال
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // المبلغ المقبوض الكلي
    public decimal Amount { get; set; }

    // الرصيد الذي لم يتم توزيعه/تخصيصه على فواتير بعد
    public decimal UnallocatedAmount { get; set; }

    // طريقة الدفع
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    // نوع المعاملة
    public TransactionKind Kind { get; set; } = TransactionKind.Normal;

    // الملاحظات
    public string? Notes { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }

    // ===== ERP Traceability & Financial Integrity =====
    public FinancialStatus FinancialStatus { get; set; } = FinancialStatus.Active;
    public FinancialSourceType? FinancialSourceType { get; set; }
    public int? FinancialSourceId { get; set; }
    public string? IdempotencyKey { get; set; } // للحماية من تكرار الضغطات
    
    // Cancellation tracking
    public int? ReversalOfId { get; set; } // If this is a reversal, point to the original record
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }

    // المخصصات التابعة لهذا السند
    public ICollection<CustomerReceiptAllocation> Allocations { get; set; } = [];
}
