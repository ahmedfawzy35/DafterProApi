namespace StoreManagement.Shared.Entities;

// ===== Plans & Features =====

/// <summary>
/// خطة الاشتراك (Basic, Pro, Enterprise)
/// </summary>
public class Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;           // Basic, Pro, Enterprise
    public string DisplayName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxBranches { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PlanFeature> Features { get; set; } = [];
    public ICollection<CompanySubscription> Subscriptions { get; set; } = [];
}

/// <summary>
/// ميزة مرتبطة بخطة اشتراك
/// </summary>
public class PlanFeature
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    // رمز الميزة (مثل: "Payroll", "DigitalWallet", "MultiWarehouse")
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// اشتراك الشركة في خطة معينة
/// </summary>
public class CompanySubscription
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // ميزات مخصصة للشركة (Override على مستوى Plan)
    public ICollection<CompanyFeatureOverride> FeatureOverrides { get; set; } = [];
}

/// <summary>
/// تخصيص ميزة لشركة معينة (Override على الـ Plan)
/// </summary>
public class CompanyFeatureOverride
{
    public int Id { get; set; }
    public int CompanySubscriptionId { get; set; }
    public CompanySubscription CompanySubscription { get; set; } = null!;
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
