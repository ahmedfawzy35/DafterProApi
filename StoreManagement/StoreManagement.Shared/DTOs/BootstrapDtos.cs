using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.DTOs.Settings;

namespace StoreManagement.Shared.DTOs;

public class BootstrapDto
{
    public UserProfileDto User { get; set; } = new();
    public CompanyInfoGroupDto Company { get; set; } = new();
    public FeatureFlagsDto Features { get; set; } = new();
    public List<BranchReadDto> Branches { get; set; } = new();
    public UIPreferencesDto UI { get; set; } = new();
}

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}

public class CompanyInfoGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "EGP";
}

public class FeatureFlagsDto
{
    public bool EnableSales { get; set; }
    public bool EnablePurchases { get; set; }
    public bool EnableInventory { get; set; }
    public bool EnableReturns { get; set; }
    public bool EnableInstallments { get; set; }
    public bool EnableHR { get; set; }
    public bool EnableTreasury { get; set; }
}

public class UIPreferencesDto
{
    public bool ShowAdvancedMenus { get; set; }
    public bool EnableQuickSaleScreen { get; set; }
    public bool EnableKeyboardShortcuts { get; set; }
}
