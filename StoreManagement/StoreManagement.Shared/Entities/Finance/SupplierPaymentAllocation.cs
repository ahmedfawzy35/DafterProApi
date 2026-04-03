using StoreManagement.Shared.Entities.Sales;

namespace StoreManagement.Shared.Entities.Finance;

/// <summary>
/// تخصيص جزء من سند الصرف لتسديد فاتورة شراء معينة
/// </summary>
public class SupplierPaymentAllocation
{
    public int Id { get; set; }

    // المرجع للسند الأساسي
    public int SupplierPaymentId { get; set; }
    public SupplierPayment SupplierPayment { get; set; } = null!;

    // الفاتورة التي تم تسديدها
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    // المبلغ الذي تم تخصيصه من السند للفاتورة
    public decimal Amount { get; set; }
}
