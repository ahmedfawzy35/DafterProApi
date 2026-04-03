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

    // الملاحظات
    public string? Notes { get; set; }

    // المخصصات التابعة لهذا السند
    public ICollection<CustomerReceiptAllocation> Allocations { get; set; } = [];
}
