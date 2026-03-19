namespace StoreManagement.Shared.DTOs;

// ===== DTOs خاصة بالاشتراكات =====

public class PlanReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxBranches { get; set; }
    public List<string> Features { get; set; } = [];
}

public class SubscriptionStatusDto
{
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public List<string> EnabledFeatures { get; set; } = [];
}

public class CreateSubscriptionDto
{
    public int CompanyId { get; set; }
    public int PlanId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public int DurationMonths { get; set; } = 1;
}

public class CreatePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxBranches { get; set; }
}

public class CreatePlanFeatureDto
{
    public int PlanId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

// ===== DTOs خاصة بالمصادقة + Refresh Token =====

public class RefreshTokenRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    
    // بيانات الشركة المرتبطة بالمستخدم
    public CompanyReadDto? Company { get; set; }
}
