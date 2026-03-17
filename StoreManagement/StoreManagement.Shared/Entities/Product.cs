namespace StoreManagement.Shared.Entities;

/// <summary>
/// كيان المنتج مع دعم الصور والحذف المؤقت
/// </summary>
public class Product : BaseEntity
{
    // اسم المنتج
    public string Name { get; set; } = string.Empty;

    // سعر المنتج
    public double Price { get; set; }

    // سعر الشراء (لحساب الأرباح)
    public double CostPrice { get; set; }

    // الكمية المتاحة في المخزون
    public double StockQuantity { get; set; } = 0;

    // وحدة القياس (قطعة، كجم، متر، إلخ)
    public string Unit { get; set; } = "قطعة";

    // قائمة صور المنتج
    public ICollection<ProductImage> ProductImages { get; set; } = [];
}

/// <summary>
/// صور المنتج
/// </summary>
public class ProductImage
{
    public int Id { get; set; }

    // معرف المنتج
    public int ProductId { get; set; }

    // رابط الصورة
    public string ImageUrl { get; set; } = string.Empty;

    // هل هي الصورة المصغرة الرئيسية
    public bool IsThumbnail { get; set; } = false;

    // علاقة بالمنتج
    public Product Product { get; set; } = null!;
}
