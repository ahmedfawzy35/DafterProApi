using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Entities.Finance;

namespace StoreManagement.Shared.Entities.Sales.Installments;

public class InstallmentPaymentAllocation : BaseEntity, ICompanyEntity
{
    public int InstallmentScheduleItemId { get; set; }
    public InstallmentScheduleItem InstallmentScheduleItem { get; set; } = null!;

    public int CustomerReceiptId { get; set; }
    public CustomerReceipt CustomerReceipt { get; set; } = null!;

    public decimal AmountAllocated { get; set; }
    public decimal PenaltyAllocated { get; set; }

    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
}
