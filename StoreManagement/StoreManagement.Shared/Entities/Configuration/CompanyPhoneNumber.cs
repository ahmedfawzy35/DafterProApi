namespace StoreManagement.Shared.Entities.Configuration;

/// <summary>
/// أرقام الهواتف الخاصة بالشركة
/// </summary>
public class CompanyPhoneNumber
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string PhoneNumber { get; set; } = string.Empty;

    // هل الرقم يدعم واتساب
    public bool IsWhatsApp { get; set; } = false;
}
