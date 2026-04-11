using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagement.Shared.Entities.Finance;

public class AccountingPeriod
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; } = false;

    public DateTime? ClosedAt { get; set; }
    
    public int? ClosedByUserId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
