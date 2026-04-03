using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// مستند تحويل المخزون بين فرعين
/// </summary>
public class StockTransfer : BaseEntity
{
    // الفرع المحول منه
    public int FromBranchId { get; set; }
    
    // الفرع المحول إليه
    public int ToBranchId { get; set; }
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public string? Notes { get; set; }
    
    // المستخدم الذي أنشأ التحويل
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    // عناصر عملية التحويل
    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}
