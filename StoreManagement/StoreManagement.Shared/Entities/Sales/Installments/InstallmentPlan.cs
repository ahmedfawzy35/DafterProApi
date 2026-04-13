using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Sales.Installments;

public class InstallmentPlan : BaseEntity, ICompanyEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public decimal DownPayment { get; set; }
    public decimal RemainingAmount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public InstallmentPlanStatus Status { get; set; }

    public ICollection<InstallmentScheduleItem> Schedules { get; set; } = new List<InstallmentScheduleItem>();
    public ICollection<InstallmentRescheduleHistory> RescheduleHistories { get; set; } = new List<InstallmentRescheduleHistory>();
}
