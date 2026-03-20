namespace StoreManagement.Shared.Entities.Inventory;

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
