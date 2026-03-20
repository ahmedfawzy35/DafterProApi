namespace StoreManagement.Shared.Entities.Partners;

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
