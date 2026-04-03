using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// كيان تصنيف المنتجات
/// </summary>
public class ProductCategory : BaseEntity
{
    // اسم التصنيف
    public string Name { get; set; } = string.Empty;

    // وصف اختياري للتصنيف
    public string? Description { get; set; }

    // معرف التصنيف الأب (اختياري، لدعم الشجرة الهرمية)
    public int? ParentCategoryId { get; set; }

    public virtual ProductCategory? ParentCategory { get; set; }
    public virtual ICollection<ProductCategory> SubCategories { get; set; } = new List<ProductCategory>();
}
