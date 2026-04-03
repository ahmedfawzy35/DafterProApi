using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// مستند تسوية المخزون (تجميع لعناصر التسوية)
/// </summary>
public class StockAdjustment : BaseEntity, IBranchEntity
{
    public int BranchId { get; set; }
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public string? Notes { get; set; }
    
    // المستخدم الذي قام باعتماد التسوية
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // عناصر المستند
    public ICollection<StockAdjustmentItem> Items { get; set; } = new List<StockAdjustmentItem>();
}
