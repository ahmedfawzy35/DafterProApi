using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Sales.Installments;

public class InstallmentScheduleItem : BaseEntity, ICompanyEntity
{
    public int InstallmentPlanId { get; set; }
    public InstallmentPlan InstallmentPlan { get; set; } = null!;

    public DateTime DueDate { get; set; }
    
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PenaltyAmount { get; set; } 

    public InstallmentItemStatus Status { get; set; }
    public DateTime? SettledDate { get; set; }

    public ICollection<InstallmentPaymentAllocation> Allocations { get; set; } = new List<InstallmentPaymentAllocation>();
}
