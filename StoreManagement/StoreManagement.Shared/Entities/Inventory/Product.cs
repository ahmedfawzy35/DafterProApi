using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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



    // أدنى مستوى للمخزون (حد إشعار تدني المخزون)
    public decimal MinimumStock { get; set; } = 0;

    // حد إعادة الطلب (Reorder Level) لتنبيه المشتريات
    public decimal ReorderLevel { get; set; } = 0;

    // وحدة القياس (قطعة، كجم، متر، إلخ)
    public string Unit { get; set; } = "قطعة";

    // ===== البيانات التجارية الإضافية =====

    // رمز التخزين التعريفي الداخلي (Stock Keeping Unit)
    public string? SKU { get; set; }

    // وصف المنتج
    public string? Description { get; set; }

    // العلامة التجارية
    public string? Brand { get; set; }

    // حالة المنتج (مفعل أم معطل)
    public bool IsActive { get; set; } = true;

    // هل المنتج قابل للبيع؟
    public bool IsSellable { get; set; } = true;

    // هل المنتج قابل للشراء؟
    public bool IsPurchasable { get; set; } = true;

    // التصنيف التابع له المنتج
    public int? CategoryId { get; set; }

    // علاقة بالتصنيف
    [ForeignKey(nameof(CategoryId))]
    public virtual ProductCategory? Category { get; set; }

    // ===== الباركود =====

    /// <summary>
    /// رقم الباركود — دائماً مُعيَّن (غير nullable).
    /// إما مصنعي (ممسوح) أو مُولَّد تلقائياً بصيغة EAN-13.
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// هل الباركود الحالي مُولَّد تلقائياً بواسطة النظام؟
    /// (true = يمكن استبداله بباركود حقيقي عند توفره، false = باركود مصنعي مقفل لا يمكن تعديله)
    /// </summary>
    public bool IsBarcodeGenerated { get; set; } = false;

    // نوع الباركود: مُولَّد تلقائياً أو من المصنع
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Generated;

    // صيغة الباركود: EAN13 أو CODE128
    public BarcodeFormat BarcodeFormat { get; set; } = BarcodeFormat.EAN13;


    // Navigation Properties
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public virtual ICollection<ProductCostHistory> CostHistories { get; set; } = new List<ProductCostHistory>();
}
