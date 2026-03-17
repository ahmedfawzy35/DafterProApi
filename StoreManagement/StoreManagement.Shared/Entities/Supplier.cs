namespace StoreManagement.Shared.Entities;

/// <summary>
/// كيان المورد مع دعم الحذف المؤقت وعزل البيانات
/// </summary>
public class Supplier : BaseEntity
{
    // اسم المورد
    public string Name { get; set; } = string.Empty;

    // الرصيد النقدي للمورد
    public double CashBalance { get; set; } = 0;

    // قائمة أرقام هواتف المورد
    public ICollection<SupplierPhone> Phones { get; set; } = [];
}

/// <summary>
/// أرقام هواتف المورد
/// </summary>
public class SupplierPhone
{
    public int Id { get; set; }

    // معرف المورد
    public int SupplierId { get; set; }

    // رقم الهاتف
    public string PhoneNumber { get; set; } = string.Empty;

    // علاقة بالمورد
    public Supplier Supplier { get; set; } = null!;
}
