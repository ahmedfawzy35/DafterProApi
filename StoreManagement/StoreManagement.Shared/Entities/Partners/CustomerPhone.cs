namespace StoreManagement.Shared.Entities.Partners;

/// <summary>
/// رقم هاتف العميل — كيان مستقل لدعم تعدد الأرقام لكل عميل
/// </summary>
public class CustomerPhone
{
    public int Id { get; set; }

    // معرف العميل المالك لهذا الرقم
    public int CustomerId { get; set; }

    // رقم الهاتف مع مؤشر الدولة (مثل: 01012345678)
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// هل هذا الرقم هو الرقم الأساسي للعميل؟
    /// يُستخدم في البحث السريع وعرض الرقم الأول
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    // علاقة بالعميل
    public Customer Customer { get; set; } = null!;
}
