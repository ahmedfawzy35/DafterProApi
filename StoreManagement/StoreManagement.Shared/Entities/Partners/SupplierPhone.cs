namespace StoreManagement.Shared.Entities.Partners;

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
