namespace StoreManagement.Shared.DTOs;

public class InstallmentReportItemDto
{
    public int PlanId { get; set; }
    public int ScheduleItemId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal AmountDue { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalRequired { get; set; }
}

public class InstallmentSummaryDto
{
    public decimal TotalActivePlansAmount { get; set; }
    public decimal TotalCollectedAmount { get; set; }
    public decimal TotalOverdueAmount { get; set; }
    public int OverdueCustomersCount { get; set; }
}
