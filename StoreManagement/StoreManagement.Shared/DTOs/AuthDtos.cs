namespace StoreManagement.Shared.DTOs;

// ===== DTOs خاصة بالمصادقة =====

/// <summary>
/// بيانات تسجيل الدخول
/// </summary>
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// الاستجابة بعد تسجيل الدخول الناجح
/// </summary>
public class LoginResponseDto
{
    // رمز الدخول JWT
    public string Token { get; set; } = string.Empty;

    // وقت انتهاء الصلاحية
    public DateTime ExpiresAt { get; set; }

    // اسم المستخدم
    public string UserName { get; set; } = string.Empty;

    // الأدوار الوظيفية
    public List<string> Roles { get; set; } = [];
}

/// <summary>
/// تسجيل مستخدم جديد
/// </summary>
public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string Role { get; set; } = "Sales";
}

// ===== DTOs خاصة بالصفحات والفلاتر =====

/// <summary>
/// معاملات الاستعلام للقوائم المقسّمة على صفحات
/// </summary>
public class PaginationQueryDto
{
    // رقم الصفحة (يبدأ من 1)
    private int _pageNumber = 1;
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    // حجم الصفحة
    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 100 ? 100 : (value < 1 ? 20 : value);
    }

    // نص البحث
    public string? Search { get; set; }
}
