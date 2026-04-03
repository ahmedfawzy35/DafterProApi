using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Finance;

/// <summary>
/// سند صرف المورد (يمثل الأموال المدفوعة للمورد)
/// </summary>
public class SupplierPayment : BaseEntity, IBranchEntity
{
    // الفرع
    public int BranchId { get; set; }

    // المورد
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    // تاريخ الإيصال
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // المبلغ المدفوع الكلي
    public decimal Amount { get; set; }

    // الرصيد الذي لم يتم توزيعه/تخصيصه على فواتير بعد
    public decimal UnallocatedAmount { get; set; }

    // طريقة الدفع
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    // نوع المعاملة
    public TransactionKind Kind { get; set; } = TransactionKind.Normal;

    // الملاحظات
    public string? Notes { get; set; }

    // المخصصات التابعة لهذا السند
    public ICollection<SupplierPaymentAllocation> Allocations { get; set; } = [];
}
