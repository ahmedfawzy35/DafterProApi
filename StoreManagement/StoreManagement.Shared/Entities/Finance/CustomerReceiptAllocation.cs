using StoreManagement.Shared.Entities.Sales;

namespace StoreManagement.Shared.Entities.Finance;

/// <summary>
/// تخصيص جزء من سند القبض لتسديد فاتورة معينة
/// </summary>
public class CustomerReceiptAllocation
{
    public int Id { get; set; }

    // المرجع للسند الأساسي
    public int CustomerReceiptId { get; set; }
    public CustomerReceipt CustomerReceipt { get; set; } = null!;

    // الفاتورة التي تم تسديدها
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    // المبلغ الذي تم تخصيصه من السند للفاتورة
    public decimal Amount { get; set; }
}
