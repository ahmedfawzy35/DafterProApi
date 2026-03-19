namespace StoreManagement.Shared.Entities;

/// <summary>
/// لوجو الشركة مخزن بصيغة ثنائية (Binary)
/// </summary>
public class CompanyLogo
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // محتوى الصورة
    public byte[] Content { get; set; } = [];

    // حجم الملف بالبايت
    public long FileSize { get; set; }

    // نوع المحتوى (image/png, image/jpeg, etc.)
    public string? ContentType { get; set; }
}
