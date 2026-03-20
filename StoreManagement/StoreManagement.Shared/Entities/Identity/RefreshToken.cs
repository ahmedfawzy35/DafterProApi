namespace StoreManagement.Shared.Entities.Identity;

/// <summary>
/// Refresh Token لتجديد JWT بأمان مع تتبع الجهاز والـ IP
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // الرمز المشفر
    public string Token { get; set; } = string.Empty;

    // تاريخ الإنشاء والانتهاء
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    // معلومات الجهاز والشبكة لمنع replay attacks
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // حالة الإلغاء
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }    // في حالة التجديد

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
