namespace StoreManagement.Shared.DTOs;

public record BranchStockDto
{
    public int BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    
    // الرصيد الفعلي
    public decimal Quantity { get; init; }
    
    // محجوز
    public decimal ReservedQuantity { get; init; }
    
    // المتاح
    public decimal AvailableQuantity => Quantity - ReservedQuantity;
}
