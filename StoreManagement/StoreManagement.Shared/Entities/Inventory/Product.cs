namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// كيان المنتج مع دعم الصور والحذف المؤقت
/// </summary>
public class Product : BaseEntity
{
    // اسم المنتج
    public string Name { get; set; } = string.Empty;

    // سعر المنتج
    public decimal Price { get; set; }

    // سعر الشراء (لحساب الأرباح)
    public decimal CostPrice { get; set; }

    // الكمية المتاحة في المخزون
    public double StockQuantity { get; set; } = 0;

    // وحدة القياس (قطعة، كجم، متر، إلخ)
    public string Unit { get; set; } = "قطعة";

    // قائمة صور المنتج
    public ICollection<ProductImage> ProductImages { get; set; } = [];
}

