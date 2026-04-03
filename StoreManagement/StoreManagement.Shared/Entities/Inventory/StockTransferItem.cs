using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// عنصر داخل مستند نقل المخزون
/// </summary>
public class StockTransferItem : BaseEntity
{
    public int StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // الكمية المنقولة (يجب أن تكون موجبة دائمة)
    public double Quantity { get; set; }
}
