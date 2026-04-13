using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Sales.Installments;

public class InstallmentRescheduleHistory : BaseEntity, ICompanyEntity
{
    public int InstallmentPlanId { get; set; }
    public InstallmentPlan InstallmentPlan { get; set; } = null!;

    public string OldScheduleSnapshotJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RescheduleDate { get; set; } = DateTime.UtcNow;

    public int? ApprovedByUserId { get; set; }
}
