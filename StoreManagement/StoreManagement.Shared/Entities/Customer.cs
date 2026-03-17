namespace StoreManagement.Shared.Entities;

/// <summary>
/// كيان العميل مع دعم الحذف المؤقت وعزل البيانات
/// </summary>
public class Customer : BaseEntity
{
    // اسم العميل
    public string Name { get; set; } = string.Empty;

    // الرصيد النقدي للعميل
    public double CashBalance { get; set; } = 0;

    // قائمة أرقام هواتف العميل
    public ICollection<CustomerPhone> Phones { get; set; } = [];
}

/// <summary>
/// أرقام هواتف العميل
/// </summary>
public class CustomerPhone
{
    public int Id { get; set; }

    // معرف العميل
    public int CustomerId { get; set; }

    // رقم الهاتف
    public string PhoneNumber { get; set; } = string.Empty;

    // علاقة بالعميل
    public Customer Customer { get; set; } = null!;
}
