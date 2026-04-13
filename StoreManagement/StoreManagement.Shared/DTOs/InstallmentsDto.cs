using System.ComponentModel.DataAnnotations;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.DTOs;

public class CreateInstallmentPlanDto
{
    [Required]
    public int InvoiceId { get; set; }
    
    [Required]
    public int CustomerId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DownPayment { get; set; }

    [Required]
    [Range(1, 360)]
    public int Months { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
}

public class InstallmentSchedulePreviewDto
{
    public decimal TotalAmount { get; set; }
    public decimal DownPayment { get; set; }
    public decimal AmountToFinance { get; set; }
    public int Months { get; set; }
    public decimal MonthlyInstallment { get; set; }
    
    public List<InstallmentScheduleItemPreviewDto> Items { get; set; } = new();
}

public class InstallmentScheduleItemPreviewDto
{
    public int MonthNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
}

public class InstallmentPlanReadDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DownPayment { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public List<InstallmentScheduleItemDto> Schedules { get; set; } = new();
}

public class InstallmentScheduleItemDto
{
    public int Id { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SettledDate { get; set; }
}

public class InstallmentPaymentResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal AppliedAmount { get; set; }
    public decimal AppliedPenalty { get; set; }
    public int ReceiptId { get; set; }
}
