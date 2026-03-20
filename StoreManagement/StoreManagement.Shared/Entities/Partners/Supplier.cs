namespace StoreManagement.Shared.Entities.Partners;

/// <summary>
/// كيان المورد مع دعم الحذف المؤقت وعزل البيانات
/// </summary>
public class Supplier : BaseEntity
{
    // اسم المورد
    public string Name { get; set; } = string.Empty;

    // الرصيد النقدي للمورد
    public decimal CashBalance { get; set; } = 0;

    // قائمة أرقام هواتف المورد
    public ICollection<SupplierPhone> Phones { get; set; } = [];
}

