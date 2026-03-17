namespace StoreManagement.Shared.Settings;

/// <summary>
/// إعدادات المصادقة بالرمز المميز JWT
/// </summary>
public class JwtSettings
{
    // المفتاح السري لتوقيع الرمز
    public string SecretKey { get; set; } = string.Empty;

    // الجهة المُصدِرة للرمز
    public string Issuer { get; set; } = string.Empty;

    // الجهة المستهدفة
    public string Audience { get; set; } = string.Empty;

    // مدة صلاحية الرمز بالدقائق
    public int ExpirationInMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
