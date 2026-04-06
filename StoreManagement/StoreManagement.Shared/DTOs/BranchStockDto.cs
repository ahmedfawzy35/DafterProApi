namespace StoreManagement.Shared.DTOs;

public record BranchStockDto
{
    public int BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    
    // الرصيد الفعلي
    public double Quantity { get; init; }
    
    // محجوز
    public double ReservedQuantity { get; init; }
    
    // المتاح
    public double AvailableQuantity => Quantity - ReservedQuantity;
}
