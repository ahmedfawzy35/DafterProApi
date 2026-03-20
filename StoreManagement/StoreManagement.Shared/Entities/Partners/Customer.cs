namespace StoreManagement.Shared.Entities.Partners;

/// <summary>
/// كيان العميل مع دعم الحذف المؤقت وعزل البيانات
/// </summary>
public class Customer : BaseEntity
{
    // اسم العميل
    public string Name { get; set; } = string.Empty;

    // الرصيد النقدي للعميل
    public decimal CashBalance { get; set; } = 0;

    // قائمة أرقام هواتف العميل
    public ICollection<CustomerPhone> Phones { get; set; } = [];
}

