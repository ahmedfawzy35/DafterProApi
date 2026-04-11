using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// عنصر داخل مستند تسوية المخزون
/// </summary>
public class StockAdjustmentItem : BaseEntity
{
    public int StockAdjustmentId { get; set; }
    public StockAdjustment StockAdjustment { get; set; } = null!;
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // دلتا الكمية (موجب مضاف، سالب مخصوم)
    public decimal Quantity { get; set; } 
    
    public StockAdjustmentReason ReasonType { get; set; }
}
