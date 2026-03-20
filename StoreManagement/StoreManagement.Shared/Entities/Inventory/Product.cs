using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// كيان المنتج مع دعم الصور والحذف المؤقت والباركود
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

    // ===== الباركود =====

    /// <summary>
    /// رقم الباركود — دائماً مُعيَّن (غير nullable).
    /// إما مصنعي (ممسوح) أو مُولَّد تلقائياً بصيغة EAN-13.
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// مصدر الباركود: Generated (داخلي) أو Factory (مصنعي)
    /// </summary>
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Generated;

    /// <summary>
    /// صيغة الباركود: EAN13 أو CODE128
    /// </summary>
    public BarcodeFormat BarcodeFormat { get; set; } = BarcodeFormat.EAN13;

    // قائمة صور المنتج
    public ICollection<ProductImage> ProductImages { get; set; } = [];
}


