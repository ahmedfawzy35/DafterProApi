using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

public class ProductCostHistory : BaseEntity
{
    public int ProductId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal OldCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal NewCost { get; set; }
    
    public string Reason { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public int UserId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }
}
