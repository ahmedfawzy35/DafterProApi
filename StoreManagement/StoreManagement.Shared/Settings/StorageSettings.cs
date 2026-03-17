namespace StoreManagement.Shared.Settings;

/// <summary>
/// إعدادات رفع الملفات وتخزينها
/// </summary>
public class StorageSettings
{
    public string LocalStoragePath { get; set; } = "wwwroot/uploads";
    public int MaxFileSizeMB { get; set; } = 10;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp", ".pdf", ".xlsx"];
}

/// <summary>
/// إعدادات الكاش
/// </summary>
public class CacheSettings
{
    public int DefaultExpirationMinutes { get; set; } = 30;
    public string RedisConnectionString { get; set; } = "localhost:6379";
}

/// <summary>
/// إعدادات Rate Limiting
/// </summary>
public class RateLimitSettings
{
    public int PermitLimit { get; set; } = 100;         // عدد الطلبات المسموح به لكل مستخدم
    public int WindowSeconds { get; set; } = 60;         // فترة النافذة بالثواني
    public int QueueLimit { get; set; } = 20;            // حجم صف الانتظار
}
