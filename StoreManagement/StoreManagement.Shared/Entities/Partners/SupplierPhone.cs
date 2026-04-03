namespace StoreManagement.Shared.Entities.Partners;

/// <summary>
/// رقم هاتف المورد — كيان مستقل لدعم تعدد الأرقام لكل مورد
/// </summary>
public class SupplierPhone
{
    public int Id { get; set; }

    // معرف المورد المالك لهذا الرقم
    public int SupplierId { get; set; }

    // رقم الهاتف مع مؤشر الدولة (مثل: 01012345678)
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// هل هذا الرقم هو الرقم الأساسي للمورد؟
    /// يُستخدم في البحث السريع وعرض الرقم الأول
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    // علاقة بالمورد
    public Supplier Supplier { get; set; } = null!;
}
